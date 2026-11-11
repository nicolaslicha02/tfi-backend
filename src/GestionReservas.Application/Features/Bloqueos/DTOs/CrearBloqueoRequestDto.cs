using System.Text.Json.Serialization;

namespace GestionReservas.Application.Features.Bloqueos.DTOs;

/// <summary>
/// Solicitud para registrar una inhabilitación o bloqueo temporal sobre un espacio físico.
/// </summary>
public class CrearBloqueoRequestDto
{
    public int EspacioId { get; set; }

    private DateTime _desde;

    /// <summary>
    /// Fecha y hora de inicio de la inhabilitación.
    /// </summary>
    [JsonPropertyName("desde")]
    public DateTime Desde
    {
        get => _desde;
        set => _desde = value;
    }

    /// <summary>
    /// Alias compatible con FechaHoraDesde.
    /// </summary>
    [JsonPropertyName("fechaHoraDesde")]
    public DateTime FechaHoraDesde
    {
        get => _desde;
        set => _desde = value;
    }

    private DateTime _hasta;

    /// <summary>
    /// Fecha y hora de finalización de la inhabilitación (debe ser mayor a Desde).
    /// </summary>
    [JsonPropertyName("hasta")]
    public DateTime Hasta
    {
        get => _hasta;
        set => _hasta = value;
    }

    /// <summary>
    /// Alias compatible con FechaHoraHasta.
    /// </summary>
    [JsonPropertyName("fechaHoraHasta")]
    public DateTime FechaHoraHasta
    {
        get => _hasta;
        set => _hasta = value;
    }

    /// <summary>
    /// Justificación institucional o motivo operativo del bloqueo.
    /// </summary>
    public string Motivo { get; set; } = string.Empty;

    /// <summary>
    /// Si es true, cancela automáticamente las reservas pendientes y aprobadas afectadas en el rango.
    /// Si es false y existen reservas afectadas, rechaza la operación con código 409 Conflict.
    /// </summary>
    public bool CancelarReservasAfectadas { get; set; } = false;
}
