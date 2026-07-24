using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/specialties")]
[Authorize(Policy = Policies.AdminPolicy)]
   public class SpecialityController : AppController
   {
       private readonly ISpecialityService _service;
       public SpecialityController(ISpecialityService service)
       {
           _service = service;
       }

       [HttpGet]
       [ProducesResponseType(StatusCodes.Status200OK)]
       public async Task<IActionResult> GetAll([FromQuery] int pageSize, [FromQuery] int pageIndex, [FromQuery] string? name = null)
       {
        if (!string.IsNullOrWhiteSpace(name) && (name.Length < 3 || name.Length > 100))
        {
            throw new ValidationException("El nombre debe tener entre 3 y 100 caracteres.", ErrorCodes.VALIDATION_ERROR).WithDetail("name", "Debe tener entre 3 y 100 caracteres.");
        }
        var specialities = await _service.GetAll(pageSize, pageIndex, name);
           return Ok(specialities);
       }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] SpecialityModel.Request request)
    {
        var speciality = await _service.Create(request);

        return StatusCode(StatusCodes.Status201Created, speciality);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
    [FromRoute] Guid id,
    [FromBody] SpecialityModel.Request request)
    {
        var speciality = await _service.Update(id, request);

        return Ok(speciality);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        await _service.Delete(id);

        return NoContent();
    }
}