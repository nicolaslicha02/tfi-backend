using GestionReservas.Domain.Enums;

namespace GestionReservas.Domain.Entities;

/// <summary>
/// Representa una solicitud de reserva eventual de un espacio físico.
/// Incluye soporte para concurrencia optimista mediante RowVersion/xmin.
/// </summary>
public class Reserva
{
    public int Id { get; set; }

    /// <summary>
    /// Usuario que realiza la solicitud de reserva.
    /// </summary>
    public int SolicitanteId { get; set; }
    public Usuario Solicitante { get; set; } = null!;

    /// <summary>
    /// Espacio físico solicitado.
    /// </summary>
    public int EspacioId { get; set; }
    public Espacio Espacio { get; set; } = null!;

    /// <summary>
    /// Fecha y hora de inicio de la actividad.
    /// </summary>
    public DateTime FechaHoraInicio { get; set; }

    /// <summary>
    /// Fecha y hora de finalización de la actividad (debe ser posterior a FechaHoraInicio).
    /// </summary>
    public DateTime FechaHoraFin { get; set; }

    /// <summary>
    /// Naturaleza o tipo de la actividad a desarrollar.
    /// </summary>
    public TipoActividad TipoActividad { get; set; }

    /// <summary>
    /// Cantidad estimada de asistentes (debe ser mayor a 0 y no superar la capacidad del espacio).
    /// </summary>
    public int AsistentesEstimados { get; set; }

    /// <summary>
    /// Justificación o motivo académico/institucional del pedido.
    /// </summary>
    public string Motivo { get; set; } = string.Empty;

    /// <summary>
    /// Observaciones o requerimientos complementarios.
    /// </summary>
    public string? Observaciones { get; set; }

    /// <summary>
    /// Estado actual en el ciclo de vida de la reserva.
    /// </summary>
    public EstadoReserva Estado { get; set; } = EstadoReserva.Solicitada;

    /// <summary>
    /// Usuario responsable o administrador que resolvió la solicitud (aprobó o rechazó).
    /// </summary>
    public int? ResueltoPorUsuarioId { get; set; }
    public Usuario? ResueltoPorUsuario { get; set; }

    /// <summary>
    /// Fecha y hora en la que se resolvió la solicitud.
    /// </summary>
    public DateTime? FechaResolucion { get; set; }

    /// <summary>
    /// Motivo documentado en caso de rechazo o cancelación.
    /// </summary>
    public string? MotivoRechazo { get; set; }

    /// <summary>
    /// Fecha y hora en que se ingresó la solicitud al sistema.
    /// </summary>
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Token de concurrencia optimista para PostgreSQL (mapeado a xmin).
    /// </summary>
    public uint RowVersion { get; set; }

    // Relaciones
    public ICollection<ReservaEquipamiento> EquipamientosRequeridos { get; set; } = new List<ReservaEquipamiento>();
}
