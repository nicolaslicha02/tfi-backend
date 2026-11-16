using GestionReservas.Application.Features.Calendario.DTOs;

namespace GestionReservas.Application.Features.Calendario;

/// <summary>
/// Contrato del servicio de consulta unificada de calendario de espacios físicos.
/// </summary>
public interface ICalendarioService
{
    /// <summary>
    /// Obtiene la lista unificada de eventos (Reservas, Bloqueos y Cursadas) ordenada por fecha/hora de inicio.
    /// </summary>
    Task<IReadOnlyList<EventoCalendarioDto>> GetEventosCalendarioAsync(
        CalendarioQueryParameters parameters,
        CancellationToken cancellationToken = default);
}
