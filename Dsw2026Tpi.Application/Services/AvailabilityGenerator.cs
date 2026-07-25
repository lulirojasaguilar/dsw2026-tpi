using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Constants;
using Dsw2026Tpi.Domain.Entities;
using System.Globalization;


namespace Dsw2026Tpi.Application.Services
{
    public class AvailabilityGenerator
    {
        
        private static readonly string[] DayNames =
        {
            "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY", "SUNDAY"
        };

       
        public static int ParseDay(string day)
        {
            var normalized = day?.Trim().ToUpperInvariant();
            var index = Array.IndexOf(DayNames, normalized);
            if (index == -1)
            {
                throw new ValidationException($"El día '{day}' no es válido. Use MONDAY..SUNDAY.", "VALIDATION_ERROR");
            }
            return index;
        }

       
        public static int NormalizeNetDayOfWeek(DayOfWeek nativeDayOfWeek)
        {
            return ((int)nativeDayOfWeek + 6) % 7;
        }

       
        public static TimeSpan ParseTime(string value, string fieldName)
        {
            if (!TimeSpan.TryParseExact(value, @"hh\:mm", CultureInfo.InvariantCulture, out var result))
            {
                throw new ValidationException($"{fieldName} debe tener formato HH:mm.", "VALIDATION_ERROR");
            }
            return result;
        }

        
        public static void ValidateRange(TimeSpan startTime, TimeSpan endTime)
        {
            if (startTime >= endTime)
            {
                throw new ValidationException("StartTime debe ser estrictamente menor a EndTime.", "VALIDATION_ERROR");
            }

            var duration = endTime - startTime;

            if (duration < TimeSpan.FromMinutes(30))
            {
                throw new ValidationException("El rango debe permitir al menos un bloque de 30 minutos.", "VALIDATION_ERROR");
            }

            if (duration.TotalMinutes % 30 != 0)
            {
                throw new ValidationException("El rango debe ser múltiplo exacto de 30 minutos (ej. 09:00-10:15 no es válido).", "VALIDATION_ERROR");
            }
        }

        
        public static HashSet<DateOnly> GetHolidaysForYear(int year)
        {
            return new HashSet<DateOnly>
            {
                new(year, 1, 1),   // Año Nuevo
                new(year, 5, 1),   // Día del Trabajador
                new(year, 5, 25),  // Revolución de Mayo
                new(year, 6, 20),  // Día de la Bandera
                new(year, 7, 9),   // Día de la Independencia
                new(year, 8, 17),  // Paso a la Inmortalidad del Gral. San Martín
                new(year, 10, 12), // Día del Respeto a la Diversidad Cultural
                new(year, 11, 20), // Día de la Soberanía Nacional
                new(year, 12, 8),  // Inmaculada Concepción
                new(year, 12, 25), // Navidad
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
                    "AvailabilityRuleId es obligatorio.",
                    "VALIDATION_ERROR");
            }

            if (doctorId == Guid.Empty)
            {
                throw new ValidationException(
                    "DoctorId es obligatorio.",
                    "VALIDATION_ERROR");
            }

            if (month is < 1 or > 12)
            {
                throw new ValidationException(
                    "Month debe estar entre 1 y 12.",
                    "VALIDATION_ERROR");
            }

            if (year is < 1 or > 9999)
            {
                throw new ValidationException(
                    "Year debe estar entre 1 y 9999.",
                    "VALIDATION_ERROR");
            }

            if (targetDayOfWeek is < 0 or > 6)
            {
                throw new ValidationException(
                    "El día de la semana debe estar entre 0 y 6.",
                    "VALIDATION_ERROR");
            }

            ValidateRange(startTime, endTime);


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

                var isToday = currentDate == today;
                var currentSlotStart = startTime;

                while (currentSlotStart.Add(TimeSpan.FromMinutes(30)) <= endTime)
                {
                    var currentSlotEnd = currentSlotStart.Add(TimeSpan.FromMinutes(30));

               
                    if (!isToday || currentSlotStart >= nowTimeOfDay)
                    {
                        slots.Add(new AvailabilitySlot(
                            ruleId,
                            doctorId,
                            currentDate,
                            currentSlotStart,
                            currentSlotEnd,
                            status: AvailabilityStatuses.Available));
                    }

                    currentSlotStart = currentSlotEnd;
                }
            }

            return slots;
        }
    }
}