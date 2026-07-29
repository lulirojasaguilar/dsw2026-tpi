using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers
{
    [Route("api/appointments")]
    public class AppointmentsController : AppController
    {
        private readonly IAppointmentService _service;

        public AppointmentsController(IAppointmentService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Policy = Policies.PatientPolicy)]
        [ProducesResponseType(
            typeof(AppointmentModel.Response),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AppointmentModel.Response>>
            Create(
        [FromBody] AppointmentModel.Request request)
        {
            var appointment =
                await _service.Create(request);

            return StatusCode(
                StatusCodes.Status201Created,
                appointment);
        }

        [HttpGet("patient")]
        [Authorize(Policy = Policies.PatientPolicy)]
        [ProducesResponseType(
            typeof(IReadOnlyCollection<AppointmentModel.Response>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<
            ActionResult<
                IReadOnlyCollection<AppointmentModel.Response>>>
            GetByPatient(
                [FromQuery] long dni)
        {
            var appointments =
                await _service.GetByPatient(dni);

            return Ok(appointments);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Policies.PatientPolicy)]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Cancel(
            [FromRoute] Guid id)
        {
            if (!TryGetPatientId(out var patientId))
            {
                return Forbid();
            }

            await _service.Cancel(
                id,
                patientId);

            return NoContent();
        }

        [HttpGet]
        [Authorize(Policy = Policies.AdminPolicy)]
        [ProducesResponseType(
            typeof(Pagination<AppointmentModel.Response>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<
            ActionResult<
                Pagination<AppointmentModel.Response>>>
            GetByDate(
                [FromQuery] DateOnly date,
                [FromQuery] int pageSize = 10,
                [FromQuery] int pageIndex = 0)
        {
            var appointments =
                await _service.GetByDate(
                    date,
                    pageSize,
                    pageIndex);

            return Ok(appointments);
        }

        [HttpGet("search")]
        [Authorize(Policy = Policies.AdminPolicy)]
        [ProducesResponseType(
            typeof(Pagination<AppointmentModel.Response>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<
            ActionResult<
                Pagination<AppointmentModel.Response>>>
            Search(
                [FromQuery] int pageSize = 10,
                [FromQuery] int pageIndex = 0,
                [FromQuery] Guid? specialtyId = null,
                [FromQuery] Guid? doctorId = null,
                [FromQuery] long? dni = null,
                [FromQuery] DateOnly? date = null)
        {
            var appointments =
                await _service.Search(
                    pageSize,
                    pageIndex,
                    specialtyId,
                    doctorId,
                    dni,
                    date);

            return Ok(appointments);
        }

        private bool TryGetPatientId(
            out Guid patientId)
        {
            var patientIdClaim =
                User.FindFirst("patientId")?.Value;

            return Guid.TryParse(
                patientIdClaim,
                out patientId);
        }
    }
}
