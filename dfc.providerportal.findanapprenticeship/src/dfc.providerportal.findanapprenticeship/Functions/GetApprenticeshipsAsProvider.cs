using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using LazyCache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dfc.Providerportal.FindAnApprenticeship.Functions
{
    public class GetApprenticeshipsAsProvider
    {
        private readonly IAppCache _cache;
        private readonly IApprenticeshipService _apprenticeshipService;
        private readonly IConfiguration _configuration;

        public GetApprenticeshipsAsProvider(IAppCache cache, IApprenticeshipService apprenticeshipService, IConfiguration configuration)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _apprenticeshipService = apprenticeshipService ?? throw new ArgumentNullException(nameof(apprenticeshipService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [FunctionName("GetApprenticeshipsAsProvider")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bulk/providers")]HttpRequest req, ILogger log)
        {
            try
            {
                var result = await _cache.GetOrAddAsync<(string ExportKey, string Value)>("DasProviders", async () =>
                {
                    log.LogInformation($"Retrieving Apprenticeships...");

                    var apprenticeships = (List<Apprenticeship>)await _apprenticeshipService.GetLiveApprenticeships();

                    if (apprenticeships == null)
                    {
                        throw new Exception($"{nameof(apprenticeships)} cannot be null.");
                    }

                    var providersExportKey = $"providers-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.json";
                    var providers = JsonConvert.SerializeObject(_apprenticeshipService.ApprenticeshipsToDasProviders(apprenticeships), new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

                    await UploadProvidersExport(providersExportKey, providers, log);

                    return (providersExportKey, providers);
                }, DateTimeOffset.Now.AddHours(8));

                log.LogInformation($"Returning {{{nameof(result.ExportKey)}}}.", result.ExportKey);

                return new ContentResult
                {
                    Content = result.Value,
                    ContentType = "application/json",
                    StatusCode = StatusCodes.Status200OK
                };
            } 
            catch (Exception ex)
            {
                log.LogError(ex, $"{nameof(Run)} failed with exception.");

                return new ObjectResult(ex)
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        private async Task UploadProvidersExport(string providersExportKey, string providers, ILogger log)
        {
            try
            {
                log.LogInformation($"Started upload of {{{nameof(providersExportKey)}}}.", providersExportKey);

                var uploadStopwatch = new Stopwatch();
                uploadStopwatch.Start();

                var blobContainerClient = new BlobContainerClient(_configuration.GetValue<string>("AzureWebJobsStorage"), "fatp-providersexport");
                await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                var blobClient = blobContainerClient.GetBlobClient(providersExportKey);

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(providers)))
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                {
                    await blobClient.UploadAsync(stream, cts.Token);
                }

                uploadStopwatch.Stop();

                log.LogInformation($"Completed upload of {{{nameof(providersExportKey)}}} in {uploadStopwatch.Elapsed}.", providersExportKey);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, $"Failed to upload {{{nameof(providersExportKey)}}}.", providersExportKey);
            }
        }
    }
}