using FluentValidation;
using GestionReservas.Application.Common.Exceptions;
using GestionReservas.Application.Common.Helpers;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Features.Calendario.DTOs;
using GestionReservas.Application.Features.Espacios;
using GestionReservas.Domain.Entities;
using GestionReservas.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ValidationException = GestionReservas.Application.Common.Exceptions.ValidationException;

namespace GestionReservas.Application.Features.Calendario.Services;

/// <summary>
/// Implementación del servicio de calendario unificado de espacios (Reservas, Bloqueos y Cursadas).
/// </summary>
public class CalendarioService : ICalendarioService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEspacioAuthorizationService _espacioAuthService;
    private readonly IValidator<CalendarioQueryParameters> _validator;

    public CalendarioService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IEspacioAuthorizationService espacioAuthService,
        IValidator<CalendarioQueryParameters> validator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _espacioAuthService = espacioAuthService;
        _validator = validator;
    }

    public async Task<IReadOnlyList<EventoCalendarioDto>> GetEventosCalendarioAsync(
        CalendarioQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(parameters, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var currentUserId = _currentUserService.UsuarioId
            ?? throw new UnauthorizedException("Usuario no autenticado.");

        var desdeUtc = DateTimeZoneHelper.NormalizeToUtc(parameters.Desde!.Value);
        var hastaUtc = DateTimeZoneHelper.NormalizeToUtc(parameters.Hasta!.Value);

        // Si el usuario envía el mismo instante para desde y hasta (ej. consulta puntual de un día), cubrir las 24 hs siguientes
        if (hastaUtc == desdeUtc)
        {
            hastaUtc = desdeUtc.AddDays(1);
        }

        var espaciosFiltradosIds = ConstruirQueryEspaciosFiltrados(parameters).Select(e => e.Id);

        // 1. Consultar en SQL Reservas APROBADAS y PENDIENTES superpuestas con el rango (inicioA < finB AND finA > inicioB)
        var queryReservas = _context.Reservas
            .AsNoTracking()
            .Where(r => (r.Estado == EstadoReserva.Aprobada ||
                         r.Estado == EstadoReserva.PendienteAprobacion ||
                         r.Estado == EstadoReserva.Solicitada) &&
                        r.FechaHoraInicio < hastaUtc &&
                        r.FechaHoraFin > desdeUtc &&
                        espaciosFiltradosIds.Contains(r.EspacioId));

        if (parameters.EspacioId.HasValue)
        {
            queryReservas = queryReservas.Where(r => r.EspacioId == parameters.EspacioId.Value);
        }

        var reservasRaw = await queryReservas
            .Select(r => new
            {
                r.Id,
                r.EspacioId,
                EspacioCodigo = r.Espacio.Codigo,
                EspacioNombre = r.Espacio.Nombre,
                Edificio = r.Espacio.Edificio,
                Sector = r.Espacio.Sector,
                DepartamentoId = r.Espacio.DepartamentoId,
                DepartamentoNombre = r.Espacio.Departamento.Nombre,
                r.FechaHoraInicio,
                r.FechaHoraFin,
                r.Estado,
                r.SolicitanteId,
                SolicitanteNombre = r.Solicitante.NombreCompleto,
                r.TipoActividad,
                r.Motivo,
                r.Observaciones
            })
            .ToListAsync(cancellationToken);

        // 2. Consultar en SQL Bloqueos vigentes superpuestos con el rango
        var queryBloqueos = _context.Bloqueos
            .AsNoTracking()
            .Where(b => b.FechaHoraDesde < hastaUtc &&
                        b.FechaHoraHasta > desdeUtc &&
                        espaciosFiltradosIds.Contains(b.EspacioId));

        if (parameters.EspacioId.HasValue)
        {
            queryBloqueos = queryBloqueos.Where(b => b.EspacioId == parameters.EspacioId.Value);
        }

        var bloqueosRaw = await queryBloqueos
            .Select(b => new
            {
                b.Id,
                b.EspacioId,
                EspacioCodigo = b.Espacio.Codigo,
                EspacioNombre = b.Espacio.Nombre,
                Edificio = b.Espacio.Edificio,
                Sector = b.Espacio.Sector,
                DepartamentoId = b.Espacio.DepartamentoId,
                DepartamentoNombre = b.Espacio.Departamento.Nombre,
                b.FechaHoraDesde,
                b.FechaHoraHasta,
                b.Motivo
            })
            .ToListAsync(cancellationToken);

        // 3. Consultar en SQL AsignacionesEspacio (Cursadas oficiales) para los días de semana del rango y ciclo lectivo activo
        var localDesde = DateTimeZoneHelper.ToFacultadLocal(desdeUtc);
        var localHasta = DateTimeZoneHelper.ToFacultadLocal(hastaUtc);
        var minFechaCicloUtc = DateTime.SpecifyKind(localDesde.Date, DateTimeKind.Utc);
        var maxFechaCicloUtc = DateTime.SpecifyKind(localHasta.Date, DateTimeKind.Utc);

        var diasSemanaEnRango = new HashSet<DayOfWeek>();
        for (var d = localDesde.Date; d <= localHasta.Date; d = d.AddDays(1))
        {
            diasSemanaEnRango.Add(d.DayOfWeek);
        }

        var queryAsignaciones = _context.AsignacionesEspacio
            .AsNoTracking()
            .Where(a => a.Comision.Cursada.CicloLectivo.Activo &&
                        a.Comision.Cursada.CicloLectivo.FechaInicio <= maxFechaCicloUtc &&
                        a.Comision.Cursada.CicloLectivo.FechaFin >= minFechaCicloUtc &&
                        diasSemanaEnRango.Contains(a.DiaSemana) &&
                        espaciosFiltradosIds.Contains(a.EspacioId));

        if (parameters.EspacioId.HasValue)
        {
            queryAsignaciones = queryAsignaciones.Where(a => a.EspacioId == parameters.EspacioId.Value);
        }

        var asignacionesRaw = await queryAsignaciones
            .Select(a => new
            {
                a.Id,
                a.EspacioId,
                EspacioCodigo = a.Espacio.Codigo,
                EspacioNombre = a.Espacio.Nombre,
                Edificio = a.Espacio.Edificio,
                Sector = a.Espacio.Sector,
                DepartamentoId = a.Espacio.DepartamentoId,
                DepartamentoNombre = a.Espacio.Departamento.Nombre,
                a.DiaSemana,
                a.HoraInicio,
                a.HoraFin,
                MateriaNombre = a.Comision.Cursada.Materia.Nombre,
                ComisionCodigo = a.Comision.Codigo,
                CicloFechaInicio = a.Comision.Cursada.CicloLectivo.FechaInicio,
                CicloFechaFin = a.Comision.Cursada.CicloLectivo.FechaFin
            })
            .ToListAsync(cancellationToken);

        // 4. Evaluar permisos departamentales por cada departamento distinto involucrado reutilizando IEspacioAuthorizationService
        var departamentosInvolucrados = reservasRaw.Select(r => r.DepartamentoId)
            .Concat(bloqueosRaw.Select(b => b.DepartamentoId))
            .Distinct()
            .ToList();

        var permisosPorDepartamento = new Dictionary<int, bool>();
        foreach (var deptoId in departamentosInvolucrados)
        {
            permisosPorDepartamento[deptoId] = await _espacioAuthService.PuedeAdministrarDepartamentoAsync(deptoId, cancellationToken);
        }

        var eventos = new List<EventoCalendarioDto>(reservasRaw.Count + bloqueosRaw.Count + asignacionesRaw.Count * 5);

        // 5. Mapear Reservas aplicando reglas de privacidad
        foreach (var r in reservasRaw)
        {
            var puedeAdministrarDepto = permisosPorDepartamento.TryGetValue(r.DepartamentoId, out var p) && p;
            var verDetalleCompleto = puedeAdministrarDepto || r.SolicitanteId == currentUserId;

            eventos.Add(new EventoCalendarioDto
            {
                Tipo = "Reserva",
                ReferenciaId = r.Id,
                EspacioId = r.EspacioId,
                EspacioCodigo = r.EspacioCodigo,
                EspacioNombre = r.EspacioNombre,
                Edificio = r.Edificio,
                Sector = r.Sector,
                DepartamentoId = r.DepartamentoId,
                Departamento = r.DepartamentoNombre,
                Inicio = DateTime.SpecifyKind(r.FechaHoraInicio, DateTimeKind.Utc),
                Fin = DateTime.SpecifyKind(r.FechaHoraFin, DateTimeKind.Utc),
                Estado = r.Estado,
                Titulo = verDetalleCompleto ? $"{r.TipoActividad}: {r.Motivo}" : "Ocupado",
                SolicitanteId = verDetalleCompleto ? r.SolicitanteId : null,
                Solicitante = verDetalleCompleto ? r.SolicitanteNombre : null,
                TipoActividad = verDetalleCompleto ? r.TipoActividad : null,
                Motivo = verDetalleCompleto ? r.Motivo : null,
                Observaciones = verDetalleCompleto ? r.Observaciones : null
            });
        }

        // 6. Mapear Bloqueos aplicando reglas de privacidad sobre el motivo
        foreach (var b in bloqueosRaw)
        {
            var puedeVerMotivo = permisosPorDepartamento.TryGetValue(b.DepartamentoId, out var p) && p;

            eventos.Add(new EventoCalendarioDto
            {
                Tipo = "Bloqueo",
                ReferenciaId = b.Id,
                EspacioId = b.EspacioId,
                EspacioCodigo = b.EspacioCodigo,
                EspacioNombre = b.EspacioNombre,
                Edificio = b.Edificio,
                Sector = b.Sector,
                DepartamentoId = b.DepartamentoId,
                Departamento = b.DepartamentoNombre,
                Inicio = DateTime.SpecifyKind(b.FechaHoraDesde, DateTimeKind.Utc),
                Fin = DateTime.SpecifyKind(b.FechaHoraHasta, DateTimeKind.Utc),
                Estado = null,
                Titulo = puedeVerMotivo ? $"Bloqueo: {b.Motivo}" : "Bloqueado",
                Motivo = puedeVerMotivo ? b.Motivo : null
            });
        }

        // 7. Proyectar las Cursadas oficiales en cada fecha del rango según DiaSemana y vigencia del CicloLectivo
        for (var date = localDesde.Date; date <= localHasta.Date; date = date.AddDays(1))
        {
            var fechaLocal = DateOnly.FromDateTime(date);

            foreach (var a in asignacionesRaw)
            {
                if (a.DiaSemana != date.DayOfWeek)
                {
                    continue;
                }

                if (date < a.CicloFechaInicio.Date || date > a.CicloFechaFin.Date)
                {
                    continue;
                }

                var inicioCursoUtc = DateTimeZoneHelper.ToUtc(fechaLocal, TimeOnly.FromTimeSpan(a.HoraInicio));
                var finCursoUtc = DateTimeZoneHelper.ToUtc(fechaLocal, TimeOnly.FromTimeSpan(a.HoraFin));

                if (inicioCursoUtc < hastaUtc && finCursoUtc > desdeUtc)
                {
                    eventos.Add(new EventoCalendarioDto
                    {
                        Tipo = "Cursada",
                        ReferenciaId = a.Id,
                        EspacioId = a.EspacioId,
                        EspacioCodigo = a.EspacioCodigo,
                        EspacioNombre = a.EspacioNombre,
                        Edificio = a.Edificio,
                        Sector = a.Sector,
                        DepartamentoId = a.DepartamentoId,
                        Departamento = a.DepartamentoNombre,
                        Inicio = inicioCursoUtc,
                        Fin = finCursoUtc,
                        Estado = null,
                        Titulo = $"{a.MateriaNombre} - Comisión {a.ComisionCodigo}"
                    });
                }
            }
        }

        return eventos
            .OrderBy(e => e.Inicio)
            .ThenBy(e => e.Fin)
            .ThenBy(e => e.EspacioCodigo)
            .ThenBy(e => e.Tipo)
            .ToList();
    }

    private IQueryable<Espacio> ConstruirQueryEspaciosFiltrados(CalendarioQueryParameters parameters)
    {
        var query = _context.Espacios
            .AsNoTracking()
            .Where(e => e.FechaBaja == null);

        if (parameters.EspacioId.HasValue)
        {
            query = query.Where(e => e.Id == parameters.EspacioId.Value);
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

        if (parameters.TipoEspacioId.HasValue)
        {
            query = query.Where(e => e.TipoEspacioId == parameters.TipoEspacioId.Value);
        }

        if (parameters.DepartamentoId.HasValue)
        {
            query = query.Where(e => e.DepartamentoId == parameters.DepartamentoId.Value);
        }

        return query;
    }
}
