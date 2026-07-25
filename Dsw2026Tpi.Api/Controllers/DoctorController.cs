using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("doctors")]
[Authorize(Policy = Policies.AdminPolicy)]
public class DoctorController : AppController
{
    private readonly IDoctorService _service;

    public DoctorController(IDoctorService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery]int pageSize, [FromQuery]int pageIndex, [FromQuery]string? name = null)
    {
        if (!string.IsNullOrWhiteSpace(name) && (name.Length < 3 || name.Length > 100))
        {
            throw new ValidationException("El nombre debe tener entre 3 y 100 caracteres.", ErrorCodes.VALIDATION_ERROR).WithDetail("name", "Debe tener entre 3 y 100 caracteres.");
        }
        var doctors = await _service.GetAll(pageSize, pageIndex, name);
        return Ok(doctors);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] DoctorModel.Request request)
    {
        var doctor = await _service.Create(request);

        return StatusCode(StatusCodes.Status201Created, doctor);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] DoctorModel.Request request)
    {
        var doctor = await _service.Update(id, request);

        return Ok(doctor);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.Delete(id);

        return NoContent();
    }

    [HttpGet("{id:guid}/availabilities")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAvailabilities(Guid id)
    {
        return Ok(Array.Empty<DoctorModel.AvailabilityResponse>());
    }
}
