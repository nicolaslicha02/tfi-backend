using GestionReservas.Application.Common.Exceptions;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionReservas.Application.Features.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly GestionReservas.Application.Features.Reservas.IReservasService _reservasService;

    public DashboardService(IApplicationDbContext context, ICurrentUserService currentUser,
        GestionReservas.Application.Features.Reservas.IReservasService reservasService)
    {
        _context = context;
        _currentUser = currentUser;
        _reservasService = reservasService;
    }

    public async Task<DashboardResumenDto> ObtenerResumenAsync(DateTime? desde, DateTime? hasta, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsInRole("Administrador") && !_currentUser.IsInRole("SecretariaAcademica"))
            throw new ForbiddenException("Solo Secretaría Académica o Administración puede consultar el resumen institucional.");
        var inicio = (desde ?? DateTime.UtcNow.Date.AddDays(-30)).ToUniversalTime();
        var fin = (hasta ?? DateTime.UtcNow).ToUniversalTime();
        if (fin <= inicio || fin - inicio > TimeSpan.FromDays(93))
            throw new ValidationException("Rango", "El rango debe ser válido y no superar 93 días.");

        var reservas = _context.Reservas.AsNoTracking();
        var ahora = DateTime.UtcNow;
        var reservasActivas = await reservas.CountAsync(r => r.Estado == EstadoReserva.Aprobada && r.FechaHoraFin >= ahora, cancellationToken);
        var pendientes = await reservas.CountAsync(r => r.Estado == EstadoReserva.PendienteAprobacion || r.Estado == EstadoReserva.Solicitada, cancellationToken);
        var espacios = _context.Espacios.AsNoTracking().Where(e => e.FechaBaja == null);
        var espaciosTotales = await espacios.CountAsync(cancellationToken);

        var aprobadasPeriodo = reservas.Where(r => r.Estado == EstadoReserva.Aprobada && r.FechaHoraInicio < fin && r.FechaHoraFin > inicio);
        var conflictos = (await _reservasService.GetConflictosAsync(inicio, fin, cancellationToken)).Count;

        var actividad = await aprobadasPeriodo.Select(r => r.Espacio.DepartamentoId).Distinct().CountAsync(cancellationToken);
        var ocupacion = await espacios.GroupBy(e => new { e.DepartamentoId, e.Departamento.Nombre })
            .Select(g => new { g.Key.DepartamentoId, g.Key.Nombre, Cantidad = g.Count(),
                Minutos = g.SelectMany(e => e.Reservas).Where(r => r.Estado == EstadoReserva.Aprobada && r.FechaHoraInicio < fin && r.FechaHoraFin > inicio)
                    .Sum(r => (double?)((r.FechaHoraFin < fin ? r.FechaHoraFin : fin) - (r.FechaHoraInicio > inicio ? r.FechaHoraInicio : inicio)).TotalMinutes) ?? 0 })
            .OrderBy(x => x.Nombre).ToListAsync(cancellationToken);
        var porDepartamento = ocupacion.Select(x => new OcupacionDepartamentoDto(x.DepartamentoId, x.Nombre, x.Cantidad,
            Math.Round(Math.Min(100d, x.Minutos / (x.Cantidad * (fin - inicio).TotalMinutes) * 100d), 2))).ToList();
        return new DashboardResumenDto(reservasActivas, pendientes, espaciosTotales, conflictos, actividad, inicio, fin, porDepartamento);
    }
}
