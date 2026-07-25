using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Constants;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services
{

    public class AvailabilityService : IAvailabilityService
    {
        private readonly IPersistence _persistence;
        private readonly ILogger<AvailabilityService> _logger;
        private readonly AvailabilityGenerator _generator;

        public AvailabilityService(IPersistence persistence, ILogger<AvailabilityService> logger)
        {
            _persistence = persistence;
            _logger = logger;
            _generator = new AvailabilityGenerator();
        }

        public async Task<AvailabilityModel.Response> CreateAvailabilityAsync(AvailabilityModel.Request request)
        {
            var now = DateTime.Now;
            var month = now.Month;
            var year = now.Year;
            var today = DateOnly.FromDateTime(now);
            var nowTimeOfDay = now.TimeOfDay;

            _logger.LogInformation(
                "Iniciando generación de disponibilidad para el doctor {DoctorId} en el periodo {Month}/{Year}",
                request.DoctorId, month, year);

            if (request.DoctorId == Guid.Empty)
            {
                throw new ValidationException( "DoctorId es obligatorio.", "VALIDATION_ERROR");
            }

            await GetActiveDoctorOrThrowAsync(request.DoctorId);

            var parsedDays = await ValidateAndParseRequestAsync(request, month, year, false);


            var createdRules = new List<AvailabilityRule>();
            var allSlots = new List<AvailabilitySlot>();


            foreach (var day in parsedDays)
            {
                var rule = new AvailabilityRule(
                    request.DoctorId, month, year, day.DayIndex, day.StartTime, day.EndTime);

                createdRules.Add(rule);

                var slots = _generator.GenerateSlotsForMonth(
                    rule.Id, request.DoctorId, month, year, day.DayIndex, day.StartTime, day.EndTime,
                    today, nowTimeOfDay);

                allSlots.AddRange(slots);
            }


            try
            {
                await _persistence.ExecuteInTransactionAsync(async () =>
                {
                    await _persistence.AddRange(createdRules);
                    await _persistence.AddRange(allSlots);
                    await _persistence.SaveChangesAsync();
                });
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _logger.LogWarning(
                    ex,
                    "Conflicto de concurrencia al crear disponibilidad para el doctor {DoctorId} en {Month}/{Year}",
                    request.DoctorId,
                    month,
                    year);

                throw new BusinessRuleException(
                    "La disponibilidad indicada se superpone o ya fue registrada por otra operación.",
                    "SCHEDULE_OVERLAP");
            }

            _logger.LogInformation(
                "Se generaron {RuleCount} reglas y {SlotCount} slots para el doctor {DoctorId} en {Month}/{Year}",
                createdRules.Count, allSlots.Count, request.DoctorId, month, year);

            return BuildResponse(request, month, year, createdRules, allSlots);
        }

        public async Task<AvailabilityModel.Response> UpdateAvailabilityAsync(AvailabilityModel.Request request)
        {
            var now = DateTime.Now;
            var month = now.Month;
            var year = now.Year;
            var today = DateOnly.FromDateTime(now);
            var nowTimeOfDay = now.TimeOfDay;

            _logger.LogInformation(
                "Iniciando sobrescritura de disponibilidad para el doctor {DoctorId} en el periodo {Month}/{Year}",
                request.DoctorId, month, year);

            if (request.DoctorId == Guid.Empty)
            {
                throw new ValidationException(
                    "DoctorId es obligatorio.",
                    "VALIDATION_ERROR");
            }

            await GetActiveDoctorOrThrowAsync(request.DoctorId);

            var parsedDays = await ValidateAndParseRequestAsync(request, month, year, true);

            var existingRules = (await _persistence.GetFiltered<AvailabilityRule>(r =>
                    r.DoctorId == request.DoctorId
                    && r.Month == month
                    && r.Year == year
                    && !r.Deleted) 
                ?? Enumerable.Empty<AvailabilityRule>())
                .ToList();

            if (existingRules.Count == 0)
            {
                throw new EntityNotFoundException("AvailabilityRule");
            }

            var ruleIds = existingRules.Select(r => r.Id).ToList();

            var existingSlots =
                (await _persistence.GetFiltered<AvailabilitySlot>(s =>
                     ruleIds.Contains(s.AvailabilityRuleId)
                    && !s.Deleted)
                 ?? Enumerable.Empty<AvailabilitySlot>())
                .ToList();


            var futureSlots = existingSlots
                   .Where(s =>
                                s.SlotDate > today
                                || (
                                    s.SlotDate == today
                                    && s.StartTime >= nowTimeOfDay
                                ))
                    .ToList();

            var hasProtectedSlots = futureSlots.Any(s =>
                 s.Status == AvailabilityStatuses.Booked
                 || s.Status == AvailabilityStatuses.Blocked);

            if (hasProtectedSlots)
            {
                _logger.LogWarning(
                    "Intento de sobrescritura fallido: el doctor {DoctorId} tiene turnos reservados o bloqueados en {Month}/{Year}",
                    request.DoctorId, month, year);

                throw new BusinessRuleException("No se puede sobrescribir el mes porque existen turnos reservados (BOOKED) o bloqueados (BLOCKED) para este período.", "APPOINTMENT_CONFLICT");
            }


            foreach (var rule in existingRules)
            {
                rule.Delete();
            }
            foreach (var slot in existingSlots)
            {
                slot.Delete();
            }

            var createdRules = new List<AvailabilityRule>();
            var allSlots = new List<AvailabilitySlot>();

            foreach (var day in parsedDays)
            {
                var newRule = new AvailabilityRule(
                    request.DoctorId, month, year, day.DayIndex, day.StartTime, day.EndTime);

                createdRules.Add(newRule);

                var slots = _generator.GenerateSlotsForMonth(
                    newRule.Id, request.DoctorId, month, year, day.DayIndex, day.StartTime, day.EndTime,
                    today, nowTimeOfDay);

                allSlots.AddRange(slots);
            }

            try
            {
                await _persistence.ExecuteInTransactionAsync(async () =>
                {
                    await _persistence.UpdateRange(existingRules);
                    await _persistence.UpdateRange(existingSlots);

                    // Primero se guardan los borrados lógicos.
                    await _persistence.SaveChangesAsync();

                    await _persistence.AddRange(createdRules);
                    await _persistence.AddRange(allSlots);

                    // Luego se insertan las nuevas reglas y slots.
                    await _persistence.SaveChangesAsync();
                });
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _logger.LogWarning(
                    ex,
                    "Conflicto de concurrencia al actualizar disponibilidad para el doctor {DoctorId} en {Month}/{Year}",
                    request.DoctorId,
                    month,
                    year);

                throw new BusinessRuleException(
                    "La disponibilidad indicada se superpone o fue modificada por otra operación.",
                    "SCHEDULE_OVERLAP");
            }

            _logger.LogInformation(
                "Se sobrescribió la disponibilidad del doctor {DoctorId} para {Month}/{Year}: {RuleCount} reglas, {SlotCount} slots nuevos",
                request.DoctorId, month, year, createdRules.Count, allSlots.Count);

            return BuildResponse(request, month, year, createdRules, allSlots);
        }



        private record ParsedDay(int DayIndex, string DayName, TimeSpan StartTime, TimeSpan EndTime);

        private async Task<List<ParsedDay>> ValidateAndParseRequestAsync(AvailabilityModel.Request request, int month, int year, bool isUpdate)
        {

            if (request.DoctorId == Guid.Empty)
            {
                throw new ValidationException("DoctorId es obligatorio.", "VALIDATION_ERROR");
            }


            if (request.Days is null || request.Days.Count == 0)
            {
                throw new ValidationException("Debe indicar al menos un día de atención.", "VALIDATION_ERROR");
            }

            var parsed = new List<ParsedDay>();

            foreach (var day in request.Days)
            {
                if (day is null)
                {
                    throw new ValidationException("Cada elemento de 'days' debe contener datos válidos.", "VALIDATION_ERROR");
                }

                if (string.IsNullOrWhiteSpace(day.Day))
                {
                    throw new ValidationException("El campo 'day' es obligatorio en cada elemento de 'days'.", "VALIDATION_ERROR");
                }
                if (string.IsNullOrWhiteSpace(day.StartTime) || string.IsNullOrWhiteSpace(day.EndTime))
                {
                    throw new ValidationException("Los campos 'startTime' y 'endTime' son obligatorios en cada elemento de 'days'.", "VALIDATION_ERROR");
                }

                var dayIndex = AvailabilityGenerator.ParseDay(day.Day);
                var startTime = AvailabilityGenerator.ParseTime(day.StartTime, "startTime");
                var endTime = AvailabilityGenerator.ParseTime(day.EndTime, "endTime");

                AvailabilityGenerator.ValidateRange(startTime, endTime);

                parsed.Add(new ParsedDay(dayIndex, day.Day.Trim().ToUpperInvariant(), startTime, endTime));
            }


            for (int i = 0; i < parsed.Count; i++)
            {
                for (int j = i + 1; j < parsed.Count; j++)
                {
                    if (parsed[i].DayIndex != parsed[j].DayIndex) continue;

                    var a = parsed[i];
                    var b = parsed[j];

                    var overlaps = a.StartTime < b.EndTime && b.StartTime < a.EndTime;
                    if (overlaps)
                    {
                        throw new BusinessRuleException($"El request contiene horarios duplicados o solapados para el día {a.DayName}.", "SCHEDULE_OVERLAP");
                    }
                }
            }


            foreach (var day in parsed)
            {
                if (!isUpdate)
                {
                    var hasOverlap = (await _persistence.GetFiltered<AvailabilityRule>(r =>
                        r.DoctorId == request.DoctorId
                        && r.DayOfWeek == day.DayIndex
                        && !r.Deleted
                        && r.Month == month
                        && r.Year == year
                        && day.StartTime < r.EndTime
                        && r.StartTime < day.EndTime)
                    ?? Enumerable.Empty<AvailabilityRule>())
                    .Any();

                    if (hasOverlap)
                    {
                        throw new BusinessRuleException($"El horario indicado para {day.DayName} se solapa con otra disponibilidad existente.", "SCHEDULE_OVERLAP");
                    }
                }
            }

            return parsed;
        }

        private async Task<Doctor> GetActiveDoctorOrThrowAsync(Guid doctorId)
        {
            var doctor = await _persistence.First<Doctor>(d => d.Id == doctorId && !d.Deleted);
            if (doctor is null)
            {
                throw new EntityNotFoundException("Doctor");
            }
            return doctor;
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            return exception.InnerException is SqlException sqlException
                && (sqlException.Number == 2601 || sqlException.Number == 2627);
        }

        private static AvailabilityModel.Response BuildResponse(
            AvailabilityModel.Request request, int month, int year, List<AvailabilityRule> rules, List<AvailabilitySlot> slots)
        {

            var ruleSummaries = rules.Select(r => new AvailabilityModel.RuleSummary(
                r.Id,
                DayIndexToName(r.DayOfWeek),
                r.StartTime.ToString(@"hh\:mm"),
                r.EndTime.ToString(@"hh\:mm")
            )).ToList();

            return new AvailabilityModel.Response(
                request.DoctorId,
                month,
                year,
                rules.Count,
                slots.Count,
                ruleSummaries);
        }

        private static readonly string[] DayNames =
        {
            "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY", "SUNDAY"
        };

        private static string DayIndexToName(int index) => DayNames[index];
    }
}