using System.Security.Claims;
using System.Text.Json;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using System.Threading.RateLimiting;

namespace Dsw2026Tpi.Api.Configurations
{
    public static class RateLimitingConfigurationExtensions
    {
        public const string AdminLoginPolicy = "AdminLogin";
        public const string PatientLoginPolicy = "PatientLogin";
        public const string AppointmentBookingPolicy = "AppointmentBooking";

        public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection("RateLimiting");

            var generalPermitLimit = section.GetValue("GeneralPermitLimit", 100);
            var generalWindowSeconds = section.GetValue("GeneralWindowSeconds", 60);

            var adminLoginPermitLimit = section.GetValue("AdminLoginPermitLimit", 5);
            var adminLoginWindowSeconds = section.GetValue("AdminLoginWindowSeconds", 60);

            var patientLoginPermitLimit = section.GetValue("PatientLoginPermitLimit", 10);
            var patientLoginWindowSeconds = section.GetValue("PatientLoginWindowSeconds", 60);

            var appointmentBookingPermitLimit = section.GetValue("AppointmentBookingPermitLimit", 5);
            var appointmentBookingWindowSeconds = section.GetValue("AppointmentBookingWindowSeconds", 60);

            services.AddRateLimiter(options =>
            {
                // Política general: 100/min por usuario autenticado o, si no hay usuario, por IP.
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
                        ? $"user:{httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown"}"
                        : $"ip:{httpContext.Connection.RemoteIpAddress}";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = generalPermitLimit,
                            Window = TimeSpan.FromSeconds(generalWindowSeconds),
                            QueueLimit = 0
                        });
                });

                // Login admin: 5/min por IP.
                options.AddPolicy(AdminLoginPolicy, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = adminLoginPermitLimit,
                            Window = TimeSpan.FromSeconds(adminLoginWindowSeconds),
                            QueueLimit = 0
                        }));

                // Login paciente: 10/min por IP.
                options.AddPolicy(PatientLoginPolicy, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = patientLoginPermitLimit,
                            Window = TimeSpan.FromSeconds(patientLoginWindowSeconds),
                            QueueLimit = 0
                        }));

                // Reserva de turnos: 5/min por paciente autenticado.
                options.AddPolicy(AppointmentBookingPolicy, httpContext =>
                {
                    var patientKey = httpContext.User.FindFirst(AppClaims.PatientId)?.Value
                        ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        patientKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = appointmentBookingPermitLimit,
                            Window = TimeSpan.FromSeconds(appointmentBookingWindowSeconds),
                            QueueLimit = 0
                        });
                });

                options.OnRejected = async (context, cancellationToken) =>
                {
                    Log.Warning(
                        "Rate limit excedido. Path={Path} RemoteIp={RemoteIp}",
                        context.HttpContext.Request.Path,
                        context.HttpContext.Connection.RemoteIpAddress);

                    context.HttpContext.Response.ContentType = "application/json";
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                    var error = new ErrorResponse(nameof(ErrorCodes.RATE_LIMIT_EXCEEDED), ErrorCodes.RATE_LIMIT_EXCEEDED);

                    await context.HttpContext.Response.WriteAsync(
                        JsonSerializer.Serialize(error, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                        cancellationToken);
                };
            });

            return services;
        }
    }
}
