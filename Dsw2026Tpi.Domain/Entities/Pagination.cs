using System.Text.Json.Serialization;

namespace Dsw2026Tpi.Domain.Entities;

public record Pagination<T>(
    [property: JsonPropertyOrder(0)] int PageSize,
    [property: JsonPropertyOrder(1)] int PageIndex,
    [property: JsonPropertyOrder(3)] int Total,
    [property: JsonPropertyOrder(2)] IEnumerable<T> Data)
{
    public Pagination<TMap> Map<TMap>(Func<T, TMap> map) => new(PageSize, PageIndex, Total, Data.Select(map));

    public static Pagination<T> Empty => new(0, 0, 0, []);
};
