using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Appointment : EntityBase
    {
        public Guid DoctorId { get; private set; }

        public Guid AvailabilityId { get; private set; }

        public Guid PatientId { get; private set; }

        public string Reason { get; private set; }

        public string Status { get; private set; }

        protected Appointment()
        {
        }

        public Appointment(
            Guid doctorId,
            Guid availabilityId,
            Guid patientId,
            string reason)
        {
            if (doctorId == Guid.Empty)
            {
                throw new ArgumentException(
                    "DoctorId es obligatorio.",
                    nameof(doctorId));
            }

            if (availabilityId == Guid.Empty)
            {
                throw new ArgumentException(
                    "AvailabilityId es obligatorio.",
                    nameof(availabilityId));
            }

            if (patientId == Guid.Empty)
            {
                throw new ArgumentException(
                    "PatientId es obligatorio.",
                    nameof(patientId));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException(
                    "Reason es obligatorio.",
                    nameof(reason));
            }

            if (reason.Trim().Length < 5)
            {
                throw new ArgumentException(
                    "Reason debe tener al menos 5 caracteres.",
                    nameof(reason));
            }

            DoctorId = doctorId;
            AvailabilityId = availabilityId;
            PatientId = patientId;
            Reason = reason.Trim();
            Status = AppointmentStatuses.Booked;
        }

        public void Cancel()
        {
            if (Status != AppointmentStatuses.Booked)
            {
                throw new BusinessRuleException(
                    "Solo se puede cancelar un turno reservado.",
                    "INVALID_APPOINTMENT_STATUS");
            }

            Status = AppointmentStatuses.Cancelled;
        }
    }
}
