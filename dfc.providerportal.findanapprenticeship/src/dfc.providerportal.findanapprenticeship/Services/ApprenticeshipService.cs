﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dfc.Providerportal.FindAnApprenticeship.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Apprenticeships;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Settings;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Models.DAS;
using Dfc.Providerportal.FindAnApprenticeship.Models.Enums;
using Dfc.Providerportal.FindAnApprenticeship.Models.Providers;
using Dfc.Providerportal.FindAnApprenticeship.Settings;
using Dfc.ProviderPortal.Packages;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Options;

namespace Dfc.Providerportal.FindAnApprenticeship.Services
{
    public class ApprenticeshipService : IApprenticeshipService
    {
        private readonly ICosmosDbHelper _cosmosDbHelper;
        private readonly ICosmosDbCollectionSettings _cosmosSettings;
        private readonly IDASHelper _DASHelper;
        private readonly IProviderServiceClient _providerService;
        private readonly TelemetryClient _telemetryClient;

        public ApprenticeshipService(
            ICosmosDbHelper cosmosDbHelper,
            IOptions<CosmosDbCollectionSettings> cosmosSettings,
            IDASHelper DASHelper,
            IProviderServiceClient providerService,
            TelemetryClient telemetryClient)
        {
            Throw.IfNull(telemetryClient, nameof(telemetryClient));
            Throw.IfNull(cosmosDbHelper, nameof(cosmosDbHelper));
            Throw.IfNull(DASHelper, nameof(DASHelper));
            Throw.IfNull(cosmosSettings, nameof(cosmosSettings));
            Throw.IfNull(providerService, nameof(providerService));

            _cosmosDbHelper = cosmosDbHelper;
            _cosmosSettings = cosmosSettings.Value;
            _DASHelper = DASHelper;
            _providerService = providerService;
            _telemetryClient = telemetryClient;
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
            using (var client = _cosmosDbHelper.GetTcpClient())
            {
                await _cosmosDbHelper.CreateDatabaseIfNotExistsAsync(client);
                await _cosmosDbHelper.CreateDocumentCollectionIfNotExistsAsync(client,
                    _cosmosSettings.ApprenticeshipCollectionId);

                return _cosmosDbHelper.GetLiveApprenticeships(client, _cosmosSettings.ApprenticeshipCollectionId);
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
        public async Task<IEnumerable<DasProviderResult>> ApprenticeshipsToDasProviders(List<Apprenticeship> apprenticeships)
        {
            try
            {
                var timer = Stopwatch.StartNew();
                var evt = new EventTelemetry { Name = "ApprenticeshipsToDasProviders" };

                var apprenticeshipsByUKPRN = apprenticeships
                    .GroupBy(a => a.ProviderUKPRN)
                    .OrderBy(g => g.Key)
                    .ToArray();

                var providersByUKPRN = (await _providerService.GetAllProviders())
                    .GroupBy(p => p.UnitedKingdomProviderReferenceNumber)
                    .ToDictionary(p => p.Key, p => p.AsEnumerable());

                evt.Metrics.TryAdd("Apprenticeships", apprenticeships.Count);
                evt.Metrics.TryAdd("Providers", apprenticeshipsByUKPRN.Length);

                Console.WriteLine($"[{DateTime.UtcNow:G}] Found {apprenticeships.Count} apprenticeships for {apprenticeshipsByUKPRN.Length} Providers");

                var results = new ConcurrentBag<DasProviderResult>();

                Parallel.ForEach(apprenticeshipsByUKPRN.Select((g, i) =>
                    new { UKPRN = g.Key, Index = i, Apprenticeships = g.Where(a => a.RecordStatus == RecordStatus.Live).ToList() }), p =>
                {
                    try
                    {
                        providersByUKPRN.TryGetValue(p.UKPRN.ToString(), out var providers);

                        var provider = ExportProvider(providers, p.Apprenticeships, p.UKPRN, p.Index + 1000);

                        results.Add(DasProviderResult.Succeeded(p.UKPRN, provider));

                        Console.WriteLine($"[{DateTime.UtcNow:G}][INFO] Exported {p.UKPRN} ({p.Index} of {p.Apprenticeships.Count})");
                    }
                    catch (ExportException ex)
                    {
                        results.Add(DasProviderResult.Failed(p.UKPRN, ex));

                        _telemetryClient.TrackException(ex);
                        Console.WriteLine($"[{DateTime.UtcNow:G}][ERROR] Failed to export {p.UKPRN} ({p.Index} of {p.Apprenticeships.Count})");
                    }
                });

                timer.Stop();

                var success = results.Count(r => r.Success);
                var failure = results.Count(r => !r.Success);

                Console.WriteLine($"[{DateTime.UtcNow:G}] Exported {results.Count(r => r.Success)} Providers in {timer.Elapsed.TotalSeconds} seconds.");

                if (failure > 0)
                {
                    Console.WriteLine($"[{DateTime.UtcNow:G}] [WARNING] Encountered {failure} errors that need attention");
                }

                evt.Metrics.TryAdd("Export elapsed time (ms)", timer.ElapsedMilliseconds);
                evt.Metrics.TryAdd("Export success", success);
                evt.Metrics.TryAdd("Export failures", failure);
                _telemetryClient.TrackEvent(evt);

                return results.OrderBy(r => r.UKPRN).ToList();
            }
            catch (Exception e)
            {
                throw new ProviderServiceException(e);
            }
        }

        private DasProvider ExportProvider(
            IEnumerable<Provider> providers,
            List<Apprenticeship> apprenticeships,
            int ukprn,
            int exportKey)
        {
            var evt = new EventTelemetry {Name = "ComposeProviderForExport"};

            evt.Properties.TryAdd("UKPRN", $"{ukprn}");
            evt.Metrics.TryAdd("ProviderApprenticeships", apprenticeships.Count);
            evt.Metrics.TryAdd("MatchingProviders", providers?.Count() ?? 0);

            if (!(providers?.Any() ?? false))
            {
                throw new ProviderNotFoundException(ukprn);
            }

            try
            {
                var dasProvider = _DASHelper.CreateDasProviderFromProvider(exportKey, providers.First());

                if (dasProvider != null)
                {
                    var apprenticeshipLocations = apprenticeships.Where(x => x.ApprenticeshipLocations != null)
                        .SelectMany(x => x.ApprenticeshipLocations).Distinct(new ApprenticeshipLocationSameAddress()).ToList();

                    var index = 1000;
                    var locationIndex = new Dictionary<string, ApprenticeshipLocation>(apprenticeshipLocations
                        .Where(x => x.RecordStatus == RecordStatus.Live)
                        .Select(x => new KeyValuePair<string, ApprenticeshipLocation>($"{exportKey:D4}{index++:D4}", x)));

                    if (locationIndex.Any(l => l.Value.National.HasValue && l.Value.National == true))
                        dasProvider.NationalProvider = true;

                    var exportStandards = apprenticeships.Where(x => x.StandardCode.HasValue);
                    var exportFrameworks = apprenticeships.Where(x => x.FrameworkCode.HasValue);

                    evt.Metrics.TryAdd("Provider Locations", locationIndex.Count());
                    evt.Metrics.TryAdd("Provider Standards", exportStandards.Count());
                    evt.Metrics.TryAdd("Provider Frameworks", exportFrameworks.Count());

                    dasProvider.Locations = _DASHelper.ApprenticeshipLocationsToLocations(exportKey, locationIndex);
                    dasProvider.Standards = _DASHelper.ApprenticeshipsToStandards(exportKey, exportStandards, locationIndex);
                    dasProvider.Frameworks = _DASHelper.ApprenticeshipsToFrameworks(exportKey, exportFrameworks, locationIndex);

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
