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

namespace Dfc.Providerportal.FindAnApprenticeship.Functions
{
    public static class GetApprenticeshipsAsProvider
    {
        [FunctionName("GetApprenticeshipsAsProvider")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
                                                    ILogger log,
                                                    [Inject] IApprenticeshipService apprenticeshipService)
        {
            List<Apprenticeship> persisted = null;


            try
            {
                persisted = (List<Apprenticeship>)await apprenticeshipService.GetApprenticeshipCollection();
                if (persisted == null)
                    return new EmptyResult();
                var providers = apprenticeshipService.ApprenticeshipsToDASProviders(persisted);
                return new OkObjectResult(providers);

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
