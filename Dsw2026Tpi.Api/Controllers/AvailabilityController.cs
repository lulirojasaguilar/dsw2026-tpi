using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers
{

    [Authorize(Policy = Policies.AdminPolicy)]
    [Route("availabilities")]
 
    public class AvailabilityController : AppController
    {
        private readonly IAvailabilityService _availabilityService;

        public AvailabilityController(IAvailabilityService availabilityService)
        {
            _availabilityService = availabilityService;
        }

       
        [HttpPost]
        [ProducesResponseType(typeof(AvailabilityModel.Response), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateAvailability([FromBody] AvailabilityModel.Request request)
        {
            var result = await _availabilityService.CreateAvailabilityAsync(request);
            return StatusCode( StatusCodes.Status201Created, result);
            
        }

        [HttpPut]
        [ProducesResponseType(typeof(AvailabilityModel.Response), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateAvailability([FromBody] AvailabilityModel.Request request)
        {
            var result = await _availabilityService.UpdateAvailabilityAsync(request);
            return Ok(result);
        }
    }
}