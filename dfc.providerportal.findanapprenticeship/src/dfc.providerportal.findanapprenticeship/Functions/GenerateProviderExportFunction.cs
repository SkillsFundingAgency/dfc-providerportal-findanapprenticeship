using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Models.DAS;
using Dfc.Providerportal.FindAnApprenticeship.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dfc.Providerportal.FindAnApprenticeship.Functions
{
    public class GenerateProviderExportFunction
    {
        private readonly IApprenticeshipService _apprenticeshipService;
        private readonly IBlobStorageClient _blobStorageClient;

        public GenerateProviderExportFunction(IApprenticeshipService apprenticeshipService, IBlobStorageClient blobStorageClient)
        {
            _apprenticeshipService = apprenticeshipService ?? throw new ArgumentNullException(nameof(apprenticeshipService));
            _blobStorageClient = blobStorageClient ?? throw new ArgumentNullException(nameof(blobStorageClient));
        }

        [FunctionName("GenerateProviderExport")]
        public Task Run([TimerTrigger("%GenerateProviderExportSchedule%")]TimerInfo timer, ILogger log, CancellationToken ct)
        {
            return GenerateProviderExport(log, ct);
        }

        [FunctionName("GenerateProviderExportOnDemand")]
        public async Task<IActionResult> RunOnDemand([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bulk/generate")] HttpRequest req, ILogger log, CancellationToken ct)
        {
            var result = await GenerateProviderExport(log, ct);

            if (!result.Success)
            {
                return new ObjectResult(result.Message)
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }

            return new OkObjectResult(result.Message);
        }

        private async Task<GenerateProviderExportResult> GenerateProviderExport(ILogger log, CancellationToken ct)
        {
            var exportKey = ExportKey.FromUtcNow();

            log.LogInformation($"Started generation of {{{nameof(exportKey)}}}.", exportKey);

            var results = default(IEnumerable<DasProviderResult>);
            try
            {
                var generateStopwatch = Stopwatch.StartNew();

                var apprenticeships = (List<Apprenticeship>)await _apprenticeshipService.GetLiveApprenticeships();

                results = await _apprenticeshipService.ApprenticeshipsToDasProviders(apprenticeships);

                generateStopwatch.Stop();

                log.LogInformation($"Completed generation of {{{nameof(exportKey)}}} in {generateStopwatch.Elapsed}.", exportKey);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Failed to generate {{{nameof(exportKey)}}}.", exportKey);
                return GenerateProviderExportResult.FailedToGenerate(exportKey);
            }

            try
            {
                log.LogInformation($"Started upload of {{{nameof(exportKey)}}}.", exportKey);

                var uploadStopwatch = Stopwatch.StartNew();

                var blobClient = _blobStorageClient.GetBlobClient(exportKey);

                var export = JsonConvert.SerializeObject(
                    results.Where(r => r.Success).Select(r => r.Result),
                    new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(export)))
                {
                    await blobClient.UploadAsync(stream, true, ct);
                }

                uploadStopwatch.Stop();

                log.LogInformation($"Completed upload of {{{nameof(exportKey)}}} in {uploadStopwatch.Elapsed}.", exportKey);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, $"Failed to upload {{{nameof(exportKey)}}}.", exportKey);
                return GenerateProviderExportResult.FailedToUpload(exportKey);
            }

            return GenerateProviderExportResult.Succeeded(exportKey, results.Count(r => r.Success));
        }

        private class GenerateProviderExportResult
        {
            public bool Success { get; }

            public string Message { get; }

            private GenerateProviderExportResult(bool success, string message)
            {
                Success = success;
                Message = message;
            }

            public static GenerateProviderExportResult Succeeded(string exportKey, int providerCount)
            {
                return new GenerateProviderExportResult(true, $"Successfully generated {exportKey} containing {providerCount} providers.");
            }

            public static GenerateProviderExportResult FailedToGenerate(string exportKey)
            {
                return new GenerateProviderExportResult(false, $"Failed to generate {exportKey}.");
            }

            public static GenerateProviderExportResult FailedToUpload(string exportKey)
            {
                return new GenerateProviderExportResult(false, $"Failed to upload {exportKey}.");
            }
        }
    }
}