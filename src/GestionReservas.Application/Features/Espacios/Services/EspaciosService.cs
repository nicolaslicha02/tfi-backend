using FluentValidation;
using GestionReservas.Application.Common.Exceptions;
using GestionReservas.Application.Common.Helpers;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Espacios.DTOs;
using GestionReservas.Domain.Entities;
using GestionReservas.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ValidationException = GestionReservas.Application.Common.Exceptions.ValidationException;

namespace GestionReservas.Application.Features.Espacios.Services;

/// <summary>
/// Implementación del servicio de gestión de espacios físicos, autorización departamental, auditoría y disponibilidad.
/// </summary>
public class EspaciosService : IEspaciosService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEspacioAuthorizationService _authService;
    private readonly IDisponibilidadService _disponibilidadService;
    private readonly IValidator<CrearEspacioRequestDto> _crearValidator;
    private readonly IValidator<ActualizarEspacioRequestDto> _actualizarValidator;
    private readonly IValidator<EspaciosQueryParameters> _queryValidator;
    private readonly IValidator<DisponibilidadEspacioQueryParameters> _dispValidator;

    public EspaciosService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IEspacioAuthorizationService authService,
        IDisponibilidadService disponibilidadService,
        IValidator<CrearEspacioRequestDto> crearValidator,
        IValidator<ActualizarEspacioRequestDto> actualizarValidator,
        IValidator<EspaciosQueryParameters> queryValidator,
        IValidator<DisponibilidadEspacioQueryParameters> dispValidator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _authService = authService;
        _disponibilidadService = disponibilidadService;
        _crearValidator = crearValidator;
        _actualizarValidator = actualizarValidator;
        _queryValidator = queryValidator;
        _dispValidator = dispValidator;
    }

    public async Task<PagedResultDto<EspacioListadoDto>> GetEspaciosAsync(EspaciosQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var validationResult = await _queryValidator.ValidateAsync(parameters, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var query = _context.Espacios
            .Include(e => e.TipoEspacio)
            .Include(e => e.Departamento)
            .Include(e => e.Equipamientos)
                .ThenInclude(ee => ee.Equipamiento)
            .AsNoTracking();

        if (!parameters.IncluirBajas)
        {
            query = query.Where(e => e.FechaBaja == null);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var term = parameters.Search.Trim().ToLower();
            query = query.Where(e => e.Nombre.ToLower().Contains(term) || e.Codigo.ToLower().Contains(term));
        }

        if (parameters.TipoEspacioId.HasValue)
        {
            query = query.Where(e => e.TipoEspacioId == parameters.TipoEspacioId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Edificio))
        {
            var term = parameters.Edificio.Trim().ToLower();
            query = query.Where(e => e.Edificio.ToLower().Contains(term));
        }

        if (parameters.Piso.HasValue)
        {
            query = query.Where(e => e.Piso == parameters.Piso.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Sector))
        {
            var term = parameters.Sector.Trim().ToLower();
            query = query.Where(e => e.Sector.ToLower().Contains(term));
        }

        if (parameters.CapacidadMinima.HasValue)
        {
            query = query.Where(e => e.CapacidadMaxima >= parameters.CapacidadMinima.Value);
        }

        if (parameters.DepartamentoId.HasValue)
        {
            query = query.Where(e => e.DepartamentoId == parameters.DepartamentoId.Value);
        }

        if (parameters.Estado.HasValue)
        {
            query = query.Where(e => e.Estado == parameters.Estado.Value);
        }

        if (parameters.EquipamientoIds != null && parameters.EquipamientoIds.Count > 0)
        {
            var distinctEquips = parameters.EquipamientoIds.Distinct().ToList();
            var countRequired = distinctEquips.Count;
            query = query.Where(e => e.Equipamientos.Count(ee => distinctEquips.Contains(ee.EquipamientoId)) == countRequired);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.Edificio)
            .ThenBy(e => e.Piso)
            .ThenBy(e => e.Nombre)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(e => new EspacioListadoDto
            {
                Id = e.Id,
                Codigo = e.Codigo,
                Nombre = e.Nombre,
                TipoEspacioId = e.TipoEspacioId,
                TipoEspacioNombre = e.TipoEspacio.Nombre,
                Edificio = e.Edificio,
                Piso = e.Piso,
                Sector = e.Sector,
                CapacidadMaxima = e.CapacidadMaxima,
                DepartamentoId = e.DepartamentoId,
                DepartamentoNombre = e.Departamento.Nombre,
                Estado = e.Estado,
                EsBaja = e.FechaBaja != null,
                Equipamientos = e.Equipamientos.Select(ee => new EspacioEquipamientoDetalleDto
                {
                    EquipamientoId = ee.EquipamientoId,
                    Nombre = ee.Equipamiento.Nombre,
                    Cantidad = ee.Cantidad,
                    Especificacion = ee.ValorOpcional
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<EspacioListadoDto>(items, total, parameters.Page, parameters.PageSize);
    }

    public async Task<EspacioDetalleDto> GetEspacioByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var espacio = await _context.Espacios
            .Include(e => e.TipoEspacio)
            .Include(e => e.Departamento)
            .Include(e => e.Equipamientos)
                .ThenInclude(ee => ee.Equipamiento)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (espacio == null || espacio.FechaBaja != null)
        {
            throw new NotFoundException($"El espacio con ID {id} no fue encontrado o se encuentra dado de baja.");
        }

        return MapToDetalleDto(espacio);
    }

    public async Task<EspacioDetalleDto> CrearEspacioAsync(CrearEspacioRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _crearValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 1. Autorización departamental (Responsable solo puede crear en su depto)
        await _authService.ValidarPuedeAdministrarDepartamentoAsync(request.DepartamentoId, cancellationToken);

        // 2. Unicidad de código (incluso contra espacios dados de baja)
        var codigoNormalized = request.Codigo.Trim().ToUpperInvariant();
        var codigoExiste = await _context.Espacios
            .AnyAsync(e => e.Codigo.ToUpper() == codigoNormalized, cancellationToken);

        if (codigoExiste)
        {
            throw new ConflictException($"El código de espacio '{request.Codigo}' ya se encuentra registrado en el sistema.");
        }

        // 3. Existencia de TipoEspacio y Departamento
        var tipoEspacio = await _context.TiposEspacio
            .FirstOrDefaultAsync(t => t.Id == request.TipoEspacioId, cancellationToken);

        if (tipoEspacio == null)
        {
            throw new NotFoundException($"El tipo de espacio con ID {request.TipoEspacioId} no existe.");
        }

        var departamento = await _context.Departamentos
            .FirstOrDefaultAsync(d => d.Id == request.DepartamentoId, cancellationToken);

        if (departamento == null)
        {
            throw new NotFoundException($"El departamento con ID {request.DepartamentoId} no existe.");
        }

        // 4. Existencia de equipamientos
        var equipamientosList = new List<EspacioEquipamiento>();
        if (request.Equipamientos.Count > 0)
        {
            var requestedEquipIds = request.Equipamientos.Select(e => e.EquipamientoId).Distinct().ToList();
            var existingEquip = await _context.Equipamientos
                .Where(e => requestedEquipIds.Contains(e.Id))
                .ToListAsync(cancellationToken);

            if (existingEquip.Count != requestedEquipIds.Count)
            {
                var missingIds = requestedEquipIds.Except(existingEquip.Select(e => e.Id));
                throw new NotFoundException($"Los siguientes equipamientos no existen: {string.Join(", ", missingIds)}.");
            }

            foreach (var item in request.Equipamientos)
            {
                var equip = existingEquip.First(e => e.Id == item.EquipamientoId);
                equipamientosList.Add(new EspacioEquipamiento
                {
                    EquipamientoId = item.EquipamientoId,
                    Equipamiento = equip,
                    Cantidad = item.Cantidad,
                    ValorOpcional = string.IsNullOrWhiteSpace(item.Especificacion) ? null : item.Especificacion.Trim()
                });
            }
        }

        // 5. Instanciar entidad y guardar
        var espacio = new Espacio
        {
            Codigo = codigoNormalized,
            Nombre = request.Nombre.Trim(),
            TipoEspacioId = request.TipoEspacioId,
            TipoEspacio = tipoEspacio,
            Edificio = request.Edificio.Trim(),
            Piso = request.Piso,
            Sector = request.Sector.Trim(),
            CapacidadMaxima = request.CapacidadMaxima,
            DepartamentoId = request.DepartamentoId,
            Departamento = departamento,
            Estado = request.Estado,
            Equipamientos = equipamientosList
        };

        _context.Espacios.Add(espacio);
        await _context.SaveChangesAsync(cancellationToken);

        // 6. Auditoría
        var currentUserId = _currentUserService.UsuarioId;
        var auditoria = new Auditoria
        {
            UsuarioId = currentUserId,
            FechaHora = DateTime.UtcNow,
            Accion = "CrearEspacio",
            Entidad = "Espacio",
            EntidadId = espacio.Id.ToString(),
            Motivo = $"Alta del espacio {espacio.Codigo}",
            Detalle = $"Nombre: {espacio.Nombre}; Capacidad: {espacio.CapacidadMaxima}; Depto: {espacio.Departamento.Nombre}; Equipamientos: {equipamientosList.Count}"
        };

        _context.Auditorias.Add(auditoria);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDetalleDto(espacio);
    }

    public async Task<EspacioDetalleDto> ActualizarEspacioAsync(int id, ActualizarEspacioRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _actualizarValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var espacio = await _context.Espacios
            .Include(e => e.Equipamientos)
                .ThenInclude(ee => ee.Equipamiento)
            .Include(e => e.Departamento)
            .Include(e => e.TipoEspacio)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (espacio == null || espacio.FechaBaja != null)
        {
            throw new NotFoundException($"El espacio con ID {id} no fue encontrado o se encuentra dado de baja.");
        }

        // 1. Autorización: debe poder administrar el depto actual y el depto destino si cambió
        await _authService.ValidarPuedeAdministrarEspacioAsync(espacio, cancellationToken);
        if (request.DepartamentoId != espacio.DepartamentoId)
        {
            await _authService.ValidarPuedeAdministrarDepartamentoAsync(request.DepartamentoId, cancellationToken);
        }

        // 2. Unicidad de código (excluyendo a sí mismo)
        var codigoNormalized = request.Codigo.Trim().ToUpperInvariant();
        var codigoExiste = await _context.Espacios
            .AnyAsync(e => e.Codigo.ToUpper() == codigoNormalized && e.Id != id, cancellationToken);

        if (codigoExiste)
        {
            throw new ConflictException($"El código de espacio '{request.Codigo}' ya se encuentra registrado por otro espacio.");
        }

        // 3. Validar TipoEspacio y Departamento
        var tipoEspacio = await _context.TiposEspacio
            .FirstOrDefaultAsync(t => t.Id == request.TipoEspacioId, cancellationToken);

        if (tipoEspacio == null)
        {
            throw new NotFoundException($"El tipo de espacio con ID {request.TipoEspacioId} no existe.");
        }

        var departamento = await _context.Departamentos
            .FirstOrDefaultAsync(d => d.Id == request.DepartamentoId, cancellationToken);

        if (departamento == null)
        {
            throw new NotFoundException($"El departamento con ID {request.DepartamentoId} no existe.");
        }

        // 4. Validar y reemplazar equipamientos
        _context.EspaciosEquipamiento.RemoveRange(espacio.Equipamientos);
        espacio.Equipamientos.Clear();

        if (request.Equipamientos.Count > 0)
        {
            var requestedEquipIds = request.Equipamientos.Select(e => e.EquipamientoId).Distinct().ToList();
            var existingEquip = await _context.Equipamientos
                .Where(e => requestedEquipIds.Contains(e.Id))
                .ToListAsync(cancellationToken);

            if (existingEquip.Count != requestedEquipIds.Count)
            {
                var missingIds = requestedEquipIds.Except(existingEquip.Select(e => e.Id));
                throw new NotFoundException($"Los siguientes equipamientos no existen: {string.Join(", ", missingIds)}.");
            }

            foreach (var item in request.Equipamientos)
            {
                var equip = existingEquip.First(e => e.Id == item.EquipamientoId);
                espacio.Equipamientos.Add(new EspacioEquipamiento
                {
                    EspacioId = espacio.Id,
                    EquipamientoId = item.EquipamientoId,
                    Equipamiento = equip,
                    Cantidad = item.Cantidad,
                    ValorOpcional = string.IsNullOrWhiteSpace(item.Especificacion) ? null : item.Especificacion.Trim()
                });
            }
        }

        // 5. Aplicar cambios en campos principales
        espacio.Codigo = codigoNormalized;
        espacio.Nombre = request.Nombre.Trim();
        espacio.TipoEspacioId = request.TipoEspacioId;
        espacio.TipoEspacio = tipoEspacio;
        espacio.Edificio = request.Edificio.Trim();
        espacio.Piso = request.Piso;
        espacio.Sector = request.Sector.Trim();
        espacio.CapacidadMaxima = request.CapacidadMaxima;
        espacio.DepartamentoId = request.DepartamentoId;
        espacio.Departamento = departamento;
        espacio.Estado = request.Estado;

        await _context.SaveChangesAsync(cancellationToken);

        // 6. Auditoría
        var currentUserId = _currentUserService.UsuarioId;
        var auditoria = new Auditoria
        {
            UsuarioId = currentUserId,
            FechaHora = DateTime.UtcNow,
            Accion = "ModificarEspacio",
            Entidad = "Espacio",
            EntidadId = espacio.Id.ToString(),
            Motivo = $"Modificación integral del espacio {espacio.Codigo}",
            Detalle = $"Nombre: {espacio.Nombre}; Capacidad: {espacio.CapacidadMaxima}; Depto: {departamento.Nombre}; Estado: {espacio.Estado}; Equipamientos: {espacio.Equipamientos.Count}"
        };

        _context.Auditorias.Add(auditoria);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDetalleDto(espacio);
    }

    public async Task EliminarEspacioAsync(int id, CancellationToken cancellationToken = default)
    {
        var espacio = await _context.Espacios
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (espacio == null || espacio.FechaBaja != null)
        {
            throw new NotFoundException($"El espacio con ID {id} no fue encontrado o ya se encuentra dado de baja.");
        }

        // 1. Autorización sobre el espacio
        await _authService.ValidarPuedeAdministrarEspacioAsync(espacio, cancellationToken);

        // 2. Regla: no permitir baja si tiene reservas futuras APROBADAS o PENDIENTES
        var nowUtc = DateTime.UtcNow;
        var reservasFuturas = await _context.Reservas
            .Where(r => r.EspacioId == id
                     && r.FechaHoraFin > nowUtc
                     && (r.Estado == EstadoReserva.Aprobada ||
                         r.Estado == EstadoReserva.PendienteAprobacion ||
                         r.Estado == EstadoReserva.Solicitada))
            .CountAsync(cancellationToken);

        if (reservasFuturas > 0)
        {
            throw new ConflictException(
                $"No es posible dar de baja el espacio porque cuenta con {reservasFuturas} reserva(s) futuras pendientes o aprobadas que deben ser resueltas o canceladas previamente.");
        }

        // 3. Baja lógica
        espacio.FechaBaja = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Auditoría
        var currentUserId = _currentUserService.UsuarioId;
        var auditoria = new Auditoria
        {
            UsuarioId = currentUserId,
            FechaHora = DateTime.UtcNow,
            Accion = "EliminarEspacio",
            Entidad = "Espacio",
            EntidadId = espacio.Id.ToString(),
            Motivo = $"Baja lógica del espacio {espacio.Codigo}",
            Detalle = $"Fecha de baja UTC: {espacio.FechaBaja}"
        };

        _context.Auditorias.Add(auditoria);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EspacioListadoDto>> GetEspaciosDisponiblesAsync(DisponibilidadEspacioQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var validationResult = await _dispValidator.ValidateAsync(parameters, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 1. Conversión de fecha y hora local de facultad a UTC
        var desdeUtc = DateTimeZoneHelper.ToUtc(parameters.Fecha!.Value, parameters.HoraInicio!.Value);
        var hastaUtc = DateTimeZoneHelper.ToUtc(parameters.Fecha!.Value, parameters.HoraFin!.Value);

        // 2. Subconsulta en BD de espacios no disponibles
        var noDisponiblesQuery = _disponibilidadService.ObtenerEspaciosNoDisponiblesQuery(desdeUtc, hastaUtc);

        // 3. Consulta de espacios disponibles con filtros
        var query = _context.Espacios
            .Include(e => e.TipoEspacio)
            .Include(e => e.Departamento)
            .Include(e => e.Equipamientos)
                .ThenInclude(ee => ee.Equipamiento)
            .Where(e => e.FechaBaja == null && e.Estado == EstadoEspacio.Disponible)
            .Where(e => !noDisponiblesQuery.Contains(e.Id))
            .AsNoTracking();

        if (parameters.CapacidadMinima.HasValue)
        {
            query = query.Where(e => e.CapacidadMaxima >= parameters.CapacidadMinima.Value);
        }

        if (parameters.TipoEspacioId.HasValue)
        {
            query = query.Where(e => e.TipoEspacioId == parameters.TipoEspacioId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Edificio))
        {
            var term = parameters.Edificio.Trim().ToLower();
            query = query.Where(e => e.Edificio.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Sector))
        {
            var term = parameters.Sector.Trim().ToLower();
            query = query.Where(e => e.Sector.ToLower().Contains(term));
        }

        if (parameters.DepartamentoId.HasValue)
        {
            query = query.Where(e => e.DepartamentoId == parameters.DepartamentoId.Value);
        }

        if (parameters.EquipamientoIds != null && parameters.EquipamientoIds.Count > 0)
        {
            var distinctEquips = parameters.EquipamientoIds.Distinct().ToList();
            var countRequired = distinctEquips.Count;
            query = query.Where(e => e.Equipamientos.Count(ee => distinctEquips.Contains(ee.EquipamientoId)) == countRequired);
        }

        var items = await query
            .OrderBy(e => e.Edificio)
            .ThenBy(e => e.Piso)
            .ThenBy(e => e.Nombre)
            .Select(e => new EspacioListadoDto
            {
                Id = e.Id,
                Codigo = e.Codigo,
                Nombre = e.Nombre,
                TipoEspacioId = e.TipoEspacioId,
                TipoEspacioNombre = e.TipoEspacio.Nombre,
                Edificio = e.Edificio,
                Piso = e.Piso,
                Sector = e.Sector,
                CapacidadMaxima = e.CapacidadMaxima,
                DepartamentoId = e.DepartamentoId,
                DepartamentoNombre = e.Departamento.Nombre,
                Estado = e.Estado,
                EsBaja = false,
                Equipamientos = e.Equipamientos.Select(ee => new EspacioEquipamientoDetalleDto
                {
                    EquipamientoId = ee.EquipamientoId,
                    Nombre = ee.Equipamiento.Nombre,
                    Cantidad = ee.Cantidad,
                    Especificacion = ee.ValorOpcional
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return items;
    }

    private static EspacioDetalleDto MapToDetalleDto(Espacio espacio)
    {
        return new EspacioDetalleDto
        {
            Id = espacio.Id,
            Codigo = espacio.Codigo,
            Nombre = espacio.Nombre,
            TipoEspacioId = espacio.TipoEspacioId,
            TipoEspacioNombre = espacio.TipoEspacio?.Nombre ?? string.Empty,
            Edificio = espacio.Edificio,
            Piso = espacio.Piso,
            Sector = espacio.Sector,
            CapacidadMaxima = espacio.CapacidadMaxima,
            DepartamentoId = espacio.DepartamentoId,
            DepartamentoNombre = espacio.Departamento?.Nombre ?? string.Empty,
            Estado = espacio.Estado,
            FechaBaja = espacio.FechaBaja,
            Equipamientos = espacio.Equipamientos.Select(ee => new EspacioEquipamientoDetalleDto
            {
                EquipamientoId = ee.EquipamientoId,
                Nombre = ee.Equipamiento?.Nombre ?? string.Empty,
                Cantidad = ee.Cantidad,
                Especificacion = ee.ValorOpcional
            }).ToList()
        };
    }
}
