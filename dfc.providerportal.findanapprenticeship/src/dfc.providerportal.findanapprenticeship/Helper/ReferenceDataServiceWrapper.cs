using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Settings;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Settings;
using Dfc.ProviderPortal.Packages;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Dfc.Providerportal.FindAnApprenticeship.Helper
{
    public class ReferenceDataServiceWrapper : IReferenceDataServiceWrapper
    {
        private readonly IReferenceDataServiceSettings _settings;
        public ReferenceDataServiceWrapper(IOptions<ReferenceDataServiceSettings> settings)
        {
            Throw.IfNull(settings, nameof(settings));
            _settings = settings.Value;
        }
        public IEnumerable<FeChoice> GetFeChoicesByUKPRN(string UKPRN)
        {
            // Call service to get data
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _settings.ApiKey);
            var response = client.GetAsync($"{_settings.ApiUrl}{UKPRN}").Result;
            if (response.IsSuccessStatusCode)
            {
                var json = response.Content.ReadAsStringAsync().Result;
                if(string.IsNullOrWhiteSpace(json) || json.StartsWith("null"))
                {
                    return new List<FeChoice>
                    {
                        new FeChoice
                        {
                            EmployerSatisfaction = 0.0,
                            LearnerSatisfaction = 0.0
                        }
                    };
                }
                if (!json.StartsWith("["))
                    json = "[" + json + "]";

                return JsonConvert.DeserializeObject<IEnumerable<FeChoice>>(json);
            }
            return new List<FeChoice>();

        }
    }
}
