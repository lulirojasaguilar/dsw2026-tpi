using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Constants;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
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
            _logger.LogInformation(
                "Iniciando generación de disponibilidad para el doctor {DoctorId} en el periodo {Month}/{Year}",
                request.DoctorId, request.Month, request.Year);

            var parsedDays = await ValidateAndParseRequestAsync(request, isUpdate: false);

            var doctor = await GetActiveDoctorOrThrowAsync(request.DoctorId);

            
            var createdRules = new List<AvailabilityRule>();
            var allSlots = new List<AvailabilitySlot>();
            var today = DateOnly.FromDateTime(DateTime.Now);
            var nowTimeOfDay = DateTime.Now.TimeOfDay;

            foreach (var day in parsedDays)
            {
                var rule = new AvailabilityRule(
                    request.DoctorId, request.Month, request.Year, day.DayIndex, day.StartTime, day.EndTime);

                createdRules.Add(rule);

                var slots = _generator.GenerateSlotsForMonth(
                    rule.Id, request.DoctorId, request.Month, request.Year, day.DayIndex, day.StartTime, day.EndTime,
                    today, nowTimeOfDay);

                allSlots.AddRange(slots);
            }

           
            foreach (var rule in createdRules)
            {
                await _persistence.Add(rule);
            }
            foreach (var slot in allSlots)
            {
                await _persistence.Add(slot);
            }

            _logger.LogInformation(
                "Se generaron {RuleCount} reglas y {SlotCount} slots para el doctor {DoctorId}",
                createdRules.Count, allSlots.Count, request.DoctorId);

            return BuildResponse(request, createdRules, allSlots);
        }

        public async Task<AvailabilityModel.Response> UpdateAvailabilityAsync(AvailabilityModel.Request request)
        {
            _logger.LogInformation(
                "Iniciando sobrescritura de disponibilidad para el doctor {DoctorId} en el periodo {Month}/{Year}",
                request.DoctorId, request.Month, request.Year);

            
            var parsedDays = await ValidateAndParseRequestAsync(request, isUpdate: true);
            await GetActiveDoctorOrThrowAsync(request.DoctorId);

            
            var existingRules = (await _persistence.GetFiltered<AvailabilityRule>(r =>
                    r.DoctorId == request.DoctorId
                    && r.Month == request.Month
                    && r.Year == request.Year
                    && !r.Deleted) ?? Enumerable.Empty<AvailabilityRule>())
                .ToList();

         
            if (existingRules.Count == 0)
            {
                throw new EntityNotFoundException("AvailabilityRule");
            }

            var ruleIds = existingRules.Select(r => r.Id).ToList();

            var existingSlots = (await _persistence.GetFiltered<AvailabilitySlot>(s =>
                    ruleIds.Contains(s.AvailabilityRuleId) && !s.Deleted) ?? Enumerable.Empty<AvailabilitySlot>())
                .ToList();

           
            var hasProtectedSlots = existingSlots.Any(s =>
                s.Status == AvailabilityStatuses.Booked || s.Status == AvailabilityStatuses.Blocked);

            if (hasProtectedSlots)
            {
                _logger.LogWarning(
                    "Intento de sobrescritura fallido: el doctor {DoctorId} tiene turnos reservados o bloqueados en {Month}/{Year}",
                    request.DoctorId, request.Month, request.Year);

                throw new BusinessRuleException(
                    "UPDATE_CONFLICT",
                    "No se puede sobrescribir el mes porque existen turnos reservados (BOOKED) o bloqueados (BLOCKED) para este período.");
            }

            
            foreach (var rule in existingRules)
            {
                rule.Delete();
                await _persistence.Update(rule);
            }
            foreach (var slot in existingSlots)
            {
                slot.Delete();
                await _persistence.Update(slot);
            }

            var createdRules = new List<AvailabilityRule>();
            var allSlots = new List<AvailabilitySlot>();
            var today = DateOnly.FromDateTime(DateTime.Now);
            var nowTimeOfDay = DateTime.Now.TimeOfDay;

            foreach (var day in parsedDays)
            {
                var newRule = new AvailabilityRule(
                    request.DoctorId, request.Month, request.Year, day.DayIndex, day.StartTime, day.EndTime);

                createdRules.Add(newRule);

                var slots = _generator.GenerateSlotsForMonth(
                    newRule.Id, request.DoctorId, request.Month, request.Year, day.DayIndex, day.StartTime, day.EndTime,
                    today, nowTimeOfDay);

                allSlots.AddRange(slots);
            }

            foreach (var rule in createdRules)
            {
                await _persistence.Add(rule);
            }
            foreach (var slot in allSlots)
            {
                await _persistence.Add(slot);
            }

            _logger.LogInformation(
                "Se sobrescribió la disponibilidad del doctor {DoctorId} para {Month}/{Year}: {RuleCount} reglas, {SlotCount} slots nuevos",
                request.DoctorId, request.Month, request.Year, createdRules.Count, allSlots.Count);

            return BuildResponse(request, createdRules, allSlots);
        }

      

        private record ParsedDay(int DayIndex, string DayName, TimeSpan StartTime, TimeSpan EndTime);

        private async Task<List<ParsedDay>> ValidateAndParseRequestAsync(AvailabilityModel.Request request, bool isUpdate)
        {
           
            if (request.DoctorId == Guid.Empty)
            {
                throw new ValidationException("VALIDATION_ERROR", "DoctorId es obligatorio.");
            }

            
            if (request.Days is null || request.Days.Count == 0)
            {
                throw new ValidationException("VALIDATION_ERROR", "Debe indicar al menos un día de atención.");
            }

           
            var today = DateOnly.FromDateTime(DateTime.Now);
            var lastDayOfRequestedMonth = new DateOnly(request.Year, request.Month, DateTime.DaysInMonth(request.Year, request.Month));
            if (lastDayOfRequestedMonth < today)
            {
                throw new ValidationException("VALIDATION_ERROR", "No se puede configurar disponibilidad para un mes completamente pasado.");
            }

            var parsed = new List<ParsedDay>();

            foreach (var day in request.Days)
            {
              
                if (string.IsNullOrWhiteSpace(day.Day))
                {
                    throw new ValidationException("VALIDATION_ERROR", "El campo 'day' es obligatorio en cada elemento de 'days'.");
                }
                if (string.IsNullOrWhiteSpace(day.StartTime) || string.IsNullOrWhiteSpace(day.EndTime))
                {
                    throw new ValidationException("VALIDATION_ERROR", "Los campos 'startTime' y 'endTime' son obligatorios en cada elemento de 'days'.");
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
                        throw new ValidationException(
                            "VALIDATION_ERROR",
                            $"El request contiene horarios duplicados o solapados para el día {a.DayName}.");
                    }
                }
            }


            foreach (var day in parsed)
            {
                var hasOverlap = (await _persistence.GetFiltered<AvailabilityRule>(r =>
                        r.DoctorId == request.DoctorId
                        && r.DayOfWeek == day.DayIndex
                        && !r.Deleted
                        && r.Month == request.Month       
                        && r.Year == request.Year         
                        && !isUpdate                      
                        && day.StartTime < r.EndTime
                        && r.StartTime < day.EndTime) ?? Enumerable.Empty<AvailabilityRule>())
                    .Any();

                if (hasOverlap)
                {
                    throw new BusinessRuleException(
                        "SCHEDULE_OVERLAP",
                        $"El horario indicado para {day.DayName} se solapa con otra disponibilidad existente.");
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

        private static AvailabilityModel.Response BuildResponse(
            AvailabilityModel.Request request, List<AvailabilityRule> rules, List<AvailabilitySlot> slots)
        {
            
            var ruleSummaries = rules.Select(r => new AvailabilityModel.RuleSummary(
                r.Id,
                DayIndexToName(r.DayOfWeek),
                r.StartTime.ToString(@"hh\:mm"),
                r.EndTime.ToString(@"hh\:mm")
            )).ToList();

            return new AvailabilityModel.Response(
                request.DoctorId,
                request.Month,
                request.Year,
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