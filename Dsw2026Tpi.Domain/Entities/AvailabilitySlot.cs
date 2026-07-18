using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class AvailabilitySlot : EntityBase
    {
        public Guid AvailabilityRuleId { get;private set; }
        public DateOnly SlotDate { get;private set; }
        public TimeSpan StartTime { get;private set; }
        public TimeSpan EndTime { get;private set; }

        public string Status { get;private set; }
        public bool Deleted { get;private set; }

        public AvailabilitySlot() { }

        public AvailabilitySlot(Guid availabilityRuleId, DateOnly slotDate, TimeSpan startTime, TimeSpan endTime, string status, bool deleted)
        {
            AvailabilityRuleId = availabilityRuleId;
            SlotDate = slotDate;
            StartTime = startTime;
            EndTime = endTime;
            Status = status;
            Deleted = false;
        }
        public void Delete()
        {
            Deleted = true;
        }

        public void MarkAsBooked() => Status = "BOOKED";
        public void MarkAsBlocked() => Status = "BLOCKED";

    }
}
