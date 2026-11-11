using GestionReservas.Application.Common.Helpers;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Domain.Entities;
using GestionReservas.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionReservas.Application.Features.Espacios.Services;

/// <summary>
/// Implementación de la lógica de disponibilidad y cálculo de superposiciones para espacios físicos.
/// Evalúa superposiciones con bloqueos, reservas aprobadas y ocupación académica oficial (AsignacionEspacio).
/// </summary>
public class DisponibilidadService : IDisponibilidadService
{
    private readonly IApplicationDbContext _context;

    public DisponibilidadService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> EstaDisponibleAsync(
        int espacioId,
        DateTime desdeUtc,
        DateTime hastaUtc,
        int? excluirReservaId = null,
        CancellationToken cancellationToken = default)
    {
        // 1. El espacio debe existir, no estar dado de baja y estar en estado Disponible
        var espacioActivo = await _context.Espacios
            .AsNoTracking()
            .AnyAsync(e => e.Id == espacioId && e.FechaBaja == null && e.Estado == EstadoEspacio.Disponible, cancellationToken);

        if (!espacioActivo)
        {
            return false;
        }

        // 2. Verificar superposición con bloqueos: inicioA < finB AND finA > inicioB
        var tieneBloqueo = await _context.Bloqueos
            .AsNoTracking()
            .AnyAsync(b => b.EspacioId == espacioId &&
                           b.FechaHoraDesde < hastaUtc &&
                           b.FechaHoraHasta > desdeUtc,
                      cancellationToken);

        if (tieneBloqueo)
        {
            return false;
        }

        // 3. Verificar superposición con reservas aprobadas: inicioA < finB AND finA > inicioB
        var queryReservas = _context.Reservas
            .AsNoTracking()
            .Where(r => r.EspacioId == espacioId &&
                        r.Estado == EstadoReserva.Aprobada &&
                        r.FechaHoraInicio < hastaUtc &&
                        r.FechaHoraFin > desdeUtc);

        if (excluirReservaId.HasValue)
        {
            queryReservas = queryReservas.Where(r => r.Id != excluirReservaId.Value);
        }

        var tieneReservaAprobada = await queryReservas.AnyAsync(cancellationToken);
        if (tieneReservaAprobada)
        {
            return false;
        }

        // 4. Verificar superposición con asignación de cursada académica oficial
        var asignacionSuperpuesta = await ObtenerAsignacionSuperpuestaAsync(espacioId, desdeUtc, hastaUtc, cancellationToken);
        if (asignacionSuperpuesta != null)
        {
            return false;
        }

        return true;
    }

    public async Task<AsignacionEspacio?> ObtenerAsignacionSuperpuestaAsync(
        int espacioId,
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default)
    {
        var localDesde = DateTimeZoneHelper.ToFacultadLocal(desdeUtc);
        var localHasta = DateTimeZoneHelper.ToFacultadLocal(hastaUtc);

        var asignaciones = await _context.AsignacionesEspacio
            .AsNoTracking()
            .Include(a => a.Comision)
                .ThenInclude(c => c.Cursada)
                    .ThenInclude(cu => cu.Materia)
            .Include(a => a.Comision)
                .ThenInclude(c => c.Cursada)
                    .ThenInclude(cu => cu.CicloLectivo)
            .Where(a => a.EspacioId == espacioId &&
                        a.Comision.Cursada.CicloLectivo.Activo)
            .ToListAsync(cancellationToken);

        foreach (var a in asignaciones)
        {
            var ciclo = a.Comision.Cursada.CicloLectivo;
            for (var date = localDesde.Date; date <= localHasta.Date; date = date.AddDays(1))
            {
                if (date < ciclo.FechaInicio.Date || date > ciclo.FechaFin.Date)
                    continue;

                if (a.DiaSemana != date.DayOfWeek)
                    continue;

                var reqInicio = date == localDesde.Date ? localDesde.TimeOfDay : TimeSpan.Zero;
                var reqFin = date == localHasta.Date ? localHasta.TimeOfDay : TimeSpan.FromDays(1);

                if (reqFin <= reqInicio)
                    continue;

                if (a.HoraInicio < reqFin && a.HoraFin > reqInicio)
                {
                    return a;
                }
            }
        }

        return null;
    }

    public IQueryable<int> ObtenerEspaciosNoDisponiblesQuery(
        DateTime desdeUtc,
        DateTime hastaUtc,
        int? excluirReservaId = null)
    {
        // 1. Espacios con bloqueos superpuestos
        var bloqueosQuery = _context.Bloqueos
            .Where(b => b.FechaHoraDesde < hastaUtc && b.FechaHoraHasta > desdeUtc)
            .Select(b => b.EspacioId);

        // 2. Espacios con reservas aprobadas superpuestas
        var reservasQuery = _context.Reservas
            .Where(r => r.Estado == EstadoReserva.Aprobada &&
                        r.FechaHoraInicio < hastaUtc &&
                        r.FechaHoraFin > desdeUtc &&
                        (!excluirReservaId.HasValue || r.Id != excluirReservaId.Value))
            .Select(r => r.EspacioId);

        // 3. Espacios dados de baja o fuera de servicio
        var inhabilitadosQuery = _context.Espacios
            .Where(e => e.FechaBaja != null || e.Estado == EstadoEspacio.FueraDeServicio)
            .Select(e => e.Id);

        // 4. Espacios con asignaciones académicas oficiales recurrentes
        var localDesde = DateTimeZoneHelper.ToFacultadLocal(desdeUtc);
        var localHasta = DateTimeZoneHelper.ToFacultadLocal(hastaUtc);
        var localFechaUtc = DateTime.SpecifyKind(localDesde.Date, DateTimeKind.Utc);
        var diaSemana = localDesde.DayOfWeek;
        var horaInicio = localDesde.TimeOfDay;
        var horaFin = localHasta.TimeOfDay;

        var asignacionesQuery = _context.AsignacionesEspacio
            .Where(a => a.Comision.Cursada.CicloLectivo.Activo &&
                        a.Comision.Cursada.CicloLectivo.FechaInicio <= localFechaUtc &&
                        a.Comision.Cursada.CicloLectivo.FechaFin >= localFechaUtc &&
                        a.DiaSemana == diaSemana &&
                        a.HoraInicio < horaFin &&
                        a.HoraFin > horaInicio)
            .Select(a => a.EspacioId);

        return bloqueosQuery
            .Union(reservasQuery)
            .Union(inhabilitadosQuery)
            .Union(asignacionesQuery);
    }
}
