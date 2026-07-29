using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class AvailabilityRule : EntityBase
    {
        public Guid DoctorId { get; private set; }
        public byte Month { get; private set; }
        public short Year { get; private set; }
        public byte DayOfWeek { get; private set; } // 0=Lunes...6=Domingo (convención del equipo)
        public TimeSpan StartTime { get; private set; }
        public TimeSpan EndTime { get; private set; }
        public bool Deleted { get; private set; }

        protected AvailabilityRule() { }

        public AvailabilityRule(
          Guid doctorId,  
          int month, 
          int year, 
          int dayOfWeek, 
          TimeSpan startTime, 
          TimeSpan endTime)
        {
            if (doctorId == Guid.Empty)
            {
                throw new ArgumentException(
                    "DoctorId es obligatorio.",
                    nameof(doctorId));
            }

            if (month is < 1 or > 12)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(month),
                    "Month debe estar entre 1 y 12.");
            }

            if (year < 1 || year > short.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    "Year no es válido.");
            }

            if (dayOfWeek is < 0 or > 6)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dayOfWeek),
                    "DayOfWeek debe estar entre 0 (Lunes) y 6 (Domingo).");
            }

            if (startTime >= endTime)
            {
                throw new ArgumentException(
                    "StartTime debe ser menor a EndTime.");
            }

            DoctorId = doctorId;
            Month = (byte)month;
            Year = (short)year;
            DayOfWeek = (byte)dayOfWeek;
            StartTime = startTime;
            EndTime = endTime;
            Deleted = false;
        }

        public void Delete()
        {
            Deleted = true;
        }
    }
}
