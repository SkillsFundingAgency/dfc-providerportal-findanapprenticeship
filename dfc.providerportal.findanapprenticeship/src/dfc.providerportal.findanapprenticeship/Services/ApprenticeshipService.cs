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
        private readonly IProviderServiceWrapper _providerService;
        private readonly ICosmosDbCollectionSettings _settings;
        private readonly IProviderServiceSettings _providerServiceSettings;


        public ApprenticeshipService(
            TelemetryClient telemetryClient,
            ICosmosDbHelper cosmosDbHelper,
            IDASHelper DASHelper,
            IOptions<CosmosDbCollectionSettings> settings,
            IOptions<ProviderServiceSettings> providerServiceSettings, IProviderServiceWrapper providerService)
        {
            Throw.IfNull(telemetryClient, nameof(telemetryClient));
            Throw.IfNull(cosmosDbHelper, nameof(cosmosDbHelper));
            Throw.IfNull(DASHelper, nameof(DASHelper));
            Throw.IfNull(providerService, nameof(providerService));
            Throw.IfNull(settings, nameof(settings));
            Throw.IfNull(providerServiceSettings, nameof(providerServiceSettings));

            _telemetryClient = telemetryClient;
            _cosmosDbHelper = cosmosDbHelper;
            _DASHelper = DASHelper;
            _providerService = providerService;
            _settings = settings.Value;
            _providerServiceSettings = providerServiceSettings.Value;
        }

        public async Task<IEnumerable<IApprenticeship>> GetApprenticeshipCollection()
        {
            using (var client = _cosmosDbHelper.GetTcpClient())
            {
                await _cosmosDbHelper.CreateDatabaseIfNotExistsAsync(client);
                await _cosmosDbHelper.CreateDocumentCollectionIfNotExistsAsync(client, _settings.ApprenticeshipCollectionId);

                return _cosmosDbHelper.GetApprenticeshipCollection(client, _settings.ApprenticeshipCollectionId);
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
                await _cosmosDbHelper.CreateDocumentCollectionIfNotExistsAsync(client, _settings.ApprenticeshipCollectionId);

                var docs = _cosmosDbHelper.GetApprenticeshipByUKPRN(client, _settings.ApprenticeshipCollectionId, ukprn);
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
            List<DASProvider> providers = new List<DASProvider>();
            List<string> listOfProviderUKPRN = new List<string>();

            var eventProperties = new Dictionary<string, string>();
            var metrics = new Dictionary<string, double>();

            var evt = new EventTelemetry();
            evt.Name = "ExportToDas";

            listOfProviderUKPRN = apprenticeships.Select(x => x.ProviderUKPRN.ToString())
                                                 .Distinct()
                                                 .ToList();

            foreach (var ukprn in listOfProviderUKPRN)
            {
                int success = 0, failure = 0;

                try
                {
                    var providerApprenticeships = apprenticeships
                        .Where(x => x.ProviderUKPRN.ToString() == ukprn && x.RecordStatus == RecordStatus.Live)
                        .ToList();
                    var provider = ExportProvider(providerApprenticeships, ukprn);

                    providers.Add(provider);
                    success++;
                }
                catch (Exception e)
                {
                    failure++;
                    _telemetryClient.TrackException(e);
                }
                finally
                {
                    metrics.Add("Export success", success);
                    metrics.Add("Export failures", failure);
                    _telemetryClient.TrackEvent(evt);
                }
            }

            return providers;
        }

        private DASProvider ExportProvider(List<Apprenticeship> apprenticeships, string ukprn)
        {
            var eventProperties = new Dictionary<string, string>();
            var metrics = new Dictionary<string, double>();

            var evt = new EventTelemetry();
            evt.Name = "ExportProvider";

            eventProperties.TryAdd("UKPRN", ukprn);
            metrics.TryAdd("Apprenticeships", apprenticeships.Count());

            var providerDetailsList = GetProviderDetails(ukprn).ToList();
            metrics.TryAdd("MatchingProviders", providerDetailsList.Count());

            if (providerDetailsList.Any())
            {
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

                        metrics.TryAdd("Locations", exportLocations.Count());
                        metrics.TryAdd("Standards", exportStandards.Count());
                        metrics.TryAdd("Frameworks", exportFrameworks.Count());

                        dasProvider.Locations = _DASHelper.ApprenticeshipLocationsToLocations(exportLocations);
                        dasProvider.Standards = _DASHelper.ApprenticeshipsToStandards(exportStandards);
                        dasProvider.Frameworks = _DASHelper.ApprenticeshipsToFrameworks(exportFrameworks);

                        _telemetryClient.TrackEvent(evt);

                        return dasProvider;
                    }
                }
                catch (Exception e)
                {
                    _telemetryClient.TrackException(e, eventProperties, metrics);
                    throw new ProviderExportException(ukprn, e);
                }
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
