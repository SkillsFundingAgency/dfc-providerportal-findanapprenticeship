using System;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Settings;
using Dfc.Providerportal.FindAnApprenticeship.Models.Providers;
using Dfc.ProviderPortal.Packages;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Settings;
using LazyCache;
using Microsoft.Extensions.Options;

namespace Dfc.Providerportal.FindAnApprenticeship.Helper
{
    public class ProviderServiceClient : IProviderServiceClient
    {
        private readonly IProviderService _service;
        private readonly IAppCache _cache;
        private readonly IProviderServiceSettings _settings;

        public ProviderServiceClient(IOptions<ProviderServiceSettings> settings, 
            IAppCache cache,
            IProviderService service)
        {
            Throw.IfNull(settings, nameof(settings));
            Throw.IfNull(cache, nameof(cache));
            Throw.IfNull(service, nameof(service));

            _settings = settings.Value;
            _cache = cache;
            _service = service;
        }

        /// <summary>
        /// Mostly returns a single provider, but in some cases, we have multiple orgs with the same UKPRN. Quirky!
        /// </summary>
        /// <param name="UKPRN">The UKPRN to lookup</param>
        /// <returns>A list of matching providers.</returns>
        public IEnumerable<Provider> GetProviderByUKPRN(string UKPRN)
        {
            try
            {
                return this.GetAllProviders().Where(x => x.UnitedKingdomProviderReferenceNumber == UKPRN);
            }
            catch (Exception e)
            {
                throw new ProviderServiceException(UKPRN, e);
            }

        }

        public IEnumerable<Provider> GetAllProviders()
        {
            Func<IEnumerable<Provider>> activeProvidersGetter = () => _service.GetActiveProviders();

            try
            {
                return _cache.GetOrAdd("ActiveProviders", activeProvidersGetter, DateTimeOffset.Now.AddHours(8));
            }
            catch (HttpRequestException e)
            {
                // add polly retry
                throw new ProviderServiceException(e);
            }
            catch (Exception e)
            {
                throw new ProviderServiceException(e);
            }
        }
    }
}
