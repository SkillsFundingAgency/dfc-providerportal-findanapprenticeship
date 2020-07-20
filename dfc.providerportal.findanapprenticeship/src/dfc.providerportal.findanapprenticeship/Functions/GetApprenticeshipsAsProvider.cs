using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using System.Collections.Generic;
using LazyCache;

namespace Dfc.Providerportal.FindAnApprenticeship.Functions
{
    public class GetApprenticeshipsAsProvider
    {
        private readonly IAppCache _cache;
        private readonly IApprenticeshipService _apprenticeshipService;

        public GetApprenticeshipsAsProvider(IAppCache cache, IApprenticeshipService apprenticeshipService)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _apprenticeshipService = apprenticeshipService ?? throw new ArgumentNullException(nameof(apprenticeshipService));
        }

        [FunctionName("GetApprenticeshipsAsProvider")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bulk/providers")]HttpRequest req, ILogger log)
        {
            List<Apprenticeship> persisted = null;

            try
            {
                log.LogInformation($"[{DateTime.UtcNow:G}] Retrieving Apprenticeships...");
                
                persisted = (List<Apprenticeship>)await _apprenticeshipService.GetLiveApprenticeships();
                
                if (persisted == null)
                {
                    return new EmptyResult();
                }

                var providers = _cache.GetOrAdd("DasProviders", () => _apprenticeshipService.ApprenticeshipsToDasProviders(persisted), DateTimeOffset.Now.AddHours(8));
                
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