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
    public class ReferenceDataServiceWrapper : IReferenceDataServiceWrapper
    {
        private readonly IReferenceDataService _client;
        private readonly IAppCache _cache;
        private readonly TelemetryClient _telemetryClient;
        private readonly IReferenceDataServiceSettings _settings;
        public ReferenceDataServiceWrapper(
            TelemetryClient telemetryClient, 
            IOptions<ReferenceDataServiceSettings> settings, 
            IAppCache cache,
            IReferenceDataService client)
        {
            Throw.IfNull(telemetryClient, nameof(telemetryClient));
            Throw.IfNull(settings, nameof(settings));
            Throw.IfNull(cache, nameof(cache));
            Throw.IfNull(client, nameof(client));

            _telemetryClient = telemetryClient;
            _settings = settings.Value;
            _cache = cache;
            _client = client;
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
            Func<IEnumerable<FeChoice>> feChoicesGetter = () => _client.GetAllFeChoices();

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
