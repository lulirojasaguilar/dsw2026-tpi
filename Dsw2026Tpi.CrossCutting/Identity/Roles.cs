namespace Dsw2026Tpi.CrossCutting.Identity;

public class Roles
{
    public const string Administrator = "Administrador";
    public const string Patient = "Paciente";

    public static string ToContractValue(string internalRoleName) => internalRoleName.ToUpperInvariant();
}
