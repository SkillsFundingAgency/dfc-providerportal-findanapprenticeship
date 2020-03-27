using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Dfc.ProviderPortal.Packages.AzureFunctions.DependencyInjection;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using System.Collections.Generic;
using System.Linq;
using Dfc.Providerportal.FindAnApprenticeship.Models.DAS;
using LazyCache;

namespace Dfc.Providerportal.FindAnApprenticeship.Functions
{
    public static class GetApprenticeshipsAsProvider
    {
        [FunctionName("GetApprenticeshipsAsProvider")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bulk/providers")] HttpRequest req,
                                                    ILogger log,
                                                    [Inject] IAppCache cache,
                                                    [Inject] IApprenticeshipService apprenticeshipService)
        {
            List<Apprenticeship> persisted = null;

            try
            {
                Console.WriteLine($"[{DateTime.UtcNow:G}] Retrieving Apprenticeships...");
                
                persisted = (List<Apprenticeship>)await apprenticeshipService.GetLiveApprenticeships();
                if (persisted == null)
                    return new EmptyResult();

                Func<Task<List<DasProvider>>> dasProviderGetter = async () =>
                {
                    return apprenticeshipService.ApprenticeshipsToDasProviders(persisted) as List<DasProvider>;
                };
                
                var providers = cache.GetOrAdd("DasProviders", dasProviderGetter, DateTimeOffset.Now.AddHours(8));
                
                return new OkObjectResult(providers.Result);
            } 
            catch (Exception e)
            {
                return new InternalServerErrorObjectResult(e);
            }
        }
    }
    internal class InternalServerErrorObjectResult : ObjectResult
    {
        public InternalServerErrorObjectResult(object value) : base(value)
        {
            StatusCode = StatusCodes.Status500InternalServerError;
        }

        public InternalServerErrorObjectResult() : this(null)
        {
            StatusCode = StatusCodes.Status500InternalServerError;
        }
    }
}
