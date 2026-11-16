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
        var tramos = ConstruirTramosLocales(desdeUtc, hastaUtc);
        if (tramos.Count == 0)
        {
            return null;
        }

        var baseQuery = _context.AsignacionesEspacio
            .AsNoTracking()
            .Include(a => a.Comision)
                .ThenInclude(c => c.Cursada)
                    .ThenInclude(cu => cu.Materia)
            .Include(a => a.Comision)
                .ThenInclude(c => c.Cursada)
                    .ThenInclude(cu => cu.CicloLectivo);

        if (tramos.Count == 1)
        {
            var t = tramos[0];
            return await baseQuery
                .Where(a => a.EspacioId == espacioId &&
                            a.Comision.Cursada.CicloLectivo.Activo &&
                            a.Comision.Cursada.CicloLectivo.FechaInicio <= t.FechaCicloUtc &&
                            a.Comision.Cursada.CicloLectivo.FechaFin >= t.FechaCicloUtc &&
                            a.DiaSemana == t.DiaSemana &&
                            a.HoraInicio < t.HoraFin &&
                            a.HoraFin > t.HoraInicio)
                .FirstOrDefaultAsync(cancellationToken);
        }

        IQueryable<int>? idsSuperpuestosQuery = null;
        foreach (var t in tramos)
        {
            var fechaCicloUtc = t.FechaCicloUtc;
            var diaSemana = t.DiaSemana;
            var horaInicio = t.HoraInicio;
            var horaFin = t.HoraFin;

            var tramoIds = _context.AsignacionesEspacio
                .Where(a => a.EspacioId == espacioId &&
                            a.Comision.Cursada.CicloLectivo.Activo &&
                            a.Comision.Cursada.CicloLectivo.FechaInicio <= fechaCicloUtc &&
                            a.Comision.Cursada.CicloLectivo.FechaFin >= fechaCicloUtc &&
                            a.DiaSemana == diaSemana &&
                            a.HoraInicio < horaFin &&
                            a.HoraFin > horaInicio)
                .Select(a => a.Id);

            idsSuperpuestosQuery = idsSuperpuestosQuery == null
                ? tramoIds
                : idsSuperpuestosQuery.Union(tramoIds);
        }

        return await baseQuery
            .Where(a => idsSuperpuestosQuery!.Contains(a.Id))
            .FirstOrDefaultAsync(cancellationToken);
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

        var resultadoQuery = bloqueosQuery
            .Union(reservasQuery)
            .Union(inhabilitadosQuery);

        // 4. Espacios con asignaciones académicas oficiales recurrentes en cualquiera de los días del rango
        var tramos = ConstruirTramosLocales(desdeUtc, hastaUtc);
        foreach (var t in tramos)
        {
            var fechaCicloUtc = t.FechaCicloUtc;
            var diaSemana = t.DiaSemana;
            var horaInicio = t.HoraInicio;
            var horaFin = t.HoraFin;

            var asignacionesTramoQuery = _context.AsignacionesEspacio
                .Where(a => a.Comision.Cursada.CicloLectivo.Activo &&
                            a.Comision.Cursada.CicloLectivo.FechaInicio <= fechaCicloUtc &&
                            a.Comision.Cursada.CicloLectivo.FechaFin >= fechaCicloUtc &&
                            a.DiaSemana == diaSemana &&
                            a.HoraInicio < horaFin &&
                            a.HoraFin > horaInicio)
                .Select(a => a.EspacioId);

            resultadoQuery = resultadoQuery.Union(asignacionesTramoQuery);
        }

        return resultadoQuery;
    }

    private static List<(DateTime FechaCicloUtc, DayOfWeek DiaSemana, TimeSpan HoraInicio, TimeSpan HoraFin)> ConstruirTramosLocales(
        DateTime desdeUtc,
        DateTime hastaUtc)
    {
        var localDesde = DateTimeZoneHelper.ToFacultadLocal(desdeUtc);
        var localHasta = DateTimeZoneHelper.ToFacultadLocal(hastaUtc);

        var tramos = new List<(DateTime FechaCicloUtc, DayOfWeek DiaSemana, TimeSpan HoraInicio, TimeSpan HoraFin)>();
        var diasCompletosVistos = new HashSet<DayOfWeek>();

        for (var date = localDesde.Date; date <= localHasta.Date; date = date.AddDays(1))
        {
            var reqInicio = date == localDesde.Date ? localDesde.TimeOfDay : TimeSpan.Zero;
            var reqFin = date == localHasta.Date ? localHasta.TimeOfDay : TimeSpan.FromDays(1);

            if (reqFin <= reqInicio)
            {
                continue;
            }

            var esDiaCompleto = reqInicio == TimeSpan.Zero && reqFin == TimeSpan.FromDays(1);
            if (esDiaCompleto && !diasCompletosVistos.Add(date.DayOfWeek))
            {
                continue;
            }

            var fechaCicloUtc = DateTime.SpecifyKind(date, DateTimeKind.Utc);
            tramos.Add((fechaCicloUtc, date.DayOfWeek, reqInicio, reqFin));
        }

        return tramos;
    }
}
