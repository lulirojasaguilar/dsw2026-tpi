using Dsw2026Tpi.Api.Configurations;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
        [EnableRateLimiting(RateLimitingConfigurationExtensions.AppointmentBookingPolicy)]
        [ProducesResponseType(typeof(AppointmentModel.Response), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
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
        [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentModel.Response>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByPatient(
            [FromQuery] long dni)
        {
            if (!TryGetPatientId(out var patientId))
            {
                throw new AuthorizationException(ErrorCodes.PATIENT_MISMATCH);
            }

            var appointments =
                await _service.GetByPatient(dni, patientId);

            return Ok(appointments);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Policies.PatientPolicy)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Cancel(
            [FromRoute] Guid id)
        {
            if (!TryGetPatientId(out var patientId))
            {
                throw new AuthorizationException(ErrorCodes.PATIENT_MISMATCH);
            }

            await _service.Cancel(
                id,
                patientId);

            return Content("ok", "text/plain");
        }

        [HttpGet]
        [Authorize(Policy = Policies.AdminPolicy)]
        [ProducesResponseType(typeof(Pagination<AppointmentModel.Response>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByDate(
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
        [ProducesResponseType(typeof(Pagination<AppointmentModel.Response>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(
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
