using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Newtonsoft.Json;

namespace Dfc.Providerportal.FindAnApprenticeship.Helper
{
    public class ReferenceDataService : IReferenceDataService
    {
        private readonly HttpClient _httpClient;

        public ReferenceDataService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public IEnumerable<FeChoice> GetAllFeChoices()
        {
            return GetAllFeChoicesAsync().Result;
        }

        public async Task<IEnumerable<FeChoice>> GetAllFeChoicesAsync()
        {
            Console.WriteLine($"[{DateTime.UtcNow:G}] Cache missing or expired... Refreshing FeChoices cache");

            // TODO: Request config changes from devops to remove 'FeChoices' from the base URL
            var response = await _httpClient.GetAsync("");

            response.EnsureSuccessStatusCode();

            var json = response.Content.ReadAsStringAsync().Result;
            List<FeChoice> feChoices = JsonConvert.DeserializeObject<IEnumerable<FeChoice>>(json).ToList();

            Console.WriteLine($"[{DateTime.UtcNow:G}] Loaded {feChoices.Count} FE Choices to cache");

            return feChoices;
        }
    }
}