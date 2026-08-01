using System.Globalization;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;


namespace Dsw2026Tpi.Application.Services
{
    public class AvailabilityGenerator
    {
        private readonly IHolidayProvider _holidayProvider;

        public AvailabilityGenerator(IHolidayProvider holidayProvider)
        {
            _holidayProvider = holidayProvider;
        }

        private static readonly Dictionary<string, int> DayValues =
             new(StringComparer.OrdinalIgnoreCase)
             {
                 ["LUNES"] = 0,
                 ["MARTES"] = 1,
                 ["MIERCOLES"] = 2,
                 ["MIÉRCOLES"] = 2,
                 ["JUEVES"] = 3,
                 ["VIERNES"] = 4,
                 ["SABADO"] = 5,
                 ["SÁBADO"] = 5,
                 ["DOMINGO"] = 6,

                 // Compatibilidad 
                 ["MONDAY"] = 0,
                 ["TUESDAY"] = 1,
                 ["WEDNESDAY"] = 2,
                 ["THURSDAY"] = 3,
                 ["FRIDAY"] = 4,
                 ["SATURDAY"] = 5,
                 ["SUNDAY"] = 6
             };


        private static readonly string[] SpanishDayNames =
            ["LUNES",
            "MARTES",
            "MIÉRCOLES",
            "JUEVES",
            "VIERNES",
            "SÁBADO",
            "DOMINGO"];

        public static string DayName(int dayOfWeek) => SpanishDayNames[dayOfWeek];

        public static int ParseDay(string? day)
        {
            var normalized = day?.Trim();

            if (string.IsNullOrWhiteSpace(normalized) ||
                !DayValues.TryGetValue(normalized, out var dayValue))
            {
                throw new ValidationException(
                    $"El día '{day}' no es válido.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "day",
                        "Use LUNES, MARTES, MIÉRCOLES, JUEVES, VIERNES, SÁBADO o DOMINGO.");
            }

            return dayValue;
        }


        public static int NormalizeNetDayOfWeek(DayOfWeek nativeDayOfWeek)
        {
            return ((int)nativeDayOfWeek + 6) % 7;
        }


        public static TimeSpan ParseTime(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !TimeSpan.TryParseExact(
                    value,
                    @"hh\:mm",
                    CultureInfo.InvariantCulture,
                    out var result))
            {
                throw new ValidationException(
                    "El horario indicado no es válido.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        fieldName,
                        "Debe tener formato HH:mm, por ejemplo 09:30.");
            }

            return result;
        }


        public static void ValidateRange(TimeSpan startTime, TimeSpan endTime)
        {
            if (startTime >= endTime)
            {
                throw new ValidationException(
                "El rango horario no es válido.",
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail(
                    "startTime",
                    "La hora de inicio debe ser menor que la hora de finalización.");
            }

            var duration = endTime - startTime;

            if (duration < TimeSpan.FromMinutes(30))
            {
                throw new ValidationException(
                "El rango horario no es válido.",
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail(
                    "endTime",
                    "El rango debe permitir al menos un bloque de 30 minutos.");
            }

            if (duration.TotalMinutes % 30 != 0)
            {
                throw new ValidationException(
                      "El rango horario no es válido.",
                      nameof(ErrorCodes.VALIDATION_ERROR))
                      .WithDetail(
                          "endTime",
                          "La duración del rango debe ser múltiplo exacto de 30 minutos.");
            }
        }

        public List<AvailabilitySlot> GenerateSlotsForMonth(
            AvailabilityRule rule,
            DateOnly today,
            TimeSpan nowTimeOfDay)
        {
            var slots = new List<AvailabilitySlot>();

            var daysInMonth = DateTime.DaysInMonth(rule.Year, rule.Month);
            var firstDay = (rule.Year == today.Year && rule.Month == today.Month) ? today.Day : 1;

            for (var day = firstDay; day <= daysInMonth; day++)
            {
                var currentDate = new DateOnly(rule.Year, rule.Month, day);

                if (NormalizeNetDayOfWeek(currentDate.DayOfWeek) != rule.DayOfWeek)
                {
                    continue;
                }

                if (_holidayProvider.IsHoliday(currentDate))
                {
                    continue;
                }

                var currentSlotStart = rule.StartTime;

                while (currentSlotStart.Add(TimeSpan.FromMinutes(30)) <= rule.EndTime)
                {
                    var currentSlotEnd = currentSlotStart.Add(TimeSpan.FromMinutes(30));

                    var isPastSlotToday = currentDate == today && currentSlotStart < nowTimeOfDay;

                    if (!isPastSlotToday)
                    {
                        slots.Add(new AvailabilitySlot(rule, rule.DoctorId, currentDate, currentSlotStart, currentSlotEnd));
                    }

                    currentSlotStart = currentSlotEnd;
                }
            }

            return slots;
        }
    }
}