using Dfc.Providerportal.FindAnApprenticeship.Models.DAS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dfc.Providerportal.FindAnApprenticeship.Controller
{
    [Produces("application/json")]
    [Route("api")]
    [ApiController]
    public class DocController : ControllerBase
    {
        [Route("GetApprenticeshipsAsProvider")]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<DASProvider>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetUpdatedApprenticeshipsAsProvider()
        {
            return Ok();
        }

    }
}
