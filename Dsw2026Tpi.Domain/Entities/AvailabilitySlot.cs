using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Constants;


namespace Dsw2026Tpi.Domain.Entities
{
    public class AvailabilitySlot : EntityBase
    {
        public Guid AvailabilityRuleId { get; private set; }

        public AvailabilityRule AvailabilityRule { get; private set; } = null!;

        public Guid DoctorId { get; private set; }

        public DateOnly SlotDate { get; private set; }

        public TimeSpan StartTime { get; private set; }

        public TimeSpan EndTime { get; private set; }

        public string Status { get; private set; } = string.Empty;

        public bool Deleted { get; private set; }

        private AvailabilitySlot() 
        { 
        }

        public AvailabilitySlot(
            AvailabilityRule rule,
            Guid doctorId, 
            DateOnly slotDate, 
            TimeSpan startTime, 
            TimeSpan endTime,
            Guid? id = null) : base(id)
        {
            if (rule is null || rule.Id == Guid.Empty)
            {
                throw new ValidationException(
                    "La regla de disponibilidad indicada no es válida.",
                    nameof(ErrorCodes.VALIDATION_ERROR));
            }

            AvailabilityRule = rule;
            AvailabilityRuleId = rule.Id;
            DoctorId = doctorId;
            SlotDate = slotDate;
            StartTime = startTime;
            EndTime = endTime;
            Status = AvailabilityStatuses.Available;
            Deleted = false;
        }

        public bool IsAvailable =>
            Status == AvailabilityStatuses.Available && !Deleted;

        public void MarkBooked()
        {
            if (!IsAvailable)
            {
                throw new BusinessRuleException(
                    "El turno no está disponible para reservar.",
                    nameof(ErrorCodes.SLOT_NOT_AVAILABLE));
            }

            Status = AvailabilityStatuses.Booked;
        }

        public void MarkBlocked()
        {
            if (!IsAvailable)
            {
                throw new BusinessRuleException(
                    "Solo se puede bloquear un turno que está disponible.",
                    nameof(ErrorCodes.SLOT_NOT_AVAILABLE));
            }
            Status = AvailabilityStatuses.Blocked;
        }

        public void MarkAvailable()
        {
            if (Deleted || Status != AvailabilityStatuses.Booked)
            {
                throw new BusinessRuleException(
                    "Solo un turno reservado puede volver a estar disponible.",
                    nameof(ErrorCodes.APPOINTMENT_CONFLICT));
            }

            Status = AvailabilityStatuses.Available;
        }

        public void Delete()
        {
            Deleted = true;
        }
    }
}
