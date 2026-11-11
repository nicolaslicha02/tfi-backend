using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionReservas.Application.Features.Espacios.Services;

/// <summary>
/// Implementación de la lógica de disponibilidad y cálculo de superposiciones para espacios físicos.
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

        return !tieneReservaAprobada;
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

        return bloqueosQuery
            .Union(reservasQuery)
            .Union(inhabilitadosQuery);
    }
}
