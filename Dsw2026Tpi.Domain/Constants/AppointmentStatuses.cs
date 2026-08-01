namespace Dsw2026Tpi.Domain.Constants
{
    public static class AppointmentStatuses
    {
        public const string Booked = "BOOKED";
        public const string Cancelled = "CANCELLED";
        public const string Attended = "ATTENDED";
        public const string NoShow = "NO_SHOW";

        public static readonly IReadOnlyCollection<string> All = [
            Booked, 
            Cancelled, 
            Attended, 
            NoShow];

        public static bool IsValid(string status) => All.Contains(status);
    }
}
