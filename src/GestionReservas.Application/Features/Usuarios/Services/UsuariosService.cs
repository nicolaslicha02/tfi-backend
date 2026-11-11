using FluentValidation;
using GestionReservas.Application.Common.Exceptions;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Usuarios.DTOs;
using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ValidationException = GestionReservas.Application.Common.Exceptions.ValidationException;

namespace GestionReservas.Application.Features.Usuarios.Services;

/// <summary>
/// Servicio de lógica de negocio para la administración de usuarios, validaciones y auditoría institucional.
/// </summary>
public class UsuariosService : IUsuariosService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CrearUsuarioRequestDto> _crearValidator;
    private readonly IValidator<ActualizarUsuarioRequestDto> _actualizarValidator;
    private readonly IValidator<UsuariosQueryParameters> _queryValidator;

    public UsuariosService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUserService,
        IValidator<CrearUsuarioRequestDto> crearValidator,
        IValidator<ActualizarUsuarioRequestDto> actualizarValidator,
        IValidator<UsuariosQueryParameters> queryValidator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
        _crearValidator = crearValidator;
        _actualizarValidator = actualizarValidator;
        _queryValidator = queryValidator;
    }

    public async Task<UsuarioDetalleDto> CrearUsuarioAsync(CrearUsuarioRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _crearValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Email único (case-insensitive)
        var emailExiste = await _context.Usuarios
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (emailExiste)
        {
            throw new ConflictException($"El correo electrónico '{request.Email}' ya se encuentra registrado en el sistema.");
        }

        // 2. Validar que los roles solicitados existan
        var requestedRoleNames = request.Roles
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingRoles = await _context.Roles
            .Where(r => requestedRoleNames.Contains(r.Nombre))
            .ToListAsync(cancellationToken);

        if (existingRoles.Count != requestedRoleNames.Count)
        {
            var missingRoles = requestedRoleNames
                .Except(existingRoles.Select(r => r.Nombre), StringComparer.OrdinalIgnoreCase);

            throw new NotFoundException($"Los siguientes roles no existen en el sistema: {string.Join(", ", missingRoles)}.");
        }

        // 3. Validar departamento si fue enviado
        Departamento? departamento = null;
        if (request.DepartamentoId.HasValue)
        {
            departamento = await _context.Departamentos
                .FirstOrDefaultAsync(d => d.Id == request.DepartamentoId.Value, cancellationToken);

            if (departamento == null)
            {
                throw new NotFoundException($"El departamento con ID {request.DepartamentoId.Value} no existe.");
            }
        }

        // 4. Crear entidad de usuario con contraseña hasheada
        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Apellido = request.Apellido.Trim(),
            NombreCompleto = $"{request.Nombre.Trim()} {request.Apellido.Trim()}".Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim(),
            DepartamentoId = request.DepartamentoId,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        foreach (var rol in existingRoles)
        {
            usuario.UsuarioRoles.Add(new UsuarioRol
            {
                Rol = rol,
                Usuario = usuario
            });
        }

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync(cancellationToken);

        // 5. Registro de Auditoría (sin contraseñas)
        var currentUserId = _currentUserService.UsuarioId;
        var auditoria = new Auditoria
        {
            UsuarioId = currentUserId,
            FechaHora = DateTime.UtcNow,
            Accion = "CrearUsuario",
            Entidad = "Usuario",
            EntidadId = usuario.Id.ToString(),
            Motivo = $"Alta de usuario {usuario.Email}",
            Detalle = $"Nombre: {usuario.NombreCompleto}; Roles: {string.Join(", ", existingRoles.Select(r => r.Nombre))}; DeptoId: {usuario.DepartamentoId?.ToString() ?? "Ninguno"}"
        };

        _context.Auditorias.Add(auditoria);
        await _context.SaveChangesAsync(cancellationToken);

        return new UsuarioDetalleDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email,
            Telefono = usuario.Telefono,
            Activo = usuario.Activo,
            DepartamentoId = usuario.DepartamentoId,
            DepartamentoNombre = departamento?.Nombre,
            Roles = existingRoles.Select(r => r.Nombre).ToList(),
            FechaAlta = usuario.FechaCreacion
        };
    }

    public async Task<PagedResultDto<UsuarioListadoDto>> GetUsuariosAsync(UsuariosQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var validationResult = await _queryValidator.ValidateAsync(parameters, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var query = _context.Usuarios
            .Include(u => u.Departamento)
            .Include(u => u.UsuarioRoles)
                .ThenInclude(ur => ur.Rol)
            .AsNoTracking();

        // Filtro por texto (Nombre, Apellido o Email)
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var term = parameters.Search.Trim().ToLower();
            query = query.Where(u =>
                u.Nombre.ToLower().Contains(term) ||
                u.Apellido.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                u.NombreCompleto.ToLower().Contains(term));
        }

        // Filtro por Rol
        if (!string.IsNullOrWhiteSpace(parameters.Rol))
        {
            var rolTerm = parameters.Rol.Trim().ToLower();
            query = query.Where(u => u.UsuarioRoles.Any(ur => ur.Rol.Nombre.ToLower() == rolTerm));
        }

        // Filtro por Departamento
        if (parameters.DepartamentoId.HasValue)
        {
            query = query.Where(u => u.DepartamentoId == parameters.DepartamentoId.Value);
        }

        // Filtro por Activo
        if (parameters.Activo.HasValue)
        {
            query = query.Where(u => u.Activo == parameters.Activo.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.Apellido)
            .ThenBy(u => u.Nombre)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(u => new UsuarioListadoDto
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                Telefono = u.Telefono,
                Activo = u.Activo,
                DepartamentoId = u.DepartamentoId,
                DepartamentoNombre = u.Departamento != null ? u.Departamento.Nombre : null,
                Roles = u.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList(),
                FechaAlta = u.FechaCreacion
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<UsuarioListadoDto>(items, total, parameters.Page, parameters.PageSize);
    }

    public async Task<UsuarioDetalleDto> GetUsuarioByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Departamento)
            .Include(u => u.UsuarioRoles)
                .ThenInclude(ur => ur.Rol)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (usuario == null)
        {
            throw new NotFoundException($"El usuario con ID {id} no fue encontrado.");
        }

        return new UsuarioDetalleDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email,
            Telefono = usuario.Telefono,
            Activo = usuario.Activo,
            DepartamentoId = usuario.DepartamentoId,
            DepartamentoNombre = usuario.Departamento?.Nombre,
            Roles = usuario.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList(),
            FechaAlta = usuario.FechaCreacion
        };
    }

    public async Task<UsuarioDetalleDto> ActualizarUsuarioAsync(int id, ActualizarUsuarioRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _actualizarValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var usuario = await _context.Usuarios
            .Include(u => u.Departamento)
            .Include(u => u.UsuarioRoles)
                .ThenInclude(ur => ur.Rol)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (usuario == null)
        {
            throw new NotFoundException($"El usuario con ID {id} no fue encontrado.");
        }

        var currentUserId = _currentUserService.UsuarioId;
        var isTargetAdmin = usuario.UsuarioRoles.Any(ur => ur.Rol.Nombre.Equals("Administrador", StringComparison.OrdinalIgnoreCase));
        var cambios = new List<string>();

        // 1. Reglas de Desactivación / Activación
        if (request.Activo.HasValue && request.Activo.Value != usuario.Activo)
        {
            if (!request.Activo.Value)
            {
                // Un administrador NO puede desactivarse a sí mismo
                if (currentUserId.HasValue && currentUserId.Value == usuario.Id && isTargetAdmin)
                {
                    throw new ConflictException("Un administrador no puede desactivar su propia cuenta.");
                }

                // Tampoco se puede dejar al sistema sin ningún administrador activo
                if (isTargetAdmin)
                {
                    var activeAdmins = await _context.UsuarioRoles
                        .CountAsync(ur => ur.Rol.Nombre == "Administrador" && ur.Usuario.Activo && ur.UsuarioId != usuario.Id, cancellationToken);

                    if (activeAdmins < 1)
                    {
                        throw new ConflictException("No es posible desactivar al usuario porque el sistema no puede quedar sin ningún administrador activo.");
                    }
                }
            }

            usuario.Activo = request.Activo.Value;
            cambios.Add(usuario.Activo ? "Usuario activado" : "Usuario desactivado");
        }

        // 2. Reglas de Cambio de Roles
        if (request.Roles != null)
        {
            var requestedRoleNames = request.Roles
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existingRoles = await _context.Roles
                .Where(r => requestedRoleNames.Contains(r.Nombre))
                .ToListAsync(cancellationToken);

            if (existingRoles.Count != requestedRoleNames.Count)
            {
                var missingRoles = requestedRoleNames
                    .Except(existingRoles.Select(r => r.Nombre), StringComparer.OrdinalIgnoreCase);

                throw new NotFoundException($"Los siguientes roles no existen en el sistema: {string.Join(", ", missingRoles)}.");
            }

            var willBeAdmin = requestedRoleNames.Contains("Administrador", StringComparer.OrdinalIgnoreCase);

            if (isTargetAdmin && !willBeAdmin)
            {
                // Un administrador no puede quitarse el rol de Administrador a sí mismo
                if (currentUserId.HasValue && currentUserId.Value == usuario.Id)
                {
                    throw new ConflictException("Un administrador no puede quitarse el rol de Administrador a sí mismo.");
                }

                // No se puede dejar al sistema sin administradores activos
                if (usuario.Activo)
                {
                    var activeAdmins = await _context.UsuarioRoles
                        .CountAsync(ur => ur.Rol.Nombre == "Administrador" && ur.Usuario.Activo && ur.UsuarioId != usuario.Id, cancellationToken);

                    if (activeAdmins < 1)
                    {
                        throw new ConflictException("No es posible remover el rol de Administrador porque el sistema no puede quedar sin ningún administrador activo.");
                    }
                }
            }

            // Actualizar roles
            _context.UsuarioRoles.RemoveRange(usuario.UsuarioRoles);
            usuario.UsuarioRoles.Clear();

            foreach (var rol in existingRoles)
            {
                usuario.UsuarioRoles.Add(new UsuarioRol
                {
                    UsuarioId = usuario.Id,
                    RolId = rol.Id,
                    Rol = rol,
                    Usuario = usuario
                });
            }

            cambios.Add($"Roles actualizados a: {string.Join(", ", requestedRoleNames)}");
        }

        // 3. Modificación parcial de campos personales
        if (!string.IsNullOrWhiteSpace(request.Nombre) && request.Nombre.Trim() != usuario.Nombre)
        {
            usuario.Nombre = request.Nombre.Trim();
            usuario.NombreCompleto = $"{usuario.Nombre} {usuario.Apellido}".Trim();
            cambios.Add("Nombre modificado");
        }

        if (!string.IsNullOrWhiteSpace(request.Apellido) && request.Apellido.Trim() != usuario.Apellido)
        {
            usuario.Apellido = request.Apellido.Trim();
            usuario.NombreCompleto = $"{usuario.Nombre} {usuario.Apellido}".Trim();
            cambios.Add("Apellido modificado");
        }

        if (request.Telefono != null && request.Telefono.Trim() != usuario.Telefono)
        {
            usuario.Telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim();
            cambios.Add("Teléfono modificado");
        }

        if (request.DepartamentoId.HasValue && request.DepartamentoId.Value != usuario.DepartamentoId)
        {
            var deptoExiste = await _context.Departamentos
                .AnyAsync(d => d.Id == request.DepartamentoId.Value, cancellationToken);

            if (!deptoExiste)
            {
                throw new NotFoundException($"El departamento con ID {request.DepartamentoId.Value} no existe.");
            }

            usuario.DepartamentoId = request.DepartamentoId.Value;
            cambios.Add($"Departamento asignado: {request.DepartamentoId.Value}");
        }

        // 4. Modificación de contraseña (si fue enviada por el administrador)
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            usuario.PasswordHash = _passwordHasher.HashPassword(request.Password);
            cambios.Add("Contraseña restablecida");
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 5. Registrar auditoría si hubo modificaciones
        if (cambios.Count > 0)
        {
            var auditoria = new Auditoria
            {
                UsuarioId = currentUserId,
                FechaHora = DateTime.UtcNow,
                Accion = "ModificarUsuario",
                Entidad = "Usuario",
                EntidadId = usuario.Id.ToString(),
                Motivo = $"Modificación de usuario {usuario.Email}",
                Detalle = string.Join("; ", cambios)
            };

            _context.Auditorias.Add(auditoria);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Recargar departamento si cambió
        if (usuario.DepartamentoId.HasValue && (usuario.Departamento == null || usuario.Departamento.Id != usuario.DepartamentoId))
        {
            usuario.Departamento = await _context.Departamentos
                .FirstOrDefaultAsync(d => d.Id == usuario.DepartamentoId.Value, cancellationToken);
        }

        return new UsuarioDetalleDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email,
            Telefono = usuario.Telefono,
            Activo = usuario.Activo,
            DepartamentoId = usuario.DepartamentoId,
            DepartamentoNombre = usuario.Departamento?.Nombre,
            Roles = usuario.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList(),
            FechaAlta = usuario.FechaCreacion
        };
    }
}
