using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Constants;


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
            if (availabilityRuleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "AvailabilityRuleId es obligatorio.",
                    nameof(availabilityRuleId));
            }

            if (doctorId == Guid.Empty)
            {
                throw new ArgumentException(
                    "DoctorId es obligatorio.",
                    nameof(doctorId));
            }

            if (startTime >= endTime)
            {
                throw new ArgumentException(
                    "StartTime debe ser menor a EndTime.");
            }

            if (endTime - startTime != TimeSpan.FromMinutes(30))
            {
                throw new ArgumentException(
                    "Cada slot debe tener una duración exacta de 30 minutos.");
            }

            if (string.IsNullOrWhiteSpace(status) || !AvailabilityStatuses.All.Contains(status))
            {
                throw new ArgumentException($"Status '{status}' inválido. Valores permitidos: AVAILABLE, BOOKED, BLOCKED.", nameof(status));
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
                throw new BusinessRuleException("El turno no está disponible para reservar.", "APPOINTMENT_CONFLICT");
            }
            Status = AvailabilityStatuses.Booked;
        }

        public void MarkAsBlocked()
        {
            if (Status != AvailabilityStatuses.Available)
            {
                throw new BusinessRuleException("Solo se puede bloquear un turno que está disponible.", "APPOINTMENT_CONFLICT");
            }
            Status = AvailabilityStatuses.Blocked;
        }

        public void MarkAsAvailable()
        {
            if (Status != AvailabilityStatuses.Booked)
            {
                throw new BusinessRuleException(
                    "Solo un turno reservado puede volver a estar disponible.",
                    "APPOINTMENT_CONFLICT");
            }

            Status = AvailabilityStatuses.Available;
        }
    }
}
