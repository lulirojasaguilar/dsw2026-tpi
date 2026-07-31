using System.Text;
using System.Text.Json;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Dsw2026Tpi.Api.Configurations;

public static class SecurityConfigurationExtensions
{
    public static IServiceCollection AddAppIdentity(
        this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password = new PasswordOptions
            {
                RequiredLength = 8,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireDigit = true,
                RequireNonAlphanumeric = false
            };
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AuthenticationDbContext>()
        .AddSignInManager();

        return services;
    }

    public static IServiceCollection AddAppAuthentication(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        //Obtener parámetros para creación del JWT desde appsettings.json
        var jwtConfig = configuration.GetSection("Jwt");
        var keyText = jwtConfig["Key"] ?? throw new ArgumentNullException("JWT Key");
        var issuer = jwtConfig["Issuer"] ?? throw new ArgumentNullException("JWT Issuer");
        var audience = jwtConfig["Audience"] ?? throw new ArgumentNullException("JWT Audience");
        var key = Encoding.UTF8.GetBytes(keyText);

        //Agregar autenticación
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
            {
                //Definir parámetros para la generación del token
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        return WriteErrorAsync(context.Response, nameof(ErrorCodes.AUTHENTICATION_FAILED), ErrorCodes.AUTHENTICATION_FAILED, StatusCodes.Status401Unauthorized);
                    },  

                    OnForbidden = context =>
                        WriteErrorAsync(context.Response, nameof(ErrorCodes.AUTHORIZATION_FAILED), ErrorCodes.AUTHORIZATION_FAILED, StatusCodes.Status403Forbidden)
                    };
            });
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.AdminPolicy, policy =>
                policy.RequireRole(Roles.Administrator))
            .AddPolicy(Policies.PatientPolicy, policy =>
                policy.RequireRole(Roles.Patient));
        return services;
    }

    private static async Task WriteErrorAsync(HttpResponse response, string errorCode, string message, int statusCode)
    {
        if (response.HasStarted)
        {
            return;
        }

        response.ContentType = "application/json";
        response.StatusCode = statusCode;

        var error = new ErrorResponse(errorCode, message);

        await response.WriteAsync(JsonSerializer.Serialize(
            error,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    public static IServiceCollection AddAppCors(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        //Obtener configuración para CORS desde appsettings.json
        var allowedOrigins = configuration
                            .GetSection("Cors:AllowedOrigins")
                            .Get<string[]>()?
                            .Where(origin => !string.IsNullOrWhiteSpace(origin))
                            .Select(origin => origin.TrimEnd('/'))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToArray();

        //Si no se definió configuración en el archivo, utilizar la que se define
        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            allowedOrigins =
            [
                "http://localhost",
                "https://localhost"
            ];
        }

        //Agregar CORS con la política por defecto a partir de las URLs definidas
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                     .AllowAnyHeader()
                     .AllowAnyMethod()
                     .AllowCredentials();
            });
        });

        return services;
    }
}
