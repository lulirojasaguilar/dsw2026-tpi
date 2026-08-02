using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Constants;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services
{

    public class AvailabilityService : IAvailabilityService
    {
        private readonly IPersistence _persistence;
        private readonly AvailabilityGenerator _generator;
        private readonly ILogger<AvailabilityService> _logger;

        public AvailabilityService(
            IPersistence persistence,
            AvailabilityGenerator generator,
            ILogger<AvailabilityService> logger)
        {
            _persistence = persistence;
            _generator = generator;
            _logger = logger;
        }

        public Task<AvailabilityModel.Response> CreateAvailabilityAsync(AvailabilityModel.Request request) =>
            BuildAvailabilityAsync(request, overwrite: false);

        public Task<AvailabilityModel.Response> UpdateAvailabilityAsync(AvailabilityModel.Request request) =>
            BuildAvailabilityAsync(request, overwrite: true);

        private async Task<AvailabilityModel.Response> BuildAvailabilityAsync(AvailabilityModel.Request request, bool overwrite)
        {
            ValidateRequest(request);

            var doctor = await _persistence.GetById<Doctor>(request.DoctorId)
                ?? throw new EntityNotFoundException(ErrorCodes.DOCTOR_NOT_FOUND, nameof(ErrorCodes.DOCTOR_NOT_FOUND));

            if (doctor.Deleted)
            {
                throw new EntityNotFoundException(ErrorCodes.DOCTOR_NOT_FOUND, nameof(ErrorCodes.DOCTOR_NOT_FOUND));
            }

            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);
            var month = (byte)today.Month;
            var year = (short)today.Year;

            var parsedDays = request.Days
                .Select(d => new
                {
                    DayOfWeek = (byte)AvailabilityGenerator.ParseDay(d.Day),
                    StartTime = AvailabilityGenerator.ParseTime(d.StartTime, "startTime"),
                    EndTime = AvailabilityGenerator.ParseTime(d.EndTime, "endTime")
                })
                .ToList();

            foreach (var day in parsedDays)
            {
                AvailabilityGenerator.ValidateRange(day.StartTime, day.EndTime);
            }

            ValidateNoOverlapsWithinRequest(parsedDays.Select(d => (d.DayOfWeek, d.StartTime, d.EndTime)));

            var existingRules = (await _persistence.GetFiltered<AvailabilityRule>(
                r => r.DoctorId == doctor.Id && r.Month == month && r.Year == year && !r.Deleted))?.ToList() ?? [];

            if (overwrite)
            {
                await OverwriteExistingMonthAsync(existingRules,today, now.TimeOfDay, now);
            }
            else
            {
                ValidateNoOverlapsWithExisting(parsedDays.Select(d => (d.DayOfWeek, d.StartTime, d.EndTime)), existingRules);
            }

            var createdRules = new List<AvailabilityRule>();
            var createdSlots = new List<AvailabilitySlot>();

            foreach (var day in parsedDays)
            {
                var rule = new AvailabilityRule(doctor, month, year, day.DayOfWeek, day.StartTime, day.EndTime);
                rule.CreatedAt = now;
                rule.UpdatedAt = now;

                await _persistence.Add(rule);
                createdRules.Add(rule);

                var slots = _generator.GenerateSlotsForMonth(rule, today, now.TimeOfDay);

                foreach (var slot in slots)
                {
                    slot.CreatedAt = now;
                    slot.UpdatedAt = now;
                    await _persistence.Add(slot);
                }

                createdSlots.AddRange(slots);
            }

            await ExecuteWithFriendlyConflictAsync(() => _persistence.SaveChangesAsync());

            _logger.LogInformation(
                "Disponibilidad {Action} para DoctorId={DoctorId}: {RulesCreated} regla(s), {SlotsCreated} slot(s) ({Month}/{Year}).",
                overwrite ? "actualizada" : "creada", doctor.Id, createdRules.Count, createdSlots.Count, month, year);

            return new AvailabilityModel.Response(
                doctor.Id,
                month,
                year,
                createdRules.Count,
                createdSlots.Count,
                createdRules.Select(r => new AvailabilityModel.RuleSummary(
                    r.Id,
                    AvailabilityGenerator.DayName(r.DayOfWeek),
                    r.StartTime.ToString(@"hh\:mm"),
                    r.EndTime.ToString(@"hh\:mm"))).ToList());
        }

        private async Task OverwriteExistingMonthAsync(List<AvailabilityRule> existingRules, DateOnly today, TimeSpan nowTime, DateTime now)
        {
            if (existingRules.Count == 0)
            {
                return;
            }

            var ruleIds = existingRules.Select(r => r.Id).ToHashSet();

            var existingSlots = (await _persistence.GetFiltered<AvailabilitySlot>(
                s => ruleIds.Contains(s.AvailabilityRuleId) && !s.Deleted))?.ToList() ?? [];

            foreach (var slot in existingSlots)
            {
                var isFuture = slot.SlotDate > today || (slot.SlotDate == today && slot.StartTime > nowTime);
                var isBooked = slot.Status == AvailabilityStatuses.Booked;

                if (!isFuture || isBooked)
                {
                    continue; 
                }

                slot.Delete();
                slot.UpdatedAt = now;
                await _persistence.Update(slot);
            }

            foreach (var rule in existingRules)
            {
                rule.Delete();
                rule.UpdatedAt = now;
                await _persistence.Update(rule);
            }
        }

        private static void ValidateRequest(AvailabilityModel.Request request)
        {
            if (request.DoctorId == Guid.Empty)
            {
                throw new ValidationException("El médico indicado no es válido.", nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail("doctorId", "doctorId es obligatorio.");
            }

            if (request.Days is null || request.Days.Count == 0)
            {
                throw new ValidationException("Los días indicados no son válidos.", nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail("days", "days debe contener al menos un elemento.");
            }
        }

        private static void ValidateNoOverlapsWithinRequest(IEnumerable<(byte DayOfWeek, TimeSpan Start, TimeSpan End)> days)
        {
            var byDay = days.GroupBy(d => d.DayOfWeek);

            foreach (var group in byDay)
            {
                var ordered = group.OrderBy(d => d.Start).ToList();

                for (var i = 1; i < ordered.Count; i++)
                {
                    if (ordered[i].Start < ordered[i - 1].End)
                    {
                        throw new ConflictException(ErrorCodes.SCHEDULE_OVERLAP, nameof(ErrorCodes.SCHEDULE_OVERLAP))
                            .WithDetail("days", "Hay horarios superpuestos para el mismo día en la solicitud.");
                    }
                }
            }
        }

        private static void ValidateNoOverlapsWithExisting(
            IEnumerable<(byte DayOfWeek, TimeSpan Start, TimeSpan End)> days,
            List<AvailabilityRule> existingRules)
        {
            foreach (var day in days)
            {
                var overlaps = existingRules.Any(r =>
                    r.DayOfWeek == day.DayOfWeek && day.Start < r.EndTime && r.StartTime < day.End);

                if (overlaps)
                {
                    throw new ConflictException(ErrorCodes.SCHEDULE_OVERLAP, nameof(ErrorCodes.SCHEDULE_OVERLAP));
                }
            }
        }

        private static async Task ExecuteWithFriendlyConflictAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (DbUpdateException ex) when (DbConflictHelper.IsUniqueConstraintViolation(ex))
            {
                throw new ConflictException(ErrorCodes.SCHEDULE_OVERLAP, nameof(ErrorCodes.SCHEDULE_OVERLAP))
                    .WithDetail("schedule", "El horario ya fue registrado por otra solicitud concurrente.");
            }
        }
    }
}