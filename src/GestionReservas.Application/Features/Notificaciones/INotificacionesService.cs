using GestionReservas.Application.Features.Notificaciones.DTOs;

namespace GestionReservas.Application.Features.Notificaciones;

/// <summary>
/// Contrato del servicio de gestión de notificaciones para usuarios del sistema.
/// </summary>
public interface INotificacionesService
{
    /// <summary>
    /// Obtiene las notificaciones propias del usuario autenticado, ordenadas por fecha descendente.
    /// </summary>
    Task<IReadOnlyList<NotificacionDto>> GetMisNotificacionesAsync(bool? leida = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca como leída una notificación propia del usuario autenticado.
    /// </summary>
    Task<NotificacionDto> MarcarComoLeidaAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encola una notificación para un usuario específico en el contexto actual (para guardarse en la misma transacción).
    /// </summary>
    void EncolarNotificacion(int usuarioId, string mensaje);

    /// <summary>
    /// Encola notificaciones para los responsables activos del departamento del espacio (excluyendo opcionalmente al creador).
    /// </summary>
    Task EncolarNotificacionResponsablesDepartamentoAsync(
        int departamentoId,
        string mensaje,
        int? excluirUsuarioId = null,
        CancellationToken cancellationToken = default);
}
