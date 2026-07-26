using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
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
            if (request is null)
            {
                throw new ValidationException(
                    "El cuerpo de la solicitud es obligatorio.",
                    "VALIDATION_ERROR");
            }

            if (request.DoctorId == Guid.Empty)
            {
                throw new ValidationException("DoctorId es obligatorio.", "VALIDATION_ERROR");
            }

            var now = DateTime.Now;
            var month = now.Month;
            var year = now.Year;
            var today = DateOnly.FromDateTime(now);
            var nowTimeOfDay = now.TimeOfDay;

            _logger.LogInformation(
                "Iniciando generación de disponibilidad para el doctor {DoctorId} en el periodo {Month}/{Year}",
                request.DoctorId, month, year);


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
                    rule.Id,
                    rule.DoctorId,
                    rule.Month,
                    rule.Year,
                    rule.DayOfWeek,
                    rule.StartTime,
                    rule.EndTime,
                    today,
                    nowTimeOfDay);

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

            return BuildResponse(request.DoctorId, month, year, createdRules, allSlots);
        }

        public async Task<AvailabilityModel.Response> UpdateAvailabilityAsync(AvailabilityModel.Request request)
        {
            if (request is null)
            {
                throw new ValidationException(
                    "El cuerpo de la solicitud es obligatorio.",
                    "VALIDATION_ERROR");
            }

            if (request.DoctorId == Guid.Empty)
            {
                throw new ValidationException(
                    "DoctorId es obligatorio.",
                    "VALIDATION_ERROR");
            }

            var now = DateTime.Now;
            var month = now.Month;
            var year = now.Year;
            var today = DateOnly.FromDateTime(now);
            var nowTimeOfDay = now.TimeOfDay;

            _logger.LogInformation(
                "Iniciando sobrescritura de disponibilidad para el doctor {DoctorId} en el periodo {Month}/{Year}",
                request.DoctorId, month, year);


            await GetActiveDoctorOrThrowAsync(request.DoctorId);

            var parsedDays = await ValidateAndParseRequestAsync(request, month, year, isUpdate: true);

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


            var currentAndFutureSlots = existingSlots
                   .Where(slot =>
                                    slot.SlotDate > today
                                    || (
                                    slot.SlotDate == today
                                    && slot.StartTime >= nowTimeOfDay
                           ))
                    .ToList();

            var hasProtectedSlots = currentAndFutureSlots.Any(slot =>
                slot.Status == AvailabilityStatuses.Booked
                || slot.Status == AvailabilityStatuses.Blocked);

            if (hasProtectedSlots)
            {
                _logger.LogWarning(
                    "Intento de sobrescritura rechazado: el doctor {DoctorId} tiene slots reservados o bloqueados en {Month}/{Year}",
                    request.DoctorId,
                    month,
                    year);

                throw new BusinessRuleException(
                    "No se puede sobrescribir el mes porque existen turnos reservados (BOOKED) o bloqueados (BLOCKED) para este período.",
                    nameof(ErrorCodes.APPOINTMENT_CONFLICT));
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
                    newRule.Id, newRule.DoctorId, newRule.Month, newRule.Year, newRule.DayOfWeek, newRule.StartTime, newRule.EndTime,
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

            return BuildResponse(
                request.DoctorId,
                month,
                year,
                createdRules,
                allSlots);
        }



        private async Task<List<ParsedDay>>
             ValidateAndParseRequestAsync(
                 AvailabilityModel.Request request,
                 int month,
                 int year,
                 bool isUpdate)
        {
            if (request.Days is null || request.Days.Count == 0)
            {
                throw new ValidationException(
                    "Debe indicar al menos un día de atención.",
                    "VALIDATION_ERROR");
            }

            var parsedDays = new List<ParsedDay>();

            foreach (var day in request.Days)
            {
                if (day is null)
                {
                    throw new ValidationException(
                        "Cada elemento de 'days' debe contener datos válidos.",
                        "VALIDATION_ERROR");
                }

                if (string.IsNullOrWhiteSpace(day.Day))
                {
                    throw new ValidationException(
                        "El campo 'day' es obligatorio en cada elemento de 'days'.",
                        "VALIDATION_ERROR");
                }

                if (string.IsNullOrWhiteSpace(day.StartTime)
                    || string.IsNullOrWhiteSpace(day.EndTime))
                {
                    throw new ValidationException(
                        "Los campos 'startTime' y 'endTime' son obligatorios en cada elemento de 'days'.",
                        "VALIDATION_ERROR");
                }

                var dayIndex = AvailabilityGenerator.ParseDay(day.Day);

                var startTime = AvailabilityGenerator.ParseTime(
                    day.StartTime,
                    "startTime");

                var endTime = AvailabilityGenerator.ParseTime(
                    day.EndTime,
                    "endTime");

                AvailabilityGenerator.ValidateRange(
                    startTime,
                    endTime);

                /*
                 * Se guarda el nombre normalizado del día.
                 * De esta manera, aunque el cliente envíe MONDAY o lunes,
                 * internamente se utiliza siempre LUNES.
                 */
                parsedDays.Add(new ParsedDay(
                    dayIndex,
                    DayIndexToName(dayIndex),
                    startTime,
                    endTime));
            }

            ValidateRequestOverlaps(parsedDays);

            /*
             * En el POST se compara con la disponibilidad
             * que ya existe en la base de datos.
             *
             * En el PUT no es necesario porque las reglas actuales
             * serán eliminadas lógicamente y reemplazadas.
             */
            if (!isUpdate)
            {
                foreach (var day in parsedDays)
                {
                    var existingRules =
                        await _persistence.GetFiltered<AvailabilityRule>(
                            rule =>
                                rule.DoctorId == request.DoctorId
                                && rule.Month == month
                                && rule.Year == year
                                && rule.DayOfWeek == day.DayIndex
                                && !rule.Deleted
                                && day.StartTime < rule.EndTime
                                && rule.StartTime < day.EndTime);

                    var hasOverlap =
                        (existingRules
                         ?? Enumerable.Empty<AvailabilityRule>())
                        .Any();

                    if (hasOverlap)
                    {
                        throw new BusinessRuleException(
                            $"El horario indicado para {day.DayName} se solapa con otra disponibilidad existente.",
                            "SCHEDULE_OVERLAP");
                    }
                }
            }

            return parsedDays;
        }

        private static void ValidateRequestOverlaps(
    List<ParsedDay> parsedDays)
        {
            for (var i = 0; i < parsedDays.Count; i++)
            {
                for (var j = i + 1; j < parsedDays.Count; j++)
                {
                    var firstDay = parsedDays[i];
                    var secondDay = parsedDays[j];

                    if (firstDay.DayIndex != secondDay.DayIndex)
                    {
                        continue;
                    }

                    var overlaps =
                        firstDay.StartTime < secondDay.EndTime
                        && secondDay.StartTime < firstDay.EndTime;

                    if (overlaps)
                    {
                        throw new BusinessRuleException(
                            $"El request contiene horarios duplicados o solapados para el día {firstDay.DayName}.",
                            "SCHEDULE_OVERLAP");
                    }
                }
            }
        }

        private async Task<Doctor> GetActiveDoctorOrThrowAsync(
            Guid doctorId)
        {
            var doctor = await _persistence.First<Doctor>(
                currentDoctor =>
                    currentDoctor.Id == doctorId
                    && !currentDoctor.Deleted);

            if (doctor is null)
            {
                throw new EntityNotFoundException("Doctor");
            }

            return doctor;
        }

        private static bool IsUniqueConstraintViolation(
         DbUpdateException exception)
        {
            return exception.InnerException is SqlException sqlException
                && (
                    sqlException.Number == 2601
                    || sqlException.Number == 2627
                );
        }

        private static AvailabilityModel.Response BuildResponse(
            Guid doctorId,
            int month,
            int year,
            List<AvailabilityRule> rules,
            List<AvailabilitySlot> slots)
        {
            var ruleSummaries = rules
                .Select(rule =>
                    new AvailabilityModel.RuleSummary(
                        rule.Id,
                        DayIndexToName(rule.DayOfWeek),
                        rule.StartTime.ToString(@"hh\:mm"),
                        rule.EndTime.ToString(@"hh\:mm")))
                .ToList();

            return new AvailabilityModel.Response(
                doctorId,
                month,
                year,
                rules.Count,
                slots.Count,
                ruleSummaries);
        }

        private static string DayIndexToName(int index)
        {
            if (index < 0 || index >= DayNames.Length)
            {
                throw new ValidationException(
                    "El valor del día de la semana no es válido.",
                    "VALIDATION_ERROR");
            }

            return DayNames[index];
        }

        private sealed record ParsedDay(
            int DayIndex,
            string DayName,
            TimeSpan StartTime,
            TimeSpan EndTime);

        private static readonly string[] DayNames =
        {
            "LUNES",
            "MARTES",
            "MIÉRCOLES",
            "JUEVES",
            "VIERNES",
            "SÁBADO",
            "DOMINGO"
        };
    }
}