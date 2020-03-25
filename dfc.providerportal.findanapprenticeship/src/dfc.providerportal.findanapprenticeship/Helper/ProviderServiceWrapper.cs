using System;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Settings;
using Dfc.Providerportal.FindAnApprenticeship.Models.Providers;
using Dfc.ProviderPortal.Packages;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Dfc.Providerportal.FindAnApprenticeship.Settings;
using LazyCache;
using Microsoft.Extensions.Options;

namespace Dfc.Providerportal.FindAnApprenticeship.Helper
{
    public class ProviderServiceWrapper : IProviderServiceWrapper
    {
        private readonly IAppCache _cache;
        private readonly IProviderServiceSettings _settings;

        public ProviderServiceWrapper(IOptions<ProviderServiceSettings> settings, 
            IAppCache cache)
        {
            Throw.IfNull(cache, nameof(cache));
            Throw.IfNull(settings, nameof(settings));

            _cache = cache;
            _settings = settings.Value;
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
                throw new ReferenceDataServiceException(UKPRN, e);
            }

        }

        public IEnumerable<Provider> GetAllProviders()
        {
            Func<IEnumerable<Provider>> activeProvidersGetter = () => PopulateActiveProvidersCache();

            try
            {
                return _cache.GetOrAdd("ActiveProviders", activeProvidersGetter, DateTimeOffset.Now.AddHours(8));
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


        private IEnumerable<Provider> PopulateActiveProvidersCache()
        {
            Console.WriteLine($"[{DateTime.UtcNow:G}] Cache missing or expired... Refreshing Active Providers cache");

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _settings.ApiKey);
            var response = client.GetAsync($"{_settings.ApiUrl}GetActiveProviders").Result;

            response.EnsureSuccessStatusCode();

            var json = response.Content.ReadAsStringAsync().Result;
            List<Provider> providers = JsonConvert.DeserializeObject<IEnumerable<Provider>>(json).ToList();

            Console.WriteLine($"[{DateTime.UtcNow:G}] Loaded {providers.Count} active Providers to cache");

            return providers;
        }
    }
}
