using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class AvailabilityRule : EntityBase
    {
        public Guid DoctorId { get; private set; }
        public int Month {  get; private set; }
        public int Year { get; private set; }
        public int DayOfWeek { get; private set; }

        public TimeSpan StartTime { get; private set; }
        public TimeSpan EndTime { get; private set; }

        public bool Deleted { get; private set; }

        protected AvailabilityRule() { }

        public AvailabilityRule(Guid doctorId, int month, int year, int dayOfWeek, TimeSpan startTime, TimeSpan endTime)
        {
            DoctorId = doctorId;
            Month = month;
            Year = year;
            DayOfWeek = dayOfWeek;
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
