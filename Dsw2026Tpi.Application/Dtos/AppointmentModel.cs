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
    }
}