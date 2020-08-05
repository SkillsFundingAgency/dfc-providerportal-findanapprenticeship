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
using Dfc.Providerportal.FindAnApprenticeship.Storage;
using Microsoft.Azure.WebJobs;
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
        public async Task Run([TimerTrigger("%GenerateProviderExportSchedule%")]TimerInfo timer, ILogger log, CancellationToken ct)
        {
            var exportKey = ExportKey.FromUtcNow();

            log.LogInformation($"Started generation of {{{nameof(exportKey)}}}.", exportKey);

            string export = null;
            try
            {
                var generateStopwatch = Stopwatch.StartNew();

                var apprenticeships = (List<Apprenticeship>)await _apprenticeshipService.GetLiveApprenticeships();

                export = JsonConvert.SerializeObject(
                    (await _apprenticeshipService.ApprenticeshipsToDasProviders(apprenticeships)).Where(r => r.Success).Select(r => r.Result),
                    new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

                generateStopwatch.Stop();

                log.LogInformation($"Completed generation of {{{nameof(exportKey)}}} in {generateStopwatch.Elapsed}.", exportKey);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Failed to generate {{{nameof(exportKey)}}}.", exportKey);
                return;
            }

            try
            {
                log.LogInformation($"Started upload of {{{nameof(exportKey)}}}.", exportKey);

                var uploadStopwatch = Stopwatch.StartNew();

                var blobClient = _blobStorageClient.GetBlobClient(exportKey);

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
            }
        }
    }
}