using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Settings;
using Dfc.Providerportal.FindAnApprenticeship.Models.Providers;
using Dfc.ProviderPortal.Packages;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using Dfc.Providerportal.FindAnApprenticeship.Settings;
using Microsoft.Extensions.Options;

namespace Dfc.Providerportal.FindAnApprenticeship.Helper
{
    public class ProviderServiceWrapper : IProviderServiceWrapper
    {
        private readonly IProviderServiceSettings _settings;

        public ProviderServiceWrapper(IOptions<ProviderServiceSettings> settings)
        {
            Throw.IfNull(settings, nameof(settings));
            
            _settings = settings.Value;
        }
        public IEnumerable<Provider> GetProviderByUKPRN(string UKPRN)
        {
            // Call service to get data
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _settings.ApiKey);
            var response = client.GetAsync($"{_settings.ApiUrl}GetProviderByPRN?PRN={UKPRN}").Result;
            if (response.IsSuccessStatusCode)
            {
                var json = response.Content.ReadAsStringAsync().Result;
                if (!json.StartsWith("["))
                    json = "[" + json + "]";
                client.Dispose();
                return JsonConvert.DeserializeObject<IEnumerable<Provider>>(json);
            }
            client.Dispose();
            return new List<Provider>();

        }
    }
}
