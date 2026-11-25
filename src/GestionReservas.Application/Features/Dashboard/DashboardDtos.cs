namespace GestionReservas.Application.Features.Dashboard;

public sealed record OcupacionDepartamentoDto(int DepartamentoId, string DepartamentoNombre, int EspaciosActivos, double PorcentajeOcupacion);

public sealed record DashboardResumenDto(
    int ReservasActivas,
    int SolicitudesPendientes,
    int EspaciosTotales,
    int Conflictos,
    int DepartamentosConActividad,
    DateTime Desde,
    DateTime Hasta,
    IReadOnlyList<OcupacionDepartamentoDto> OcupacionPorDepartamento);
