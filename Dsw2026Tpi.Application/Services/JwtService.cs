using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dsw2026Tpi.Application.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class JwtService : IJwtService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiresInMinutes;

    public JwtService(IConfiguration config)
    {
        var jwtConfig = config.GetSection("Jwt");

        _key = jwtConfig["Key"] ?? throw new ArgumentNullException("Jwt Key");
        _issuer = jwtConfig["Issuer"] ?? throw new ArgumentNullException("Jwt Issuer");
        _audience = jwtConfig["Audience"] ?? throw new ArgumentNullException("Jwt Audience");
        _expiresInMinutes = int.Parse(jwtConfig["ExpiresInMinutes"] ?? "60");
    }

    public string GenerateToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiresInMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}