using System;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Settings;
using Dfc.Providerportal.FindAnApprenticeship.Models.Providers;
using Dfc.ProviderPortal.Packages;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Settings;
using LazyCache;
using Microsoft.Extensions.Options;

namespace Dfc.Providerportal.FindAnApprenticeship.Helper
{
    public class ProviderServiceWrapper : IProviderServiceWrapper
    {
        private readonly IProviderService _client;
        private readonly IAppCache _cache;
        private readonly IProviderServiceSettings _settings;

        public ProviderServiceWrapper(IOptions<ProviderServiceSettings> settings, 
            IAppCache cache,
            IProviderService client)
        {
            Throw.IfNull(settings, nameof(settings));
            Throw.IfNull(cache, nameof(cache));
            Throw.IfNull(client, nameof(client));

            _settings = settings.Value;
            _cache = cache;
            _client = client;
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
            Func<IEnumerable<Provider>> activeProvidersGetter = () => _client.GetActiveProviders();

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
