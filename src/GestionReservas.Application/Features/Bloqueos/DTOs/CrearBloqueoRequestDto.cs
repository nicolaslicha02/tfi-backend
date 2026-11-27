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
    /// IDs de las reservas que el usuario confirmó cancelar. Los IDs se comparan como conjunto.
    /// </summary>
    public List<int>? ReservasConfirmadas { get; set; }

    /// <summary>
    /// Versiones devueltas con cada reserva afectada. Si alguna reserva cambió desde que se mostró,
    /// la confirmación queda obsoleta y el servidor devuelve 409.
    /// </summary>
    public List<ReservaConfirmadaVersionDto>? VersionesReservasConfirmadas { get; set; }

    /// <summary>
    /// Si es true, cancela solo reservas actuales que coinciden con la selección confirmada.
    /// Si es false y existen reservas afectadas, rechaza la operación con código 409 Conflict.
    /// </summary>
    public bool CancelarReservasAfectadas { get; set; } = false;
}
