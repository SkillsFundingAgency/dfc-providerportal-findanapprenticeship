using Dfc.Providerportal.FindAnApprenticeship.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Apprenticeships;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.DAS;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Settings;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Models.DAS;
using Dfc.Providerportal.FindAnApprenticeship.Models.Enums;
using Dfc.Providerportal.FindAnApprenticeship.Models.Providers;
using Dfc.Providerportal.FindAnApprenticeship.Settings;
using Dfc.ProviderPortal.Packages;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Dfc.Providerportal.FindAnApprenticeship.Services
{
    public class ApprenticeshipService : IApprenticeshipService
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ICosmosDbHelper _cosmosDbHelper;
        private readonly IDASHelper _DASHelper;
        private readonly ICosmosDbCollectionSettings _cosmosSettings;
        private readonly IProviderServiceSettings _providerServiceSettings;
        private readonly IProviderServiceWrapper _providerService;

        public ApprenticeshipService(
            TelemetryClient telemetryClient,
            ICosmosDbHelper cosmosDbHelper,
            IOptions<CosmosDbCollectionSettings> cosmosSettings,
            IProviderServiceWrapper providerService, 
            IDASHelper DASHelper)
        {
            Throw.IfNull(telemetryClient, nameof(telemetryClient));
            Throw.IfNull(cosmosDbHelper, nameof(cosmosDbHelper));
            Throw.IfNull(DASHelper, nameof(DASHelper));
            Throw.IfNull(cosmosSettings, nameof(cosmosSettings));
            Throw.IfNull(providerService, nameof(providerService));

            _telemetryClient = telemetryClient;
            _cosmosDbHelper = cosmosDbHelper;
            _DASHelper = DASHelper;
            _providerService = providerService;
            _cosmosSettings = cosmosSettings.Value;
        }

        public async Task<IEnumerable<IApprenticeship>> GetApprenticeshipCollection()
        {
            using (var client = _cosmosDbHelper.GetTcpClient())
            {
                await _cosmosDbHelper.CreateDatabaseIfNotExistsAsync(client);
                await _cosmosDbHelper.CreateDocumentCollectionIfNotExistsAsync(client, _cosmosSettings.ApprenticeshipCollectionId);

                return _cosmosDbHelper.GetApprenticeshipCollection(client, _cosmosSettings.ApprenticeshipCollectionId);
            }
        }

        public async Task<IEnumerable<IApprenticeship>> GetApprenticeshipsByUkprn(int ukprn)
        {
            Throw.IfNull(ukprn, nameof(ukprn));
            Throw.IfLessThan(0, ukprn, nameof(ukprn));

            IEnumerable<Apprenticeship> persisted;
            using (var client = _cosmosDbHelper.GetTcpClient())
            {
                await _cosmosDbHelper.CreateDatabaseIfNotExistsAsync(client);
                await _cosmosDbHelper.CreateDocumentCollectionIfNotExistsAsync(client, _cosmosSettings.ApprenticeshipCollectionId);

                var docs = _cosmosDbHelper.GetApprenticeshipByUKPRN(client, _cosmosSettings.ApprenticeshipCollectionId, ukprn);
                persisted = docs;
            }

            return persisted;
        }

        /// <summary>
        /// Maps apprenticeships to provider(s) ready for export to DAS
        /// </summary>
        /// <param name="apprenticeships">A list of apprenticeships to be processed and grouped into Providers</param>
        /// <returns></returns>
        [Obsolete("This shouldn't be used any more - if possible replace with a mapping class using something like AutoMapper ", false)]
        public IEnumerable<IDASProvider> ApprenticeshipsToDASProviders(List<Apprenticeship> apprenticeships)
        {
            var timer = Stopwatch.StartNew();
            List<DASProvider> providers = new List<DASProvider>();
            List<string> listOfProviderUKPRN = new List<string>();


            var evt = new EventTelemetry {Name = "ApprenticeshipsToDASProviders" };

            listOfProviderUKPRN = apprenticeships.Select(x => x.ProviderUKPRN.ToString())
                                                 .OrderBy(x => x)
                                                 .Distinct()
                                                 .ToList();
            
            evt.Metrics.TryAdd("Apprenticeships", apprenticeships.Count);
            evt.Metrics.TryAdd("Providers", listOfProviderUKPRN.Count);

            Console.WriteLine($"[{DateTime.UtcNow:G}] Found {apprenticeships.Count} apprenticeships for {listOfProviderUKPRN.Count} Providers");

            int success = 0, failure = 0, count = 1;

            Parallel.ForEach(listOfProviderUKPRN, (ukprn) =>
            {
                try
                {
                    var providerApprenticeships = apprenticeships
                        .Where(x => x.ProviderUKPRN.ToString() == ukprn && x.RecordStatus == RecordStatus.Live)
                        .ToList();
                    var provider = ExportProvider(providerApprenticeships, ukprn);

                    providers.Add(provider);
                    success++;
                    Console.WriteLine($"[{DateTime.UtcNow:G}] Exported {ukprn} ({count} of {listOfProviderUKPRN.Count})");
                }
                catch (ExportException e)
                {
                    failure++;
                    _telemetryClient.TrackException(e);
                    Console.WriteLine($"[{DateTime.UtcNow:G}] Failed to export {ukprn} ({count} of {listOfProviderUKPRN.Count})");
                }
                count++;
            });

            evt.Metrics.TryAdd("Export success", success);
            evt.Metrics.TryAdd("Export failures", failure);
            _telemetryClient.TrackEvent(evt);
            Console.WriteLine($"[{DateTime.UtcNow:G}] Exported {success} Providers in {timer.Elapsed.TotalSeconds} seconds.");
            if (failure > 1)
                Console.WriteLine($"[{DateTime.UtcNow:G}] [WARNING] Encountered {failure} errors that need attention");
            
            timer.Stop();

            Console.WriteLine($"[{DateTime.UtcNow:G}] Exported {success} Providers in {timer.Elapsed.TotalSeconds} seconds.");
            if (failure > 1)
                Console.WriteLine($"[{DateTime.UtcNow:G}] [WARNING] Encountered {failure} errors that need attention");
            return providers;
        }

        private DASProvider ExportProvider(List<Apprenticeship> apprenticeships, string ukprn)
        {
            var evt = new EventTelemetry {Name = "ComposeProviderForExport"};

            evt.Properties.TryAdd("UKPRN", ukprn);
            evt.Metrics.TryAdd("ProviderApprenticeships", apprenticeships.Count());

            var providerDetailsList = GetProviderDetails(ukprn).ToList();
            evt.Metrics.TryAdd("MatchingProviders", providerDetailsList.Count());

            if (!providerDetailsList.Any()) throw new ProviderNotFoundException(ukprn);
            try
            {
                    var dasProvider = _DASHelper.CreateDASProviderFromProvider(providerDetailsList.FirstOrDefault());

                if (dasProvider != null)
                {
                    var apprenticeshipLocations = apprenticeships.Where(x => x.ApprenticeshipLocations != null)
                        .SelectMany(x => x.ApprenticeshipLocations).Distinct();

                    var exportLocations = apprenticeshipLocations.Where(x => x.RecordStatus == RecordStatus.Live);
                    var exportStandards = apprenticeships.Where(x => x.StandardCode.HasValue);
                    var exportFrameworks = apprenticeships.Where(x => x.FrameworkCode.HasValue);

                    evt.Metrics.TryAdd("ProviderLocations", exportLocations.Count());
                    evt.Metrics.TryAdd("ProviderStandards", exportStandards.Count());
                    evt.Metrics.TryAdd("ProviderFrameworks", exportFrameworks.Count());

                    dasProvider.Locations = _DASHelper.ApprenticeshipLocationsToLocations(exportLocations);
                    dasProvider.Standards = _DASHelper.ApprenticeshipsToStandards(exportStandards);
                    dasProvider.Frameworks = _DASHelper.ApprenticeshipsToFrameworks(exportFrameworks);

                    _telemetryClient.TrackEvent(evt);

                    return dasProvider;
                }
            }
            catch (Exception e)
            {
                throw new ProviderExportException(ukprn, e);
            }

            throw new ProviderNotFoundException(ukprn);
        }

        internal IEnumerable<Provider> GetProviderDetails(string UKPRN)
        {
            return _providerService.GetProviderByUKPRN(UKPRN);
        }

        internal IEnumerable<Apprenticeship> OnlyUpdatedCourses(IEnumerable<Apprenticeship> apprenticeships)
        {
            DateTime dateToCheckAgainst = DateTime.Now.Subtract(TimeSpan.FromDays(1));

            return apprenticeships.Where(x => x.UpdatedDate.HasValue && x.UpdatedDate > dateToCheckAgainst ||
                                       x.CreatedDate > dateToCheckAgainst && x.RecordStatus == RecordStatus.Live).ToList();
        }
        internal IEnumerable<Apprenticeship> RemoveNonLiveApprenticeships(IEnumerable<Apprenticeship> apprenticeships)
        {
            List<Apprenticeship> editedList = new List<Apprenticeship>();
            foreach (var apprenticeship in apprenticeships)
            {
                if (apprenticeship.ApprenticeshipLocations.All(x => x.RecordStatus == RecordStatus.Live))
                {
                    editedList.Add(apprenticeship);
                }
            }
            return editedList;
        }
    }
}
