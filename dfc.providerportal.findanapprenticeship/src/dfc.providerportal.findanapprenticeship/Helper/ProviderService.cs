﻿using System;
using System.Collections.Generic;
using System.Linq;
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

        public IEnumerable<Provider> GetActiveProviders()
        {
            return GetActiveProvidersAsync().GetAwaiter().GetResult();
        }

        public async Task<IEnumerable<Provider>> GetActiveProvidersAsync()
        {
            Console.WriteLine($"[{DateTime.UtcNow:G}] Cache missing or expired... Refreshing Active Providers cache");

            var response = await _httpClient.GetAsync($"GetActiveProviders");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            List<Provider> providers = JsonConvert.DeserializeObject<IEnumerable<Provider>>(json).ToList();

            Console.WriteLine($"[{DateTime.UtcNow:G}] Loaded {providers.Count} active Providers to cache");

            return providers;
        }
    }
}