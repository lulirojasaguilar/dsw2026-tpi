namespace Dsw2026Tpi.Domain.Interfaces
{
    public interface IClinicClock
    {
        DateTime Now { get; }

        DateOnly Today { get; }
    }
}
