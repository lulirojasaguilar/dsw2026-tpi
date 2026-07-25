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

        if (user is null)
        {
            _logger.LogInformation(
                "Intento de login de administrador fallido para: {Email}. Usuario inexistente",
                request.Email);

            throw new AuthenticationException();
        }

        var result = await _signInManager.CheckPassword(user, request.Password);

        if (!result)
        {
            _logger.LogInformation(
                "Intento de login de administrador fallido para: {Email}. Contraseña incorrecta",
                request.Email);

            throw new AuthenticationException();
        }

        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? Roles.Administrator;

        var token = _jwtService.GenerateToken(user.UserName!, role);

        _logger.LogInformation(
            "Login de administrador exitoso para: {Email}",
            request.Email);

        return new LoginAdminModel.Response(
            token,
            role.ToUpperInvariant()
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
                    "Autoregistro de paciente fallido para: {Email}",
                    request.Email);

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
                await _roleManager.CreateAsync(
                    new IdentityRole(Roles.Patient)
                );
            }

            
            var addRoleResult = await _userManager.AddToRoleAsync(
                user,
                Roles.Patient
            );

            if (!addRoleResult.Succeeded)
            {
                _logger.LogInformation(
                    "No se pudo asignar el rol Paciente a: {Email}",
                    request.Email);

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
                "Paciente autoregistrado correctamente. DNI: {Dni}",
                request.Dni
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
                    "Login de paciente fallido. El email no coincide con el DNI: {Dni}",
                    request.Dni
                );

                throw new AuthenticationException();
            }

            user = await _userManager.FindByIdAsync(
                patient.ApplicationUserId
            ) ?? throw new AuthenticationException();

            _logger.LogInformation(
                "Login de paciente existente exitoso. DNI: {Dni}",
                request.Dni
            );
        }

       
        var role = (await _userManager.GetRolesAsync(user))
            .FirstOrDefault() ?? Roles.Patient;

        var token = _jwtService.GenerateToken(
            user.UserName!,
            role
        );

        return new LoginPatientModel.Response(
            token,
            role.ToUpperInvariant()
        );
    }

    public async Task<RegisterModel.Response> Register(RegisterModel.Request request)
    {
        if (!request.Email.IsEmailValid()) throw new ValidationException(ErrorCodes.REGISTER_USER_INVALID,
            nameof(ErrorCodes.REGISTER_USER_INVALID));

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded) throw new ConflictException(nameof(ErrorCodes.REGISTER_USER_CONFLICT),
            ErrorCodes.REGISTER_USER_CONFLICT)
                .WithDetail(result.Errors.Select(e => (e.Code, e.Description)));
       
        _ = await _userManager.AddToRoleAsync(user, Roles.Administrator);

        _logger.LogInformation("Usuario registrado: {Email}", request.Email);

        return new RegisterModel.Response(request.Email);
    }
}
