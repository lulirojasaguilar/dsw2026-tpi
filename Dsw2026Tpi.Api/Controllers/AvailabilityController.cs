using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[ApiController]
[Route("api/availabilities")]
[Authorize(Policy = Policies.AdminPolicy)]
public class AvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _availabilityService;

    public AvailabilityController(IAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAvailabilityRule([FromBody] CreateAvailabilityRequest request)
    {
        try
        {
            var startTime = TimeSpan.Parse(request.StartTime);
            var endTime = TimeSpan.Parse(request.EndTime);

            var rule = await _availabilityService.CreateAvailabilityRuleAsync(
                request.DoctorId,
                request.Month,
                request.Year,
                request.DayOfWeek,
                startTime,
                endTime);

            return Ok(new { Message = "Disponibilidad generada con exito", RuleId = rule.Id });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("SCHEDULE_OVERLAP"))
        {
            return Conflict(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }


    [HttpPut]
    public async Task<IActionResult> UpdateAvailabilityRule([FromBody] CreateAvailabilityRequest request)
    {
        try
        {
            var startTime = TimeSpan.Parse(request.StartTime);
            var endTime = TimeSpan.Parse(request.EndTime);

            var rule = await _availabilityService.UpdateAvailabilityRuleAsync(request.DoctorId,
            request.Month,
            request.Year,
            request.DayOfWeek,
            startTime,
            endTime);

            return Ok(new { Message = "Disponibilidad sobreescrita con exito", RuleId = rule.Id });

        }
        catch(InvalidOperationException ex ) when (ex.Message.StartsWith("SCHEDULE_OVERLAP"))
        {
            return Conflict(new {Error = ex.Message });
        }
        catch (BusinessRuleException ex ) when (ex.Message.Contains("UPDATE_CONFLICT"))
        {
            return Conflict(new {Error = ex.Message});
        }
        catch (Exception ex)
        {
            return BadRequest(new {Error = ex.Message});
        }
    }

    public record CreateAvailabilityRequest(
        Guid DoctorId,
        int Month,
        int Year,
        int DayOfWeek,
        string StartTime,
        string EndTime


        );
}