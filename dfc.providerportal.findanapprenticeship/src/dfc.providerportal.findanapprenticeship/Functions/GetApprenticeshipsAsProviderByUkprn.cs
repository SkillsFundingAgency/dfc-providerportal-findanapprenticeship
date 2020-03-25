using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.ProviderPortal.Packages.AzureFunctions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Dfc.Providerportal.FindAnApprenticeship.Functions
{
    public static class GetApprenticeshipsAsProviderByUkprn
    {
        [FunctionName("GetApprenticeshipsAsProviderByUkprn")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log,
            [Inject] IApprenticeshipService apprenticeshipService)
        {
            string fromQuery = req.Query["ukprn"];

            if (string.IsNullOrWhiteSpace(fromQuery))
                return new BadRequestObjectResult($"Empty or missing UKPRN value.");

            if (!int.TryParse(fromQuery, out int ukprn))
                return new BadRequestObjectResult($"Invalid UKPRN value, expected a valid integer");


            try
            {
                var persisted = (List<Apprenticeship>)await apprenticeshipService.GetApprenticeshipsByUkprn(ukprn);
                if (persisted == null)
                    return new EmptyResult();
                var providers = apprenticeshipService.ApprenticeshipsToDasProviders(persisted);
                return new OkObjectResult(providers);

            }
            catch (Exception e)
            {
                return new InternalServerErrorObjectResult(e);
            }
        }
    }
}