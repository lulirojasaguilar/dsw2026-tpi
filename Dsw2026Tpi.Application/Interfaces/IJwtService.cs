using System.Security.Claims;

namespace Dsw2026Tpi.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(IEnumerable<Claim> claims);

    }
}
