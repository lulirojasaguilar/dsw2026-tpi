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

            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);
            var month = (byte)today.Month;
            var year = (short)today.Year;

            // Se valida y convierte lo que llega antes de ir a la base
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

            // Una sola consulta: el médico con sus disponibilidades vigentes del mes (las eliminadas no se traen)
            // y los turnos vigentes de cada una.
            var doctor = await _persistence.FirstWith<Doctor>(
                d => d.Id == request.DoctorId && !d.Deleted,
                query => query
                    .Include(d => d.Availabilities.Where(r => !r.Deleted && r.Month == month && r.Year == year))
                    .ThenInclude(r => r.Slots.Where(s => !s.Deleted)))
                ?? throw new EntityNotFoundException(ErrorCodes.DOCTOR_NOT_FOUND, nameof(ErrorCodes.DOCTOR_NOT_FOUND));

            // Experto: el médico sabe qué disponibilidades tiene. Creador: él crea las reglas y cada regla sus turnos.
            if (overwrite)
            {
                doctor.ClearAvailabilities(month, year, today, now.TimeOfDay, now);
            }

            var existingSlots = await _persistence.GetFiltered<AvailabilitySlot>(
                s => s.DoctorId == doctor.Id && !s.Deleted && s.SlotDate >= today) ?? [];

            var occupied = existingSlots
                .Where(s => !s.Deleted)
                .Select(s => (s.SlotDate, s.StartTime))
                .ToHashSet();

            var createdRules = new List<AvailabilityRule>();
            var createdSlots = new List<AvailabilitySlot>();

            foreach (var day in parsedDays)
            {
                var rule = doctor.AddAvailability(month, year, day.DayOfWeek, day.StartTime, day.EndTime);
                rule.CreatedAt = now;
                rule.UpdatedAt = now;

                var slots = _generator.GenerateSlotsForMonth(rule, today, now.TimeOfDay, occupied);

                foreach (var slot in slots)
                {
                    slot.CreatedAt = now;
                    slot.UpdatedAt = now;
                }

                createdRules.Add(rule);
                createdSlots.AddRange(slots);
            }

            // Las entidades nuevas se agregan de forma explícita (tienen Guid asignado desde el constructor)
            await _persistence.AddRange(createdRules);
            await _persistence.AddRange(createdSlots);

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