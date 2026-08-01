using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Constants;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Appointment : EntityBase
    {
        public Guid AvailabilitySlotId { get; private set; }

        public AvailabilitySlot AvailabilitySlot { get; private set; } = null!;

        public Guid DoctorId { get; private set; }

        public Doctor Doctor { get; private set; } = null!;

        public Guid PatientId { get; private set; }

        public Patient Patient { get; private set; } = null!;

        public string Reason { get; private set; } = string.Empty;

        public string Status { get; private set; } = string.Empty;

        public DateTime? CancelledAt { get; private set; }

        public DateTime? AttendedAt { get; private set; }

        private Appointment()
        {
        }

        public Appointment(
            AvailabilitySlot slot,
            Patient patient,
            string reason,
            Guid? id = null) : base(id)
        {
            Validate(slot, patient, reason);

            AvailabilitySlot = slot;
            AvailabilitySlotId = slot.Id;
            DoctorId = slot.DoctorId;
            Doctor = slot.AvailabilityRule.Doctor;
            Patient = patient;
            PatientId = patient.Id;
            Reason = reason.Trim();
            Status = AppointmentStatuses.Booked;
        }

        public void Cancel()
        {
            if (Status != AppointmentStatuses.Booked)
            {
                throw new BusinessRuleException(
                    "La cita no se puede cancelar porque no está reservada.",
                    nameof(ErrorCodes.APPOINTMENT_NOT_CANCELLABLE));
            }

            Status = AppointmentStatuses.Cancelled;
            CancelledAt = DateTime.UtcNow;
        }

        public void MarkAttended()
        {
            EnsureBookedForClosure();

            Status = AppointmentStatuses.Attended;
            AttendedAt = DateTime.UtcNow;
        }

        public void MarkNoShow()
        {
            EnsureBookedForClosure();

            Status = AppointmentStatuses.NoShow;
        }

        private void EnsureBookedForClosure()
        {
            if (Status != AppointmentStatuses.Booked)
            {
                throw new BusinessRuleException(
                    "La cita no puede cerrarse porque no está reservada.",
                    nameof(ErrorCodes.APPOINTMENT_NOT_UPDATABLE));
            }
        }

        private static void Validate(
            AvailabilitySlot slot, 
            Patient patient, 
            string reason)
        {
            if (slot is null || slot.Id == Guid.Empty)
            {
                throw new ValidationException(
                    "El turno/disponibilidad indicado no es válido.",
                    nameof(ErrorCodes.AVAILABILITY_NOT_FOUND));
            }

            if (patient is null || patient.Id == Guid.Empty)
            {
                throw new ValidationException(
                    "El paciente indicado no es válido.",
                    nameof(ErrorCodes.PATIENT_NOT_FOUND));
            }

            var normalizedReason = reason?.Trim() ??
                string.Empty;

            if (normalizedReason.Length is < 5 or > 300)
            {
                throw new ValidationException(
                    "El motivo de la consulta no es válido.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "reason", 
                        "La longitud debe estar entre 5 y 300 caracteres.");
            }
        }
    }
}
