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
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using LazyCache;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json.Converters;

namespace Dfc.Providerportal.FindAnApprenticeship.Services
{
    public class ApprenticeshipService : IApprenticeshipService
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ICosmosDbHelper _cosmosDbHelper;
        private readonly IDASHelper _DASHelper;
        private readonly ICosmosDbCollectionSettings _cosmosSettings;
        private readonly IProviderServiceSettings _providerServiceSettings;
        private readonly IProviderServiceClient _providerService;
        private readonly IAppCache _cache;

        public ApprenticeshipService(
            TelemetryClient telemetryClient,
            ICosmosDbHelper cosmosDbHelper,
            IOptions<CosmosDbCollectionSettings> cosmosSettings,
            IProviderServiceClient providerService, 
            IDASHelper DASHelper, IAppCache cache)
        {
            Throw.IfNull(telemetryClient, nameof(telemetryClient));
            Throw.IfNull(cosmosDbHelper, nameof(cosmosDbHelper));
            Throw.IfNull(DASHelper, nameof(DASHelper));
            Throw.IfNull(cosmosSettings, nameof(cosmosSettings));
            Throw.IfNull(providerService, nameof(providerService));

            _telemetryClient = telemetryClient;
            _cosmosDbHelper = cosmosDbHelper;
            _DASHelper = DASHelper;
            _cache = cache;
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

        public async Task<IEnumerable<IApprenticeship>> GetLiveApprenticeships()
        {
            Func<Task<List<Apprenticeship>>> liveApprenticeshipsGetter = async () =>
            {
                using (var client = _cosmosDbHelper.GetTcpClient())
                {
                    await _cosmosDbHelper.CreateDatabaseIfNotExistsAsync(client);
                    await _cosmosDbHelper.CreateDocumentCollectionIfNotExistsAsync(client,
                        _cosmosSettings.ApprenticeshipCollectionId);

                    return _cosmosDbHelper.GetLiveApprenticeships(client, _cosmosSettings.ApprenticeshipCollectionId);
                }
            };

            return await _cache.GetOrAddAsync("LiveApprenticeships", liveApprenticeshipsGetter,
                DateTimeOffset.Now.AddHours(8));
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
        public IEnumerable<IDasProvider> ApprenticeshipsToDasProviders(List<Apprenticeship> apprenticeships)
        {
            try
            {
                var timer = Stopwatch.StartNew();
                var export = new List<DasProvider>();

                var evt = new EventTelemetry { Name = "ApprenticeshipsToDasProviders" };

                var providerIndex = 1000;
                var apprenticeshipProviders = apprenticeships
                    .Select(x => x.ProviderUKPRN)
                    .OrderBy(x => x)
                    .Distinct()
                    .ToList();

                var apprenticeshipProvidersIndex = apprenticeshipProviders.ToDictionary(x => providerIndex++);

                evt.Metrics.TryAdd("Apprenticeships", apprenticeships.Count);
                evt.Metrics.TryAdd("Providers", apprenticeshipProviders.Count);

                Console.WriteLine(
                    $"[{DateTime.UtcNow:G}] Found {apprenticeships.Count} apprenticeships for {apprenticeshipProviders.Count} Providers");

                int success = 0, failure = 0, count = 1;

                Parallel.ForEach(apprenticeshipProvidersIndex, (currentIndex) =>
                {
                    try
                    {
                        var providerApprenticeships = apprenticeships
                            .Where(x => x.ProviderUKPRN == currentIndex.Value && x.RecordStatus == RecordStatus.Live)
                            .ToList();

                        var provider = ExportProvider(providerApprenticeships, currentIndex.Value, currentIndex.Key);

                        export.Add(provider);
                        success++;
                        Console.WriteLine(
                            $"[{DateTime.UtcNow:G}][INFO] Exported {currentIndex.Value} ({count} of {apprenticeshipProviders.Count})");
                    }
                    catch (ExportException e)
                    {
                        failure++;
                        _telemetryClient.TrackException(e);
                        Console.WriteLine(
                            $"[{DateTime.UtcNow:G}][ERROR] Failed to export {currentIndex.Value} ({count} of {apprenticeshipProviders.Count})");
                    }

                    count++;
                });

                Console.WriteLine($"[{DateTime.UtcNow:G}] Exported {success} Providers in {timer.Elapsed.TotalSeconds} seconds.");
                if (failure > 1)
                    Console.WriteLine($"[{DateTime.UtcNow:G}] [WARNING] Encountered {failure} errors that need attention");

                timer.Stop();

                evt.Metrics.TryAdd("Export elapsed time (ms)", timer.ElapsedMilliseconds);
                evt.Metrics.TryAdd("Export success", success);
                evt.Metrics.TryAdd("Export failures", failure);
                _telemetryClient.TrackEvent(evt);

                return export;
            }
            catch (Exception e)
            {
                throw new ProviderServiceException(e);
            }
        }

        private DasProvider ExportProvider(List<Apprenticeship> apprenticeships, int ukprn, int exportKey)
        {
            var evt = new EventTelemetry {Name = "ComposeProviderForExport"};

            evt.Properties.TryAdd("UKPRN", $"{ukprn}");
            evt.Metrics.TryAdd("ProviderApprenticeships", apprenticeships.Count());

            var providerDetailsList = GetProviderDetails(ukprn).ToList();
            evt.Metrics.TryAdd("MatchingProviders", providerDetailsList.Count());

            if (!providerDetailsList.Any()) throw new ProviderNotFoundException(ukprn);
            try
            {
                var DasProvider = _DASHelper.CreateDasProviderFromProvider(providerDetailsList.FirstOrDefault());

                if (DasProvider != null)
                {
                    var apprenticeshipLocations = apprenticeships.Where(x => x.ApprenticeshipLocations != null)
                        .SelectMany(x => x.ApprenticeshipLocations).Distinct(new ApprenticeshipLocationSameAddress());

                    var index = 1000;
                    var locationIndex = new Dictionary<string, ApprenticeshipLocation>(apprenticeshipLocations
                        .Where(x => x.RecordStatus == RecordStatus.Live)
                        .Select(x => new KeyValuePair<string, ApprenticeshipLocation>($"{exportKey:D4}{index++:D4}", x)));

                    var exportStandards = apprenticeships.Where(x => x.StandardCode.HasValue);
                    var exportFrameworks = apprenticeships.Where(x => x.FrameworkCode.HasValue);

                    evt.Metrics.TryAdd("Provider Locations", locationIndex.Count());
                    evt.Metrics.TryAdd("Provider Standards", exportStandards.Count());
                    evt.Metrics.TryAdd("Provider Frameworks", exportFrameworks.Count());

                    DasProvider.Locations = _DASHelper.ApprenticeshipLocationsToLocations(exportKey, locationIndex);
                    DasProvider.Standards = _DASHelper.ApprenticeshipsToStandards(exportKey, exportStandards, locationIndex);
                    DasProvider.Frameworks = _DASHelper.ApprenticeshipsToFrameworks(exportKey, exportFrameworks, locationIndex);

                    _telemetryClient.TrackEvent(evt);

                    return DasProvider;
                }
            }
            catch (Exception e)
            {
                throw new ProviderExportException(ukprn, e);
            }

            throw new ProviderNotFoundException(ukprn);
        }

        internal IEnumerable<Provider> GetProviderDetails(int UKPRN)
        {
            return _providerService.GetProviderByUkprn(UKPRN);
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
