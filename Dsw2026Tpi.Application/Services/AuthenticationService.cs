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

namespace Dsw2026Tpi.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISignInService _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IPersistence _persistence;
    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        ISignInService signInManager,
        RoleManager<IdentityRole> roleManager,
        JwtService jwtService,
        ILogger<AuthenticationService> logger,
        IPersistence persistence)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _logger = logger;
        _persistence = persistence;
    }

    public async Task<LoginAdminModel.Response> LoginAdmin(LoginAdminModel.Request request)
    {
        var validationErrors = new List<(string Field, string Issue)>();

        if (!request.Email.IsEmailValid())
        {
            validationErrors.Add((
                "email",
                "El email es obligatorio y debe tener un formato válido."
            ));
        }

        if (string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 8)
        {
            validationErrors.Add((
                "password",
                "La contraseña es obligatoria y debe contener al menos 8 caracteres."
            ));
        }

        if (validationErrors.Count > 0)
        {
            _logger.LogInformation(
                "Intento de login de administrador fallido por datos inválidos");

            throw new ValidationException()
                .WithDetail(validationErrors);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || user.Deleted)
        {
            _logger.LogInformation(
                "Intento de login de administrador fallido para. Usuario inexistente o eliminado"
                );

            throw new AuthenticationException();
        }

        var result = await _signInManager.CheckPassword(user, request.Password);

        if (!result)
        {
            _logger.LogInformation(
                "Intento de login de administrador fallido para. Contraseña incorrecta"
                );

            throw new AuthenticationException();
        }

        var roles = await _userManager.GetRolesAsync(user);

        var adminRole = roles.FirstOrDefault(role =>
            string.Equals(
                role,
                Roles.Administrator,
                StringComparison.OrdinalIgnoreCase));

        if (adminRole is null)
        {
            _logger.LogWarning(
                "Intento de acceso administrativo rechazado porque el usuario no posee el rol requerido.");

            throw new AuthenticationException();
        }

        var username = user.UserName
            ?? user.Email
            ?? throw new AuthenticationException();

        var token = _jwtService.GenerateToken(user.UserName!, adminRole);

        _logger.LogInformation(
            "Login de administrador realizado correctamente."
            );

        return new LoginAdminModel.Response(
            token,
            adminRole.ToUpperInvariant()
        );
    }

    public async Task<LoginPatientModel.Response> LoginPatient(
        LoginPatientModel.Request request)
    {
        
        var validationErrors = new List<(string Field, string Issue)>();

        if (!request.Email.IsEmailValid())
        {
            validationErrors.Add((
                "email",
                "El email es obligatorio y debe tener un formato válido."
            ));
        }

        var dniLength = request.Dni > 0
            ? request.Dni.ToString().Length
            : 0;

        if (dniLength is not 7 and not 8)
        {
            validationErrors.Add((
                "dni",
                "El DNI es obligatorio y debe contener 7 u 8 dígitos numéricos."
            ));
        }

        if (validationErrors.Count > 0)
        {
            _logger.LogInformation(
                "Intento de login de paciente fallido por datos inválidos");

            throw new ValidationException()
                .WithDetail(validationErrors);
        }

        
        var patient = await _persistence.First<Patient>(
            p => p.Dni == request.Dni && !p.Deleted);

        ApplicationUser user;

        if (patient is null)
        {
            
            user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                Deleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createUserResult = await _userManager.CreateAsync(user);

            if (!createUserResult.Succeeded)
            {
                _logger.LogInformation(
                    "Autoregistro de paciente fallido."
                    );

                throw new ConflictException(
                    nameof(ErrorCodes.REGISTER_USER_CONFLICT),
                    ErrorCodes.REGISTER_USER_CONFLICT)
                    .WithDetail(
                        createUserResult.Errors.Select(
                            error => (error.Code, error.Description)
                        )
                    );
            }

            
            if (!await _roleManager.RoleExistsAsync(Roles.Patient))
            {
                var createRoleResult = await _roleManager.CreateAsync(
                          new IdentityRole(Roles.Patient));

                if (!createRoleResult.Succeeded)
                {
                    _logger.LogError(
                        "No se pudo crear el rol Paciente.");

                    throw new ConflictException(
                        nameof(ErrorCodes.REGISTER_USER_CONFLICT),
                        ErrorCodes.REGISTER_USER_CONFLICT)
                        .WithDetail(
                            createRoleResult.Errors.Select(
                                error => (error.Code, error.Description)
                            )
                        );
                }
            }

            
            var addRoleResult = await _userManager.AddToRoleAsync(
                user,
                Roles.Patient
            );

            if (!addRoleResult.Succeeded)
            {
                _logger.LogInformation(
                    "No se pudo asignar el rol Paciente al usuario"
                    );

                throw new ConflictException(
                    nameof(ErrorCodes.REGISTER_USER_CONFLICT),
                    ErrorCodes.REGISTER_USER_CONFLICT)
                    .WithDetail(
                        addRoleResult.Errors.Select(
                            error => (error.Code, error.Description)
                        )
                    );
            }

            
            patient = new Patient(
                request.Dni,
                request.Email,
                user.Id
            )
            {
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _persistence.Add(patient);
            await _persistence.SaveChangesAsync();

            _logger.LogInformation(
                "Paciente autoregistrado correctamente."
            );
        }
        else
        {
            
            if (!string.Equals(
                    patient.Email,
                    request.Email,
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Login de paciente fallido. El email no coincide con el DNI"
                );

                throw new AuthenticationException();
            }

            user = await _userManager.FindByIdAsync(
                patient.ApplicationUserId)
                ?? throw new AuthenticationException();

            if (user.Deleted)
            {
                _logger.LogInformation(
                    "Login de paciente fallido. Usuario eliminado.");

                throw new AuthenticationException();
            }

            _logger.LogInformation(
                "Login de paciente existente realizado correctamente.");
        }


        var roles = await _userManager.GetRolesAsync(user);

        var patientRole = roles.FirstOrDefault(role =>
            string.Equals(
        role,
        Roles.Patient,
        StringComparison.OrdinalIgnoreCase));

        if (patientRole is null)
        {
            _logger.LogWarning(
                "Login de paciente rechazado porque el usuario no posee el rol requerido.");

            throw new AuthenticationException();
        }

        var username = user.UserName
            ?? user.Email
            ?? throw new AuthenticationException();

        var token = _jwtService.GenerateToken(
            username,
            patientRole,
            patient.Id,
            patient.Dni);

        return new LoginPatientModel.Response(
            token,
            patientRole.ToUpperInvariant()
        );
    }
}
