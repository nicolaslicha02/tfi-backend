namespace GestionReservas.Domain.Entities;

/// <summary>
/// Representa una inhabilitación temporal de un espacio físico por tareas de mantenimiento,
/// refacciones, reparaciones o actos institucionales de fuerza mayor.
/// </summary>
public class Bloqueo
{
    public int Id { get; set; }

    /// <summary>
    /// Espacio físico afectado por el bloqueo.
    /// </summary>
    public int EspacioId { get; set; }
    public Espacio Espacio { get; set; } = null!;

    /// <summary>
    /// Fecha y hora desde la cual rige la inhabilitación del espacio.
    /// </summary>
    public DateTime FechaHoraDesde { get; set; }

    /// <summary>
    /// Fecha y hora hasta la cual rige la inhabilitación del espacio (debe ser posterior a FechaHoraDesde).
    /// </summary>
    public DateTime FechaHoraHasta { get; set; }

    /// <summary>
    /// Motivo o justificación del bloqueo (ej: "Pintura de paredes y cambio de luminarias").
    /// </summary>
    public string Motivo { get; set; } = string.Empty;

    /// <summary>
    /// Usuario responsable o autoridad que registró el bloqueo.
    /// </summary>
    public int CreadoPorUsuarioId { get; set; }
    public Usuario CreadoPorUsuario { get; set; } = null!;

    /// <summary>
    /// Fecha y hora de creación del registro de bloqueo.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
