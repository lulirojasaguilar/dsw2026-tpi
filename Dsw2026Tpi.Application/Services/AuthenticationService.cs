using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ISignInService _signInManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IPersistence _persistence;
    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ISignInService signInManager,
        IPersistence persistence,
        IJwtService jwtService,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _persistence = persistence;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<RegisterModel.Response> Register(RegisterModel.Request request)
    {
        ValidateRequest(request);

        var email = request.Email.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            _logger.LogWarning("Intento de registrar un email ya existente: {Email}", email);
            throw new ConflictException(ErrorCodes.REGISTER_USER_CONFLICT, nameof(ErrorCodes.REGISTER_USER_CONFLICT));
        }

        var now = DateTime.UtcNow;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Deleted = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            var error = new ValidationException(
                ErrorCodes.REGISTER_USER_INVALID,
                nameof(ErrorCodes.REGISTER_USER_INVALID));

            error.WithDetail(createResult.Errors.Select(e => (e.Code, e.Description)));

            _logger.LogWarning(
                "No se pudo registrar el administrador {Email}: {Errors}",
                email, string.Join("; ", createResult.Errors.Select(e => e.Description)));

            throw error;
        }

        await EnsureAdministratorRoleExists();

        var roleResult = await _userManager.AddToRoleAsync(user, Roles.Administrator);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            _logger.LogError("No se pudo asignar el rol Administrador a {Email}: {Errors}", email, errors);

            throw new ConflictException(ErrorCodes.REGISTER_USER_CONFLICT, errors);
        }

        _logger.LogInformation("Administrador registrado: {Email}", email);

        return new RegisterModel.Response(email);
    }

    private async Task EnsureAdministratorRoleExists()
    {
        if (await _roleManager.RoleExistsAsync(Roles.Administrator))
        {
            return;
        }

        var result = await _roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Administrator));

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new ConflictException(ErrorCodes.REGISTER_USER_CONFLICT, errors);
        }
    }

    public async Task<LoginAdminModel.Response> LoginAdmin(LoginAdminModel.Request request)
    {
        ValidateAdminRequest(request);

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || user.Deleted)
        {
            _logger.LogInformation("Intento de login de administrador fallido. Usuario inexistente o eliminado");
            throw new AuthenticationException();
        }

        var result = await _signInManager.CheckPassword(user, request.Password);

        if (!result)
        {
            _logger.LogInformation("Intento de login de administrador fallido. Contraseña incorrecta");
            throw new AuthenticationException();
        }

        var roles = await _userManager.GetRolesAsync(user);

        var adminRole = roles.FirstOrDefault(role =>
            string.Equals(role, Roles.Administrator, StringComparison.OrdinalIgnoreCase));

        if (adminRole is null)
        {
            _logger.LogWarning("Intento de acceso administrativo rechazado. El usuario no posee el rol requerido.");
            throw new AuthenticationException();
        }

        var username = user.UserName ?? user.Email ?? throw new AuthenticationException();

        var token = _jwtService.GenerateToken(BuildClaims(username, adminRole));

        _logger.LogInformation("Login de administrador realizado correctamente.");

        return new LoginAdminModel.Response(token, adminRole.ToUpperInvariant());
    }

    public async Task<LoginPatientModel.Response> LoginPatient(LoginPatientModel.Request request)
    {
        ValidatePatientRequest(request);

        var patient = await _persistence.First<Patient>(
            p => p.Dni == request.Dni.ToString() && !p.Deleted);

        ApplicationUser user;

        if (patient is null)
        {
            user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true,
                Deleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createUserResult = await _userManager.CreateAsync(user);

            if (!createUserResult.Succeeded)
            {
                _logger.LogInformation("Autoregistro de paciente fallido. No se pudo crear el usuario.");

                throw new ConflictException(ErrorCodes.REGISTER_USER_CONFLICT, nameof(ErrorCodes.REGISTER_USER_CONFLICT))
                    .WithDetail(createUserResult.Errors.Select(error => (error.Code, error.Description)));
            }

            try
            {
                if (!await _roleManager.RoleExistsAsync(Roles.Patient))
                {
                    var createRoleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Patient));

                    if (!createRoleResult.Succeeded)
                    {
                        _logger.LogError("No se pudo crear el rol Paciente.");

                        throw new ConflictException(ErrorCodes.REGISTER_USER_CONFLICT, nameof(ErrorCodes.REGISTER_USER_CONFLICT))
                            .WithDetail(createRoleResult.Errors.Select(error => (error.Code, error.Description)));
                    }
                }

                var addRoleResult = await _userManager.AddToRoleAsync(user, Roles.Patient);

                if (!addRoleResult.Succeeded)
                {
                    _logger.LogInformation("No se pudo asignar el rol Paciente al usuario");

                    throw new ConflictException(ErrorCodes.REGISTER_USER_CONFLICT, nameof(ErrorCodes.REGISTER_USER_CONFLICT))
                        .WithDetail(addRoleResult.Errors.Select(error => (error.Code, error.Description)));
                }

                patient = new Patient(user.Id, request.Dni.ToString())
                {
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _persistence.Add(patient);
                await _persistence.SaveChangesAsync();
            }
            catch
            {
                await _userManager.DeleteAsync(user);
                throw;
            }

            _logger.LogInformation("Paciente autoregistrado correctamente.");
        }
        else
        {
            user = await _userManager.FindByIdAsync(patient.ApplicationUserId.ToString())
                ?? throw new AuthenticationException();

            if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Login de paciente fallido. El email no coincide con el DNI");
                throw new AuthenticationException();
            }

            if (user.Deleted)
            {
                _logger.LogInformation("Login de paciente fallido. Usuario eliminado.");
                throw new AuthenticationException();
            }

            _logger.LogInformation("Login de paciente existente realizado correctamente.");
        }

        var roles = await _userManager.GetRolesAsync(user);

        var patientRole = roles.FirstOrDefault(role =>
            string.Equals(role, Roles.Patient, StringComparison.OrdinalIgnoreCase));

        if (patientRole is null)
        {
            _logger.LogWarning("Login de paciente rechazado porque el usuario no posee el rol requerido.");
            throw new AuthenticationException();
        }

        var username = user.UserName ?? user.Email ?? throw new AuthenticationException();

        var token = _jwtService.GenerateToken(BuildClaims(username, patientRole, patient.Id, long.Parse(patient.Dni)));

        _logger.LogInformation("Login de paciente realizado correctamente.");

        return new LoginPatientModel.Response(token, patientRole.ToUpperInvariant());
    }

    private static List<Claim> BuildClaims(string username, string role, Guid? patientId = null, long? dni = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, username),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role)
        };

        if (patientId.HasValue)
        {
            claims.Add(new Claim("patientId", patientId.Value.ToString()));
        }

        if (dni.HasValue)
        {
            claims.Add(new Claim("dni", dni.Value.ToString()));
        }

        return claims;
    }

    private static void ValidateRequest(RegisterModel.Request request)
    {
        var errors = new List<(string Field, string Issue)>();

        if (!request.Email.IsEmailValid())
        {
            errors.Add(("email", "El email es obligatorio y debe tener un formato válido."));
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            errors.Add(("password", "La contraseña es obligatoria y debe contener al menos 8 caracteres."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(ErrorCodes.VALIDATION_ERROR, nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail(errors);
        }
    }

    private static void ValidateAdminRequest(LoginAdminModel.Request request)
    {
        var errors = new List<(string Field, string Issue)>();

        if (!request.Email.IsEmailValid())
        {
            errors.Add(("email", "El email es obligatorio y debe tener un formato válido."));
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            errors.Add(("password", "La contraseña es obligatoria y debe contener al menos 8 caracteres."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(ErrorCodes.VALIDATION_ERROR, nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail(errors);
        }
    }

    private static void ValidatePatientRequest(LoginPatientModel.Request request)
    {
        var errors = new List<(string Field, string Issue)>();

        if (!request.Email.IsEmailValid())
        {
            errors.Add(("email", "El email es obligatorio y debe tener un formato válido."));
        }

        var dniLength = request.Dni > 0 ? request.Dni.ToString().Length : 0;

        if (dniLength is not 7 and not 8)
        {
            errors.Add(("dni", "El DNI es obligatorio y debe contener 7 u 8 dígitos numéricos."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(ErrorCodes.VALIDATION_ERROR, nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail(errors);
        }
    }
}
