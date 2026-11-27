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

    /// <summary>
    /// Versión opaca de PostgreSQL para confirmar que la reserva no cambió desde que se mostró.
    /// </summary>
    public uint Version { get; set; }
}

/// <summary>
/// Versión que el cliente confirma para una reserva mostrada previamente.
/// </summary>
public class ReservaConfirmadaVersionDto
{
    /// <summary>Identificador de la reserva mostrada al usuario.</summary>
    public int Id { get; set; }

    /// <summary>Valor de versión devuelto en la lista de reservas afectadas.</summary>
    public uint Version { get; set; }
}
