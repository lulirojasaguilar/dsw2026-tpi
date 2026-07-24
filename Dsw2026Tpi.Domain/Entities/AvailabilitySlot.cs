using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class AvailabilitySlot : EntityBase
    {
        public Guid AvailabilityRuleId { get; private set; }
        public Guid DoctorId { get; private set; }
        public DateOnly SlotDate { get; private set; }
        public TimeSpan StartTime { get; private set; }
        public TimeSpan EndTime { get; private set; }
        public string Status { get; private set; }
        public bool Deleted { get; private set; }

        protected AvailabilitySlot() { }

        public AvailabilitySlot(
            Guid availabilityRuleId, Guid doctorId, DateOnly slotDate, TimeSpan startTime, TimeSpan endTime, string status)
        {
            if (!AvailabilityStatuses.All.Contains(status))
            {
                throw new ArgumentException($"Status '{status}' inválido. Valores permitidos: AVAILABLE, BOOKED, BLOCKED.");
            }

            AvailabilityRuleId = availabilityRuleId;
            DoctorId = doctorId;
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

      
        public void MarkAsBooked()
        {
            if (Status != AvailabilityStatuses.Available)
            {
                throw new InvalidOperationException("APPOINTMENT_CONFLICT: El turno no está disponible para reservar.");
            }
            Status = AvailabilityStatuses.Booked;
        }

        public void MarkAsBlocked()
        {
            if (Status != AvailabilityStatuses.Available)
            {
                throw new InvalidOperationException("APPOINTMENT_CONFLICT: Solo se puede bloquear un turno que está disponible.");
            }
            Status = AvailabilityStatuses.Blocked;
        }

        public void MarkAsAvailable()
        {
            Status = AvailabilityStatuses.Available;
        }
    }
}
