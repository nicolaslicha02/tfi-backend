using FluentValidation;
using GestionReservas.Application.Common.Exceptions;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Features.Auth.DTOs;
using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ValidationException = GestionReservas.Application.Common.Exceptions.ValidationException;

namespace GestionReservas.Application.Features.Auth.Services;

/// <summary>
/// Implementación de los casos de uso de autenticación y consulta de perfil.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IValidator<LoginRequestDto> _loginValidator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IValidator<LoginRequestDto> loginValidator,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _loginValidator = loginValidator;
        _logger = logger;
    }

    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Validación de entrada con FluentValidation
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 2. Consulta de usuario con sus roles y permisos
        var usuario = await _context.Usuarios
            .Include(u => u.UsuarioRoles)
                .ThenInclude(ur => ur.Rol)
                    .ThenInclude(r => r.RolPermisos)
                        .ThenInclude(rp => rp.Permiso)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        // 3. Validación de existencia y contraseña (mensaje genérico)
        if (usuario == null)
        {
            await RegistrarAuditoriaAsync(
                usuarioId: null,
                accion: "LoginFallido",
                entidadId: normalizedEmail,
                motivo: "Credenciales inválidas (usuario no encontrado)",
                detalle: $"IP: {ipAddress ?? "desconocida"}",
                cancellationToken: cancellationToken);

            _logger.LogWarning("Intento de login fallido para el email '{Email}': usuario no encontrado.", normalizedEmail);
            throw new UnauthorizedException("Credenciales inválidas.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, usuario.PasswordHash))
        {
            await RegistrarAuditoriaAsync(
                usuarioId: usuario.Id,
                accion: "LoginFallido",
                entidadId: usuario.Id.ToString(),
                motivo: "Credenciales inválidas (contraseña incorrecta)",
                detalle: $"IP: {ipAddress ?? "desconocida"}",
                cancellationToken: cancellationToken);

            _logger.LogWarning("Intento de login fallido para el usuario ID {UsuarioId}: contraseña no coincide.", usuario.Id);
            throw new UnauthorizedException("Credenciales inválidas.");
        }

        // 4. Rechazar usuarios inactivos o dados de baja
        if (!usuario.Activo)
        {
            await RegistrarAuditoriaAsync(
                usuarioId: usuario.Id,
                accion: "LoginFallido",
                entidadId: usuario.Id.ToString(),
                motivo: "Usuario inactivo o dado de baja",
                detalle: $"IP: {ipAddress ?? "desconocida"}",
                cancellationToken: cancellationToken);

            _logger.LogWarning("Intento de login rechazado para el usuario ID {UsuarioId}: cuenta inactiva.", usuario.Id);
            throw new UnauthorizedException("El usuario se encuentra inhabilitado para operar en el sistema.");
        }

        // 5. Consolidación de roles y permisos
        var roles = usuario.UsuarioRoles
            .Select(ur => ur.Rol.Nombre)
            .Distinct()
            .ToList();

        var permisos = usuario.UsuarioRoles
            .SelectMany(ur => ur.Rol.RolPermisos)
            .Select(rp => rp.Permiso.Codigo)
            .Distinct()
            .ToList();

        // 6. Generación del token JWT
        var token = _jwtTokenService.GenerateToken(usuario, roles, permisos, out var expiresAt);

        // 7. Registro de auditoría exitoso
        await RegistrarAuditoriaAsync(
            usuarioId: usuario.Id,
            accion: "LoginExitoso",
            entidadId: usuario.Id.ToString(),
            motivo: "Inicio de sesión correcto",
            detalle: $"Roles: {string.Join(", ", roles)} | IP: {ipAddress ?? "desconocida"}",
            cancellationToken: cancellationToken);

        _logger.LogInformation("Usuario ID {UsuarioId} ({Email}) inició sesión exitosamente.", usuario.Id, usuario.Email);

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            Usuario = new UsuarioInfoDto
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Roles = roles
            }
        };
    }

    public async Task<CurrentUserProfileDto> GetCurrentUserProfileAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.UsuarioRoles)
                .ThenInclude(ur => ur.Rol)
                    .ThenInclude(r => r.RolPermisos)
                        .ThenInclude(rp => rp.Permiso)
            .FirstOrDefaultAsync(u => u.Id == usuarioId, cancellationToken);

        if (usuario == null)
        {
            throw new NotFoundException(nameof(Usuario), usuarioId);
        }

        var roles = usuario.UsuarioRoles
            .Select(ur => ur.Rol.Nombre)
            .Distinct()
            .ToList();

        var permisos = usuario.UsuarioRoles
            .SelectMany(ur => ur.Rol.RolPermisos)
            .Select(rp => rp.Permiso.Codigo)
            .Distinct()
            .ToList();

        // Determinar departamento asociado (ej. para responsables o docentes de departamento)
        DepartamentoSimpleDto? departamentoDto = null;
        if (roles.Contains("ResponsableEspacio") || usuario.Email.Contains("sistemas"))
        {
            var depto = await _context.Departamentos
                .FirstOrDefaultAsync(d => d.Nombre.Contains("Sistemas"), cancellationToken);

            if (depto != null)
            {
                departamentoDto = new DepartamentoSimpleDto
                {
                    Id = depto.Id,
                    Nombre = depto.Nombre,
                    Descripcion = depto.Descripcion
                };
            }
        }
        else if (roles.Contains("SecretariaAcademica") || usuario.Email.Contains("secretaria"))
        {
            var depto = await _context.Departamentos
                .FirstOrDefaultAsync(d => d.Nombre.Contains("Secretaría Académica"), cancellationToken);

            if (depto != null)
            {
                departamentoDto = new DepartamentoSimpleDto
                {
                    Id = depto.Id,
                    Nombre = depto.Nombre,
                    Descripcion = depto.Descripcion
                };
            }
        }

        return new CurrentUserProfileDto
        {
            Id = usuario.Id,
            NombreCompleto = usuario.NombreCompleto,
            Email = usuario.Email,
            Telefono = usuario.Telefono,
            Activo = usuario.Activo,
            FechaCreacion = usuario.FechaCreacion,
            Roles = roles,
            Permisos = permisos,
            Departamento = departamentoDto
        };
    }

    private async Task RegistrarAuditoriaAsync(
        int? usuarioId,
        string accion,
        string entidadId,
        string motivo,
        string detalle,
        CancellationToken cancellationToken)
    {
        try
        {
            var auditoria = new Auditoria
            {
                UsuarioId = usuarioId,
                FechaHora = DateTime.UtcNow,
                Accion = accion,
                Entidad = "Usuario",
                EntidadId = entidadId,
                Motivo = motivo,
                Detalle = detalle
            };

            _context.Auditorias.Add(auditoria);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar registro de auditoría para la acción {Accion}.", accion);
            // No interrumpir el flujo si la auditoría falla
        }
    }
}
