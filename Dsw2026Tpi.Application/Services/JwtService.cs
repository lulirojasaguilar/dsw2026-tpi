using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Dsw2026Tpi.Application.Services;

public class JwtService
{
    private readonly IConfiguration _config;
    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(string username, string? role, Guid? patientId = null, long? dni = null)
    {
        var jwtConfig = _config.GetSection("Jwt");

        var keyText = jwtConfig["Key"]
            ?? throw new ArgumentNullException("Jwt Key");

        var issuer = jwtConfig["Issuer"]
            ?? throw new ArgumentNullException("Jwt Issuer");

        var audience = jwtConfig["Audience"]
            ?? throw new ArgumentNullException("Jwt Audience");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(keyText));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var expiresIn = int.Parse(
            jwtConfig["ExpiresInMinutes"] ?? "60");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, username),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role ?? string.Empty)
        };

        if (patientId.HasValue)
        {
            claims.Add(
                new Claim(
                    "patientId",
                    patientId.Value.ToString()));
        }

        if (dni.HasValue)
        {
            claims.Add(
                new Claim(
                    "dni",
                    dni.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresIn),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}
