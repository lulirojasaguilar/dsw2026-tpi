using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.CrossCutting.Exceptions;

/// <summary>
/// Excepción que se lanza cuando una entidad no se encuentra en la base de datos.
/// </summary>
public class EntityNotFoundException : AppException
{
    public EntityNotFoundException(string entityName)
        : base(
            string.Format(ErrorCodes.ENTITY_NOT_FOUND, entityName),
            nameof(ErrorCodes.ENTITY_NOT_FOUND))
    {
    }

    public EntityNotFoundException(
        string message,
        string specificErrorCode)
        : base(message, specificErrorCode)
    {
    }
}
