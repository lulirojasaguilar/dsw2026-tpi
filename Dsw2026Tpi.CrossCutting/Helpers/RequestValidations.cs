using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.CrossCutting.Helpers;

public static class RequestValidations
{
    public const int MinNameFilterLength = 3;
    public const int MaxNameFilterLength = 100;

    public static void ValidatePagination(int pageSize, int pageIndex)
    {
        if (pageSize <= 0)
        {
            throw new ValidationException(
                "El tamaño de página debe ser mayor que cero.",
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("pageSize", "Debe ser mayor que cero.");
        }

        if (pageIndex < 0)
        {
            throw new ValidationException(
                "El índice de página no es válido.",
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("pageIndex", "No puede ser negativo (1 es la primera página).");
        }
    }

    public static string? NormalizeNameFilter(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var normalized = name.Trim();

        if (normalized.Length is < MinNameFilterLength or > MaxNameFilterLength)
        {
            throw new ValidationException(
                "El filtro por nombre debe tener entre 3 y 100 caracteres.",
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("name", "Debe tener entre 3 y 100 caracteres.");
        }

        return normalized;
    }
}
