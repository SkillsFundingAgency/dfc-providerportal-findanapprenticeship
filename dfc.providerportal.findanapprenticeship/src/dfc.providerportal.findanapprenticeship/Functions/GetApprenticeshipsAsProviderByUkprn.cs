using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Dfc.Providerportal.FindAnApprenticeship.Functions
{
    public class GetApprenticeshipsAsProviderByUkprn
    {
        private readonly IApprenticeshipService _apprenticeshipService;

        public GetApprenticeshipsAsProviderByUkprn(IApprenticeshipService apprenticeshipService)
        {
            _apprenticeshipService = apprenticeshipService ?? throw new ArgumentNullException(nameof(apprenticeshipService));
        }

        [FunctionName("GetApprenticeshipsAsProviderByUkprn")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req, ILogger log)
        {
            string fromQuery = req.Query["ukprn"];

            if (string.IsNullOrWhiteSpace(fromQuery))
            {
                return new BadRequestObjectResult($"Empty or missing UKPRN value.");
            }

            if (!int.TryParse(fromQuery, out int ukprn))
            {
                return new BadRequestObjectResult($"Invalid UKPRN value, expected a valid integer");
            }

            try
            {
                log.LogInformation($"[{DateTime.UtcNow:G}] Retrieving Apprenticeships for {nameof(ukprn)} {{{nameof(ukprn)}}}...", ukprn);

                var persisted = (List<Apprenticeship>)await _apprenticeshipService.GetApprenticeshipsByUkprn(ukprn);
                
                if (persisted == null)
                {
                    return new EmptyResult();
                }
                    
                var providers = _apprenticeshipService.ApprenticeshipsToDasProviders(persisted);

                return new OkObjectResult(providers);
            }
            catch (Exception ex)
            {
                return new ObjectResult(ex)
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}