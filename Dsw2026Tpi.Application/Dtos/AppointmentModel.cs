namespace Dsw2026Tpi.Application.Dtos
{
    public record AppointmentModel
    {
        public record Request(
            Guid DoctorId,
            Guid AvailabilityId,
            PatientRequestData Patient,
            string Reason
        );

        public record PatientRequestData(
            long Dni
        );

        public record Response(
            Guid Id,
            string Status,
            DateOnly Date,
            TimeSpan StartTime
        );
    }
}