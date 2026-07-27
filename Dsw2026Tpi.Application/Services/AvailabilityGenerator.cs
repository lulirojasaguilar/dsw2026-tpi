using System.Globalization;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;


namespace Dsw2026Tpi.Application.Services
{
    public class AvailabilityGenerator
    {

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
                        "Use MONDAY, TUESDAY, WEDNESDAY, THURSDAY, FRIDAY, SATURDAY o SUNDAY.");
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


        public static HashSet<DateOnly> GetHolidaysForYear(int year)
        {
            if (year != 2027)
            {
                return new HashSet<DateOnly>
                {
                    new(year, 1, 1),
                    new(year, 3, 24),
                    new(year, 4, 2),
                    new(year, 5, 1),
                    new(year, 5, 25),
                    new(year, 6, 20),
                    new(year, 7, 9),
                    new(year, 12, 8),
                    new(year, 12, 25)
                };
            }

            return new HashSet<DateOnly>
            {
                new(2027, 1, 1),   // Año Nuevo

                new(2027, 2, 8),   // Lunes de Carnaval
                new(2027, 2, 9),   // Martes de Carnaval

                new(2027, 3, 24),  // Día de la Memoria
                new(2027, 3, 25),  // Jueves Santo - día no laborable
                new(2027, 3, 26),  // Viernes Santo

                new(2027, 4, 2),   // Veteranos y Caídos en Malvinas

                new(2027, 5, 1),   // Día del Trabajador
                new(2027, 5, 25),  // Revolución de Mayo

                new(2027, 6, 20),  // Día de la Bandera
                new(2027, 6, 21),  // Güemes trasladado

                new(2027, 7, 9),   // Día de la Independencia

                new(2027, 8, 16),  // San Martín trasladado

                new(2027, 10, 11), // Diversidad Cultural trasladado

                new(2027, 11, 20), // Día de la Soberanía Nacional

                new(2027, 12, 8),  // Inmaculada Concepción
                new(2027, 12, 25)  // Navidad
             };
        }


        public List<AvailabilitySlot> GenerateSlotsForMonth(
            Guid ruleId,
            Guid doctorId,
            int month,
            int year,
            int targetDayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime,
            DateOnly today,
            TimeSpan nowTimeOfDay)
        {
            if (ruleId == Guid.Empty)
            {
                throw new ValidationException(
                    "Los datos de la regla de disponibilidad no son válidos.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "availabilityRuleId",
                        "El identificador de la regla es obligatorio.");
            }

            if (doctorId == Guid.Empty)
            {
                throw new ValidationException(
                    "Los datos de disponibilidad no son válidos.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "doctorId",
                        "El identificador del médico es obligatorio.");
            }

            if (month is < 1 or > 12)
            {
                throw new ValidationException(
                    "Los datos de disponibilidad no son válidos.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "month",
                        "El mes debe estar entre 1 y 12.");
            }

            if (year is < 1 or > 9999)
            {
                throw new ValidationException(
                    "Los datos de disponibilidad no son válidos.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "year",
                        "El año debe estar entre 1 y 9999.");
            }

            if (targetDayOfWeek is < 0 or > 6)
            {
                throw new ValidationException(
                    "Los datos de disponibilidad no son válidos.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "day",
                        "El día de la semana debe estar entre 0 y 6.");

            }

            ValidateRange(startTime, endTime);

            var requestedMonth = new DateOnly(year, month, 1);
            var currentMonth = new DateOnly(today.Year, today.Month, 1);

            if (requestedMonth < currentMonth)
            {
                throw new ValidationException(
                "No se puede generar disponibilidad para un mes pasado.",
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail(
                    "month",
                    "El mes y el año deben corresponder al mes actual o a uno futuro.");
            }

            var slots = new List<AvailabilitySlot>();
            int daysInMonth = DateTime.DaysInMonth(year, month);
            var holidays = GetHolidaysForYear(year);

            var isCurrentMonth = year == today.Year && month == today.Month;
            var firstDay = isCurrentMonth ? today.Day : 1;

            for (int day = firstDay; day <= daysInMonth; day++)
            {
                var currentDate = new DateOnly(year, month, day);

                if (NormalizeNetDayOfWeek(currentDate.DayOfWeek) != targetDayOfWeek)
                {
                    continue;
                }

                if (holidays.Contains(currentDate))
                {
                    continue;
                }


                var currentSlotStart = startTime;

                while (currentSlotStart.Add(TimeSpan.FromMinutes(30)) <= endTime)
                {
                    var currentSlotEnd = currentSlotStart.Add(TimeSpan.FromMinutes(30));

                    var isPastSlotToday =
                        currentDate == today &&
                        currentSlotStart < nowTimeOfDay;

                    if (!isPastSlotToday)
                    {
                        slots.Add(new AvailabilitySlot(
                            ruleId,
                            doctorId,
                            currentDate,
                            currentSlotStart,
                            currentSlotEnd));
                    }

                    currentSlotStart = currentSlotEnd;
                }
            }

            return slots;
        }
    }
}