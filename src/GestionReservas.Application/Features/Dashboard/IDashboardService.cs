namespace GestionReservas.Application.Features.Dashboard;

public interface IDashboardService
{
    Task<DashboardResumenDto> ObtenerResumenAsync(DateTime? desde, DateTime? hasta, CancellationToken cancellationToken = default);
}
