using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Constants;

namespace Dsw2026Tpi.Domain.Entities
{
    public class AvailabilityRule : EntityBase
    {
        private readonly List<AvailabilitySlot> _slots = [];
        public Guid DoctorId { get; private set; }

        public Doctor Doctor { get; private set; } = null!;

        public byte Month { get; private set; }

        public short Year { get; private set; }

        public byte DayOfWeek { get; private set; } // 0=Lunes...6=Domingo (convención del equipo)

        public TimeSpan StartTime { get; private set; }

        public TimeSpan EndTime { get; private set; }

        public bool Deleted { get; private set; }

        //Turnos de 30 minutos de esta regla. La lista es privada: los turnos los crea la regla (<see cref="AddSlot"/>).
        public IReadOnlyCollection<AvailabilitySlot> Slots => _slots.AsReadOnly();
        private AvailabilityRule()
        {
        }

        public AvailabilityRule(
            Doctor doctor,
            byte month,
            short year,
            byte dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime,
            Guid? id = null) : base(id)
        {

            Validate(
                doctor, 
                month, 
                year, 
                dayOfWeek, 
                startTime, 
                endTime);

            Doctor = doctor;
            DoctorId = doctor.Id;
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

        //Creador: la disponibilidad crea sus turnos

        //Crea y registra un turno de esta regla.
        public AvailabilitySlot AddSlot(DateOnly slotDate, TimeSpan startTime, TimeSpan endTime)
        {
            var slot = new AvailabilitySlot(this, DoctorId, slotDate, startTime, endTime);

            _slots.Add(slot);

            return slot;
        }

        //Da de baja los turnos futuros que todavía no fueron reservados.
        public void DeleteFreeFutureSlots(DateOnly today, TimeSpan nowTime, DateTime now)
        {
            foreach (var slot in _slots.Where(s => !s.Deleted))
            {
                var isFuture = slot.SlotDate > today || (slot.SlotDate == today && slot.StartTime > nowTime);
                var isBooked = slot.Status == AvailabilityStatuses.Booked;

                if (isFuture && !isBooked)
                {
                    slot.Delete();
                    slot.UpdatedAt = now;
                }
            }
        }

        private static void Validate(
            Doctor doctor,
            byte month,
            short year,
            byte dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            if (doctor is null || doctor.Id == Guid.Empty)
            {
                throw new ValidationException(
                    "El médico indicado no es válido.",
                    nameof(ErrorCodes.DOCTOR_NOT_FOUND));
            }

            if (doctor.Deleted)
            {
                throw new ValidationException(
                    "No se puede crear disponibilidad para un médico eliminado.",
                    nameof(ErrorCodes.DOCTOR_NOT_FOUND));
            }

            if (month is < 1 or > 12)
            {
                throw new ValidationException(
                    "El mes no es válido.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "month",
                        "Debe estar entre 1 y 12.");
            }

            if (year <= 0)
            {
                throw new ValidationException(
                    "El año no es válido.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "year",
                        "Debe ser un año válido.");
            }

            if (dayOfWeek > 6)
            {
                throw new ValidationException(
                    "El día de la semana no es válido.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "dayOfWeek",
                        "Debe estar entre 0 (lunes) y 6 (domingo).");
            }

            if (startTime >= endTime)
            {
                throw new ValidationException(
                    "El rango horario no es válido.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "startTime",
                        "Debe ser menor que endTime.");
            }

            if ((endTime - startTime).TotalMinutes % 30 != 0)
            {
                throw new ValidationException(
                    "El rango horario no es válido.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "endTime",
                        "La duración debe ser múltiplo exacto de 30 minutos.");
            }
        }
    }

}
