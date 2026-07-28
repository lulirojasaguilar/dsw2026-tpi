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
         Guid availabilityRuleId,
         Guid doctorId,
         DateOnly slotDate,
         TimeSpan startTime,
         TimeSpan endTime,
         string status,
         Guid? id = null) : base(id)
        {
            Validate(availabilityRuleId, doctorId, status, startTime, endTime);

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
        private static void Validate(
        Guid availabilityRuleId,
        Guid doctorId,
        string status,
        TimeSpan startTime,
        TimeSpan endTime)
        {
            if (availabilityRuleId == Guid.Empty)
            {
                throw new ArgumentException("AvailabilityRuleId no puede ser vacío.", nameof(availabilityRuleId));
            }

            if (doctorId == Guid.Empty)
            {
                throw new ArgumentException("DoctorId no puede ser vacío.", nameof(doctorId));
            }

            if (!AvailabilityStatuses.All.Contains(status))
            {
                throw new ArgumentException($"Status '{status}' inválido. Valores permitidos: AVAILABLE, BOOKED, BLOCKED.");
            }

            if (endTime <= startTime)
            {
                throw new ArgumentException("EndTime debe ser mayor a StartTime.");
            }
        }
    }
}
