namespace Dsw2026Tpi.Domain.Interfaces
{
    public interface IHolidayProvider
    {
        IReadOnlySet<DateOnly> GetHolidays(int year);

        bool IsHoliday(DateOnly date);
    }
}
