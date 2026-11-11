namespace GestionReservas.Domain.Entities;

/// <summary>
/// Bitácora institucional de auditoría para registro y trazabilidad de operaciones críticas.
/// Actúa como caja negra del sistema para garantizar transparencia y control.
/// </summary>
public class Auditoria
{
    public int Id { get; set; }

    /// <summary>
    /// Usuario que ejecutó la acción (puede ser null para acciones del sistema previas a autenticación).
    /// </summary>
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    /// <summary>
    /// Fecha y hora UTC del suceso.
    /// </summary>
    public DateTime FechaHora { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Acción realizada (ej: "Crear", "Modificar", "Aprobar", "Rechazar", "Cancelar", "Bloquear").
    /// </summary>
    public string Accion { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la entidad afectada (ej: "Reserva", "Espacio", "Bloqueo", "Usuario").
    /// </summary>
    public string Entidad { get; set; } = string.Empty;

    /// <summary>
    /// Identificador de la entidad afectada.
    /// </summary>
    public string? EntidadId { get; set; }

    /// <summary>
    /// Motivo o justificación registrada para la acción.
    /// </summary>
    public string? Motivo { get; set; }

    /// <summary>
    /// Detalle técnico complementario o snapshot en formato JSON si aplica.
    /// </summary>
    public string? Detalle { get; set; }
}
