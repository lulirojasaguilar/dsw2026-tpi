using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.CrossCutting.Exceptions;

/// <summary>
/// Excepción que se lanza cuando el usuario no tiene permisos suficientes.
/// </summary>
public class AuthorizationException : AppException
{
    public AuthorizationException()
        : this(nameof(ErrorCodes.AUTHORIZATION_FAILED))
    {
    }

    public AuthorizationException(string errorCode)
    : base(
        GetMessage(errorCode),
        errorCode)
    {
    }

    private static string GetMessage(string errorCode)
    {
        return errorCode switch
        {
            nameof(ErrorCodes.AUTHORIZATION_FAILED) =>
                 ErrorCodes.AUTHORIZATION_FAILED,

            nameof(ErrorCodes.PATIENT_MISMATCH) =>
                 ErrorCodes.PATIENT_MISMATCH,


            _ => ErrorCodes.AUTHORIZATION_FAILED
        };
    }

}
