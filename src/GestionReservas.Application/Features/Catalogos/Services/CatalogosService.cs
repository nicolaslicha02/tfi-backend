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
                Descripcion = d.Descripcion
            })
            .ToListAsync(cancellationToken);
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

        _context.Equipamientos.Add(equipamiento);
        await _context.SaveChangesAsync(cancellationToken);

        var usuarioId = _currentUserService.UsuarioId;
        _context.Auditorias.Add(new Auditoria
        {
            UsuarioId = usuarioId,
            FechaHora = DateTime.UtcNow,
            Accion = "CrearEquipamiento",
            Entidad = "Equipamiento",
            EntidadId = equipamiento.Id.ToString(),
            Motivo = "Alta en catálogo de equipamiento",
            Detalle = $"Equipamiento '{equipamiento.Nombre}' registrado en el catálogo."
        });
        await _context.SaveChangesAsync(cancellationToken);

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

        _context.TiposEspacio.Add(tipoEspacio);
        await _context.SaveChangesAsync(cancellationToken);

        var usuarioId = _currentUserService.UsuarioId;
        _context.Auditorias.Add(new Auditoria
        {
            UsuarioId = usuarioId,
            FechaHora = DateTime.UtcNow,
            Accion = "CrearTipoEspacio",
            Entidad = "TipoEspacio",
            EntidadId = tipoEspacio.Id.ToString(),
            Motivo = "Alta en catálogo de tipos de espacio",
            Detalle = $"Tipo de espacio '{tipoEspacio.Nombre}' registrado en el catálogo."
        });
        await _context.SaveChangesAsync(cancellationToken);

        return new TipoEspacioDto
        {
            Id = tipoEspacio.Id,
            Nombre = tipoEspacio.Nombre,
            Descripcion = tipoEspacio.Descripcion
        };
    }
}
