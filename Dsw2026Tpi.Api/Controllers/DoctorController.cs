using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/doctors")]
[Authorize]
public class DoctorController : AppController
{
    private readonly IDoctorService _service;

    public DoctorController(IDoctorService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Pagination<DoctorModel.Response>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery]int pageSize = 10, 
        [FromQuery]int pageIndex = 0, 
        [FromQuery]string? name = null, 
        [FromQuery] Guid? specialtyId = null)
    {
        var doctors = await _service.GetAll(
            pageSize,
            pageIndex,
            name,
            specialtyId);

        return Ok(doctors);
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(typeof(DoctorModel.Response), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]

    public async Task<IActionResult> Create(
        [FromBody] DoctorModel.Request request)
    {
        var doctor = await _service.Create(request);

        return StatusCode(StatusCodes.Status201Created, doctor);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(typeof(DoctorModel.Response), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]

    public async Task<IActionResult> Update(
        [FromRoute] Guid id, 
        [FromBody] DoctorModel.Request request)
    {
        var doctor = await _service.Update(id, request);

        return Ok(doctor);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id)
    {
        await _service.Delete(id);

        return Content("ok", "text/plain");
    }

    [HttpGet("{id:guid}/availabilities")]
    [ProducesResponseType(typeof(IReadOnlyCollection<DoctorModel.AvailabilityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailabilities(
        [FromRoute] Guid id,
        [FromQuery] byte? month = null,
        [FromQuery] short? year = null)
    {
        var availabilities = await _service.GetAvailabilities(id, month, year);

        return Ok(availabilities);
    }
}
