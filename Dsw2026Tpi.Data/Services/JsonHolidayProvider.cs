using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Data.Services
{
    public class JsonHolidayProvider : IHolidayProvider
    {
        private sealed record FixedHoliday(
    [property: JsonPropertyName("month")] int Month,
    [property: JsonPropertyName("day")] int Day,
    [property: JsonPropertyName("name")] string Name);

        private sealed record MovableHoliday(
            [property: JsonPropertyName("date")] string Date,
            [property: JsonPropertyName("name")] string Name);

        private sealed record HolidaysFile(
            [property: JsonPropertyName("fixed")] List<FixedHoliday> Fixed,
            [property: JsonPropertyName("movable")] List<MovableHoliday> Movable);

        private readonly Lazy<HolidaysFile> _file;
        private readonly ConcurrentDictionary<int, IReadOnlySet<DateOnly>> _cache = new();

        public JsonHolidayProvider()
        {
            _file = new Lazy<HolidaysFile>(Load);
        }

        private static HolidaysFile Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Sources", "holidays.json");

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "No se encontró el catálogo de feriados. Verifique que 'Sources/holidays.json' " +
                    "se copie al directorio de salida (CopyToOutputDirectory).",
                    path);
            }

            var json = File.ReadAllText(path);

            return JsonSerializer.Deserialize<HolidaysFile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new HolidaysFile([], []);
        }

        public IReadOnlySet<DateOnly> GetHolidays(int year)
        {
            return _cache.GetOrAdd(year, y =>
            {
                var holidays = new HashSet<DateOnly>();

                foreach (var fixedHoliday in _file.Value.Fixed)
                {
                    holidays.Add(new DateOnly(y, fixedHoliday.Month, fixedHoliday.Day));
                }

                foreach (var movable in _file.Value.Movable)
                {
                    if (DateOnly.TryParse(movable.Date, out var date) && date.Year == y)
                    {
                        holidays.Add(date);
                    }
                }

                return holidays;
            });
        }

        public bool IsHoliday(DateOnly date) => GetHolidays(date.Year).Contains(date);
    }
}
