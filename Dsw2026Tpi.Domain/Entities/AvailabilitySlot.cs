using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Constants;


namespace Dsw2026Tpi.Domain.Entities
{
    public class AvailabilitySlot : EntityBase
    {
        private readonly List<Appointment> _appointments = [];
        public Guid AvailabilityRuleId { get; private set; }

        public AvailabilityRule AvailabilityRule { get; private set; } = null!;

        public Guid DoctorId { get; private set; }

        public DateOnly SlotDate { get; private set; }

        public TimeSpan StartTime { get; private set; }

        public TimeSpan EndTime { get; private set; }

        public string Status { get; private set; } = string.Empty;

        public bool Deleted { get; private set; }

        //Historial de citas del turno (una cancelada puede volver a reservarse).
        public IReadOnlyCollection<Appointment> Appointments => _appointments.AsReadOnly();

        //La cita vigente del turno (0..1): la que está reservada, o null si el turno está libre.
        public Appointment? Appointment =>
            _appointments.FirstOrDefault(appointment => appointment.Status == AppointmentStatuses.Booked);
        private AvailabilitySlot() 
        { 
        }

        internal AvailabilitySlot(
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

        //Experto: el turno sabe cuándo empieza.
        public DateTime StartDateTime =>
            SlotDate.ToDateTime(TimeOnly.FromTimeSpan(StartTime));

        public bool HasStarted(DateTime now) => StartDateTime <= now;

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

        internal void RegisterAppointment(Appointment appointment)
        {
            _appointments.Add(appointment);
        }
    }
}
