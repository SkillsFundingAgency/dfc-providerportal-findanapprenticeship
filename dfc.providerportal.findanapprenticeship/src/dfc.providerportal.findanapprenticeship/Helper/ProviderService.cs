using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services;
using Dfc.Providerportal.FindAnApprenticeship.Models.Providers;
using Newtonsoft.Json;

namespace Dfc.Providerportal.FindAnApprenticeship.Helper
{
    public class ProviderService : IProviderService
    {
        private readonly HttpClient _httpClient;

        public ProviderService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<Provider>> GetActiveProvidersAsync()
        {
            var response = await _httpClient.GetAsync($"GetActiveProviders");

            response.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<IEnumerable<Provider>>(
                await response.Content.ReadAsStringAsync());
        }
    }
}