using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Data.Services
{
    public class ClinicClock : IClinicClock
    {
        private static readonly string[] TimeZoneIds =
        [
            "America/Argentina/Buenos_Aires",
            "Argentina Standard Time"
        ];

        private readonly TimeZoneInfo _timeZone = Resolve();

        public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);

        public DateOnly Today => DateOnly.FromDateTime(Now);

        private static TimeZoneInfo Resolve()
        {
            foreach (var id in TimeZoneIds)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return TimeZoneInfo.Local;
        }
    }
}
