using GestionReservas.Domain.Enums;

namespace GestionReservas.Application.Features.Bloqueos.DTOs;

/// <summary>
/// Representación de una reserva afectada (aprobada o pendiente) por un bloqueo de espacio.
/// </summary>
public class ReservaAfectadaDto
{
    public int Id { get; set; }
    public int SolicitanteId { get; set; }
    public string Solicitante { get; set; } = string.Empty;
    public string SolicitanteEmail { get; set; } = string.Empty;
    public DateTime FechaHoraInicio { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public EstadoReserva Estado { get; set; }
}
