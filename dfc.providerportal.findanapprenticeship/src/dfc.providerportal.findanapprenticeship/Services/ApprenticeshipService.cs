using Dfc.Providerportal.FindAnApprenticeship.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Apprenticeships;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.DAS;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Models;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Settings;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Models.DAS;
using Dfc.Providerportal.FindAnApprenticeship.Models.Enums;
using Dfc.Providerportal.FindAnApprenticeship.Models.Providers;
using Dfc.Providerportal.FindAnApprenticeship.Settings;
using Dfc.ProviderPortal.Packages;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;

namespace Dfc.Providerportal.FindAnApprenticeship.Services
{
    public class ApprenticeshipService : IApprenticeshipService
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ICosmosDbHelper _cosmosDbHelper;
        private readonly IDASHelper _DASHelper;
        private readonly ICosmosDbCollectionSettings _settings;
        private readonly IProviderServiceSettings _providerServiceSettings;


        public ApprenticeshipService(
            TelemetryClient telemetryClient,
            ICosmosDbHelper cosmosDbHelper,
            IDASHelper DASHelper,
            IOptions<CosmosDbCollectionSettings> settings,
            IOptions<ProviderServiceSettings> providerServiceSettings)
        {
            Throw.IfNull(telemetryClient, nameof(telemetryClient));
            Throw.IfNull(cosmosDbHelper, nameof(cosmosDbHelper));
            Throw.IfNull(DASHelper, nameof(DASHelper));
            Throw.IfNull(settings, nameof(settings));
            Throw.IfNull(providerServiceSettings, nameof(providerServiceSettings));

            _telemetryClient = telemetryClient;
            _cosmosDbHelper = cosmosDbHelper;
            _DASHelper = DASHelper;
            _settings = settings.Value;
            _providerServiceSettings = providerServiceSettings.Value;
        }

        public async Task<IEnumerable<IApprenticeship>> GetApprenticeshipCollection()
        {
            using (var client = _cosmosDbHelper.GetClient())
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
            using (var client = _cosmosDbHelper.GetClient())
            {
                await _cosmosDbHelper.CreateDatabaseIfNotExistsAsync(client);
                await _cosmosDbHelper.CreateDocumentCollectionIfNotExistsAsync(client, _settings.ApprenticeshipCollectionId);

                var docs = _cosmosDbHelper.GetApprenticeshipByUKPRN(client, _settings.ApprenticeshipCollectionId, ukprn);
                persisted = docs;
            }

            return persisted;
        }

        public IEnumerable<IDASProvider> ApprenticeshipsToDASProviders(List<Apprenticeship> apprenticeships)
        {
            List<DASProvider> providers = new List<DASProvider>();
            List<string> listOfProviderUKPRN = new List<string>();

            listOfProviderUKPRN = apprenticeships.Select(x => x.ProviderUKPRN.ToString())
                                                 .Distinct()
                                                 .ToList();
            foreach (var ukprn in listOfProviderUKPRN)
            {
                var providerApprenticeships = apprenticeships.Where(x => x.ProviderUKPRN.ToString() == ukprn && x.RecordStatus == RecordStatus.Live).ToList();

                var providerDetailsList = GetProviderDetails(ukprn);
                if (providerDetailsList != null && providerDetailsList.Count() > 0)
                {

                    var DASProvider = _DASHelper.CreateDASProviderFromProvider(providerDetailsList.FirstOrDefault());
                    var apprenticeshipLocations = providerApprenticeships.Where(x => x.ApprenticeshipLocations != null)
                                                 .SelectMany(x => x.ApprenticeshipLocations);

                    DASProvider.Locations = _DASHelper.ApprenticeshipLocationsToLocations(apprenticeshipLocations.Where(x => x.RecordStatus == RecordStatus.Live));
                    DASProvider.Standards = _DASHelper.ApprenticeshipsToStandards(providerApprenticeships.Where(x => x.StandardCode.HasValue));
                    DASProvider.Frameworks = _DASHelper.ApprenticeshipsToFrameworks(providerApprenticeships.Where(x => x.FrameworkCode.HasValue));
                    providers.Add(DASProvider);
                }
            }
            return providers;
        }
        internal IEnumerable<Provider> GetProviderDetails(string UKPRN)
        {
            return new ProviderServiceWrapper(_providerServiceSettings).GetProviderByUKPRN(UKPRN);
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
