namespace Dsw2026Tpi.Domain.Constants
{
    public static class AvailabilityStatuses
    {
        public const string Available = "AVAILABLE";
        public const string Booked = "BOOKED";
        public const string Blocked = "BLOCKED";

        public static readonly IReadOnlyCollection<string> All = [Available, Booked, Blocked];

        public static bool IsValid(string status) => All.Contains(status);
    }
}
