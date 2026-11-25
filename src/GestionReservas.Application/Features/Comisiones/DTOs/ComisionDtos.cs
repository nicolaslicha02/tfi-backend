namespace GestionReservas.Application.Features.Comisiones.DTOs;

public sealed record ComisionListadoDto(
    int Id,
    string Codigo,
    string MateriaCodigo,
    string MateriaNombre,
    int DepartamentoId,
    string DepartamentoNombre,
    int AnioCursada,
    string Periodo,
    string Turno,
    string Modalidad,
    int AlumnosInscriptos,
    string EstadoAsignacion,
    IReadOnlyList<AsignacionComisionDto> Asignaciones);

public sealed record AsignacionComisionDto(
    int Id,
    int EspacioId,
    string EspacioCodigo,
    string EspacioNombre,
    DayOfWeek DiaSemana,
    TimeSpan HoraInicio,
    TimeSpan HoraFin);

public sealed record GuardarAsignacionComisionRequest(
    int EspacioId,
    DayOfWeek DiaSemana,
    TimeSpan HoraInicio,
    TimeSpan HoraFin);
