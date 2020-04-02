using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Settings;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Settings;
using Dfc.ProviderPortal.Packages;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services;
using LazyCache;
using Microsoft.ApplicationInsights;

namespace Dfc.Providerportal.FindAnApprenticeship.Helper
{
    // TODO: Polly Polly Polly!
    public class ReferenceDataServiceClient : IReferenceDataServiceClient
    {
        private readonly IReferenceDataService _service;
        private readonly IAppCache _cache;
        private readonly TelemetryClient _telemetryClient;
        private readonly IReferenceDataServiceSettings _settings;
        public ReferenceDataServiceClient(
            TelemetryClient telemetryClient, 
            IOptions<ReferenceDataServiceSettings> settings, 
            IAppCache cache,
            IReferenceDataService service)
        {
            Throw.IfNull(telemetryClient, nameof(telemetryClient));
            Throw.IfNull(settings, nameof(settings));
            Throw.IfNull(cache, nameof(cache));
            Throw.IfNull(service, nameof(service));

            _telemetryClient = telemetryClient;
            _settings = settings.Value;
            _cache = cache;
            _service = service;
        }

        public FeChoice GetFeChoicesByUKPRN(string UKPRN)
        {
            try
            {
                var validUkprn = int.Parse(UKPRN);
                return this.GetAllFeChoiceData().SingleOrDefault(x => x.UKPRN == validUkprn);
            }
            catch (Exception e)
            {
                throw new ReferenceDataServiceException(UKPRN, e);
            }
        }

        public IEnumerable<FeChoice> GetAllFeChoiceData()
        {
            Func<IEnumerable<FeChoice>> feChoicesGetter = () => _service.GetAllFeChoices();

            try
            {
                return _cache.GetOrAdd("FeChoices", feChoicesGetter, DateTimeOffset.Now.AddHours(8));
            }
            catch (HttpRequestException e)
            {
                // add polly retry
                throw new ReferenceDataServiceException(e);
            }
            catch (Exception e)
            {
                throw new ReferenceDataServiceException(e);
            }
        }
    }
}
