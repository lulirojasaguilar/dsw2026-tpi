namespace Dsw2026Tpi.Application.Dtos
{
    public record AppointmentModel
    {
        public record Request(
            Guid DoctorId,
            Guid AvailabilitySlotId,
            PatientRequestData Patient,
            string Reason
        );

        public record PatientRequestData(
            long Dni
        );

        public record SpecialityDto(
            Guid Id,
            string Name);

        public record DoctorDto(
            Guid Id,
            string Name,
            SpecialityDto Specialty);

        public record PatientDto(
            Guid Id,
            long Dni,
            string? FullName);

        public record Response(
            Guid Id,
            string Status,
            string Reason,
            DateOnly Date,
            string StartTime,
            string EndTime,
            DateTime? CancelledAt,
            DateTime? AttendedAt,
            DoctorDto Doctor,
            PatientDto Patient
        );

        // ---- Respuesta de las búsquedas del administrador (GET /api/appointments?date= y /search).
        // Los nombres son exactamente los que define el enunciado del TPI.
        // date / startTime / endTime son el "availableTime" que muestra la tabla lógica.

        public record SearchSpecialityDto(
            Guid SpecialtyId,
            string Name);

        public record SearchDoctorDto(
            Guid DoctorId,
            string Name,
            SearchSpecialityDto Specialty);

        public record SearchPatientDto(
            long Dni,
            string FullName);

        public record SearchResponse(
            Guid AppointmentsId,
            string AppointmentsStatus,
            SearchPatientDto Patient,
            SearchDoctorDto Doctor,
            DateOnly Date,
            string StartTime,
            string EndTime
        );
    }
}