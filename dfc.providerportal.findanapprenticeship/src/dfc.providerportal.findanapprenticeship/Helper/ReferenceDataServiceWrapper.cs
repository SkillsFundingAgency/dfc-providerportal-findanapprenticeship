﻿using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Settings;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Settings;
using Dfc.ProviderPortal.Packages;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using LazyCache;
using Microsoft.ApplicationInsights;

namespace Dfc.Providerportal.FindAnApprenticeship.Helper
{
    // TODO: Polly Polly Polly!
    public class ReferenceDataServiceWrapper : IReferenceDataServiceWrapper
    {
        private readonly IAppCache _cache;
        private readonly TelemetryClient _telemetryClient;
        private readonly IReferenceDataServiceSettings _settings;
        public ReferenceDataServiceWrapper(
            TelemetryClient telemetryClient, 
            IOptions<ReferenceDataServiceSettings> settings, 
            IAppCache cache)
        {
            Throw.IfNull(telemetryClient, nameof(telemetryClient));
            Throw.IfNull(settings, nameof(settings));

            _telemetryClient = telemetryClient;
            _cache = cache;
            _settings = settings.Value;
        }

        public FeChoice GetFeChoicesByUKPRN(string UKPRN)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _settings.ApiKey);
                var response = client.GetAsync($"{_settings.ApiUrl}{UKPRN}").Result;

                response.EnsureSuccessStatusCode();

                var json = response.Content.ReadAsStringAsync().Result;

                if (string.IsNullOrWhiteSpace(json) || json.StartsWith("null"))
                {
                    return new FeChoice
                    {
                        UKPRN = int.Parse(UKPRN),
                        EmployerSatisfaction = null,
                        LearnerSatisfaction = null,
                        CreatedDateTimeUtc = DateTime.UtcNow
                    };
                }

                if (!json.StartsWith("["))
                    json = "[" + json + "]";

                var feChoicesResult = JsonConvert.DeserializeObject<IEnumerable<FeChoice>>(json);
                var latestScore = feChoicesResult.OrderByDescending(f => f.CreatedDateTimeUtc).FirstOrDefault();
                return latestScore;
            }
            catch (HttpRequestException e)
            {
                // add polly retry
                throw new ReferenceDataServiceException(UKPRN, e);
            }
            catch (Exception e)
            {
                throw new ReferenceDataServiceException(UKPRN, e);
            }
        }

        public IEnumerable<FeChoice> GetAllFeChoiceData()
        {
            Func<IEnumerable<FeChoice>> feChoicesGetter = () => PopulateFeChoicesCache();

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


        private IEnumerable<FeChoice> PopulateFeChoicesCache()
        {
            Console.WriteLine($"[{DateTime.UtcNow:u}] Refreshing FeChoices cache");

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _settings.ApiKey);
            var response = client.GetAsync($"{_settings.ApiUrl}").Result;

            response.EnsureSuccessStatusCode();

            var json = response.Content.ReadAsStringAsync().Result;
            List<FeChoice> feChoices = JsonConvert.DeserializeObject<IEnumerable<FeChoice>>(json).ToList();
            
            Console.WriteLine($"[{DateTime.UtcNow:u}] Loaded {feChoices.Count} FE Choices to cache");

            return feChoices;
        }
    }
}
