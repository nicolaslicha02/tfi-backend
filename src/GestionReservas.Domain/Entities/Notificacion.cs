namespace GestionReservas.Domain.Entities;

/// <summary>
/// Representa una notificación dirigida a un usuario del sistema (ej: aviso de resolución de reserva).
/// </summary>
public class Notificacion
{
    public int Id { get; set; }

    /// <summary>
    /// Usuario destinatario del aviso.
    /// </summary>
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    /// <summary>
    /// Mensaje descriptivo de la notificación.
    /// </summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>
    /// Indica si el usuario ya visualizó o leyó la notificación.
    /// </summary>
    public bool Leida { get; set; } = false;

    /// <summary>
    /// Fecha y hora de emisión del aviso.
    /// </summary>
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
