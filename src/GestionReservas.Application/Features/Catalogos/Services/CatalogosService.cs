using FluentValidation;
using GestionReservas.Application.Common.Exceptions;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Features.Catalogos.DTOs;
using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ValidationException = GestionReservas.Application.Common.Exceptions.ValidationException;

namespace GestionReservas.Application.Features.Catalogos.Services;

/// <summary>
/// Implementación del servicio de consulta y administración de catálogos maestros y roles.
/// </summary>
public class CatalogosService : ICatalogosService
{
    private static readonly HashSet<string> RolesInstitucionales = new(StringComparer.OrdinalIgnoreCase)
    {
        "Solicitante", "ResponsableEspacio", "SecretariaAcademica", "Administrador"
    };

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CrearEquipamientoRequestDto> _equipamientoValidator;
    private readonly IValidator<CrearTipoEspacioRequestDto> _tipoEspacioValidator;

    public CatalogosService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IValidator<CrearEquipamientoRequestDto> equipamientoValidator,
        IValidator<CrearTipoEspacioRequestDto> tipoEspacioValidator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _equipamientoValidator = equipamientoValidator;
        _tipoEspacioValidator = tipoEspacioValidator;
    }

    public async Task<IReadOnlyList<DepartamentoDto>> GetDepartamentosAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Departamentos
            .AsNoTracking()
            .OrderBy(d => d.Nombre)
            .Select(d => new DepartamentoDto
            {
                Id = d.Id,
                Nombre = d.Nombre,
                Descripcion = d.Descripcion,
                CantidadEspacios = d.Espacios.Count(e => e.FechaBaja == null)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartamentoDto> CrearDepartamentoAsync(GuardarDepartamentoRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidarGestionDepartamentos(request);
        var nombre = request.Nombre.Trim();
        if (await _context.Departamentos.AnyAsync(d => d.Nombre.ToLower() == nombre.ToLower(), cancellationToken))
            throw new ConflictException($"Ya existe el departamento '{nombre}'.");

        var department = new Departamento { Nombre = nombre, Descripcion = NormalizarDescripcion(request.Descripcion) };
        _context.Departamentos.Add(department);
        await _context.SaveChangesAsync(cancellationToken);
        await RegistrarAuditoriaDepartamentoAsync("CrearDepartamento", department, cancellationToken);
        return MapearDepartamento(department);
    }

    public async Task<DepartamentoDto> ActualizarDepartamentoAsync(int id, GuardarDepartamentoRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidarGestionDepartamentos(request);
        var department = await _context.Departamentos.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new NotFoundException("Departamento", id);
        var nombre = request.Nombre.Trim();
        if (await _context.Departamentos.AnyAsync(d => d.Id != id && d.Nombre.ToLower() == nombre.ToLower(), cancellationToken))
            throw new ConflictException($"Ya existe el departamento '{nombre}'.");

        department.Nombre = nombre;
        department.Descripcion = NormalizarDescripcion(request.Descripcion);
        await _context.SaveChangesAsync(cancellationToken);
        await RegistrarAuditoriaDepartamentoAsync("ActualizarDepartamento", department, cancellationToken);
        return MapearDepartamento(department);
    }

    public async Task EliminarDepartamentoAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidarGestionDepartamentos();
        var department = await _context.Departamentos.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new NotFoundException("Departamento", id);
        var referenciado = await _context.Espacios.AnyAsync(e => e.DepartamentoId == id, cancellationToken) ||
            await _context.Usuarios.AnyAsync(u => u.DepartamentoId == id, cancellationToken) ||
            await _context.Materias.AnyAsync(m => m.DepartamentoId == id, cancellationToken);
        if (referenciado)
            throw new ConflictException("No se puede eliminar un departamento con espacios, usuarios o materias asociados.");

        _context.Departamentos.Remove(department);
        await _context.SaveChangesAsync(cancellationToken);
        await RegistrarAuditoriaDepartamentoAsync("EliminarDepartamento", department, cancellationToken);
    }

    private void ValidarGestionDepartamentos(GuardarDepartamentoRequestDto? request = null)
    {
        if (!_currentUserService.IsInRole("Administrador") && !_currentUserService.IsInRole("SecretariaAcademica"))
            throw new GestionReservas.Application.Common.Exceptions.ForbiddenException("Solo Secretaría Académica o Administración puede gestionar departamentos.");
        if (request is not null && (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > 150 || request.Descripcion?.Length > 300))
            throw new ValidationException("Nombre", "El nombre es obligatorio (máximo 150 caracteres) y la descripción no puede superar 300 caracteres.");
    }

    private static DepartamentoDto MapearDepartamento(Departamento department) => new()
    {
        Id = department.Id,
        Nombre = department.Nombre,
        Descripcion = department.Descripcion,
        CantidadEspacios = department.Espacios.Count(e => e.FechaBaja is null)
    };

    private async Task RegistrarAuditoriaDepartamentoAsync(string action, Departamento department, CancellationToken cancellationToken)
    {
        _context.Auditorias.Add(new Auditoria
        {
            UsuarioId = _currentUserService.UsuarioId,
            UsuarioNombre = _currentUserService.NombreCompleto,
            UsuarioEmail = _currentUserService.Email,
            Rol = string.Join(", ", _currentUserService.Roles),
            FechaHora = DateTime.UtcNow,
            Accion = action,
            Entidad = "Departamento",
            EntidadId = department.Id.ToString(),
            Motivo = "Gestión del catálogo de departamentos",
            Detalle = $"{action}: {department.Nombre}."
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EquipamientoDto>> GetEquipamientosAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Equipamientos
            .AsNoTracking()
            .OrderBy(e => e.Nombre)
            .Select(e => new EquipamientoDto
            {
                Id = e.Id,
                Nombre = e.Nombre,
                Descripcion = e.Descripcion
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TipoEspacioDto>> GetTiposEspacioAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TiposEspacio
            .AsNoTracking()
            .OrderBy(t => t.Nombre)
            .Select(t => new TipoEspacioDto
            {
                Id = t.Id,
                Nombre = t.Nombre,
                Descripcion = t.Descripcion
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RolDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .Select(r => new RolDto
            {
                Id = r.Id,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<RolDto> CrearRolAsync(GuardarRolRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidarGestionRoles(request);
        var nombre = request.Nombre.Trim();
        if (RolesInstitucionales.Contains(nombre))
            throw new ConflictException("El nombre está reservado para un rol institucional.");
        if (await _context.Roles.AnyAsync(r => r.Nombre.ToLower() == nombre.ToLower(), cancellationToken))
            throw new ConflictException($"Ya existe el rol '{nombre}'.");

        var rol = new Rol { Nombre = nombre, Descripcion = NormalizarDescripcion(request.Descripcion) };
        _context.Roles.Add(rol);
        await _context.SaveChangesAsync(cancellationToken);
        await RegistrarAuditoriaRolAsync("CrearRol", rol, cancellationToken);
        return MapearRol(rol);
    }

    public async Task<RolDto> ActualizarRolAsync(int id, GuardarRolRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidarGestionRoles(request);
        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("Rol", id);
        var nombre = request.Nombre.Trim();
        if (RolesInstitucionales.Contains(rol.Nombre) && !string.Equals(nombre, rol.Nombre, StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Los roles institucionales no se pueden renombrar.");
        if (RolesInstitucionales.Contains(nombre) && !string.Equals(nombre, rol.Nombre, StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("El nombre está reservado para un rol institucional.");
        if (await _context.Roles.AnyAsync(r => r.Id != id && r.Nombre.ToLower() == nombre.ToLower(), cancellationToken))
            throw new ConflictException($"Ya existe el rol '{nombre}'.");

        rol.Nombre = nombre;
        rol.Descripcion = NormalizarDescripcion(request.Descripcion);
        await _context.SaveChangesAsync(cancellationToken);
        await RegistrarAuditoriaRolAsync("ActualizarRol", rol, cancellationToken);
        return MapearRol(rol);
    }

    public async Task EliminarRolAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidarGestionRoles();
        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("Rol", id);
        if (RolesInstitucionales.Contains(rol.Nombre))
            throw new ConflictException("Los roles institucionales no se pueden eliminar.");
        if (await _context.UsuarioRoles.AnyAsync(x => x.RolId == id, cancellationToken))
            throw new ConflictException("No se puede eliminar un rol que está asignado a usuarios.");
        if (await _context.RolPermisos.AnyAsync(x => x.RolId == id, cancellationToken))
            throw new ConflictException("No se puede eliminar un rol que tiene permisos asociados.");

        _context.Roles.Remove(rol);
        await _context.SaveChangesAsync(cancellationToken);
        await RegistrarAuditoriaRolAsync("EliminarRol", rol, cancellationToken);
    }

    private void ValidarGestionRoles(GuardarRolRequestDto? request = null)
    {
        if (!_currentUserService.IsInRole("Administrador") && !_currentUserService.IsInRole("SecretariaAcademica"))
            throw new GestionReservas.Application.Common.Exceptions.ForbiddenException("Solo Secretaría Académica o Administración puede gestionar roles.");
        if (request is not null && (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > 50 || request.Descripcion?.Length > 250))
            throw new ValidationException("Nombre", "El nombre es obligatorio (máximo 50 caracteres) y la descripción no puede superar 250 caracteres.");
    }

    private static RolDto MapearRol(Rol rol) => new() { Id = rol.Id, Nombre = rol.Nombre, Descripcion = rol.Descripcion };

    private static string? NormalizarDescripcion(string? descripcion) =>
        string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();

    private async Task RegistrarAuditoriaRolAsync(string accion, Rol rol, CancellationToken cancellationToken)
    {
        _context.Auditorias.Add(new Auditoria
        {
            UsuarioId = _currentUserService.UsuarioId,
            UsuarioNombre = _currentUserService.NombreCompleto,
            UsuarioEmail = _currentUserService.Email,
            Rol = string.Join(", ", _currentUserService.Roles),
            FechaHora = DateTime.UtcNow,
            Accion = accion,
            Entidad = "Rol",
            EntidadId = rol.Id.ToString(),
            Motivo = "Gestión del catálogo de roles",
            Detalle = $"{accion}: {rol.Nombre}."
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<EquipamientoDto> CrearEquipamientoAsync(CrearEquipamientoRequestDto request, CancellationToken cancellationToken = default)
    {
        var validation = await _equipamientoValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var nombreLimpio = request.Nombre.Trim();
        var nombreLower = nombreLimpio.ToLower();
        var existe = await _context.Equipamientos
            .AsNoTracking()
            .AnyAsync(e => e.Nombre.ToLower() == nombreLower, cancellationToken);

        if (existe)
        {
            throw new ConflictException($"Ya existe un equipamiento registrado con el nombre '{nombreLimpio}'.");
        }

        var equipamiento = new Equipamiento
        {
            Nombre = nombreLimpio,
            Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim()
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _context.Equipamientos.Add(equipamiento);
        await _context.SaveChangesAsync(cancellationToken);

        var usuarioId = _currentUserService.UsuarioId;
        _context.Auditorias.Add(new Auditoria
        {
            UsuarioId = usuarioId,
            Rol = string.Join(", ", _currentUserService.Roles),
            UsuarioNombre = _currentUserService.NombreCompleto,
            UsuarioEmail = _currentUserService.Email,
            FechaHora = DateTime.UtcNow,
            Accion = "CrearEquipamiento",
            Entidad = "Equipamiento",
            EntidadId = equipamiento.Id.ToString(),
            Motivo = "Alta en catálogo de equipamiento",
            Detalle = $"Equipamiento '{equipamiento.Nombre}' registrado en el catálogo."
        });
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new EquipamientoDto
        {
            Id = equipamiento.Id,
            Nombre = equipamiento.Nombre,
            Descripcion = equipamiento.Descripcion
        };
    }

    public async Task<TipoEspacioDto> CrearTipoEspacioAsync(CrearTipoEspacioRequestDto request, CancellationToken cancellationToken = default)
    {
        var validation = await _tipoEspacioValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var nombreLimpio = request.Nombre.Trim();
        var nombreLower = nombreLimpio.ToLower();
        var existe = await _context.TiposEspacio
            .AsNoTracking()
            .AnyAsync(t => t.Nombre.ToLower() == nombreLower, cancellationToken);

        if (existe)
        {
            throw new ConflictException($"Ya existe un tipo de espacio registrado con el nombre '{nombreLimpio}'.");
        }

        var tipoEspacio = new TipoEspacio
        {
            Nombre = nombreLimpio,
            Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim()
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _context.TiposEspacio.Add(tipoEspacio);
        await _context.SaveChangesAsync(cancellationToken);

        var usuarioId = _currentUserService.UsuarioId;
        _context.Auditorias.Add(new Auditoria
        {
            UsuarioId = usuarioId,
            Rol = string.Join(", ", _currentUserService.Roles),
            UsuarioNombre = _currentUserService.NombreCompleto,
            UsuarioEmail = _currentUserService.Email,
            FechaHora = DateTime.UtcNow,
            Accion = "CrearTipoEspacio",
            Entidad = "TipoEspacio",
            EntidadId = tipoEspacio.Id.ToString(),
            Motivo = "Alta en catálogo de tipos de espacio",
            Detalle = $"Tipo de espacio '{tipoEspacio.Nombre}' registrado en el catálogo."
        });
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new TipoEspacioDto
        {
            Id = tipoEspacio.Id,
            Nombre = tipoEspacio.Nombre,
            Descripcion = tipoEspacio.Descripcion
        };
    }
}
