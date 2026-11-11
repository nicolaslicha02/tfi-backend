using GestionReservas.Application.Features.Notificaciones;
using GestionReservas.Application.Features.Notificaciones.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionReservas.Api.Controllers;

/// <summary>
/// Controlador para consulta y lectura de notificaciones del usuario autenticado.
/// </summary>
[ApiController]
[Route("api/notificaciones")]
[Authorize]
public class NotificacionesController : ControllerBase
{
    private readonly INotificacionesService _notificacionesService;

    public NotificacionesController(INotificacionesService notificacionesService)
    {
        _notificacionesService = notificacionesService;
    }

    /// <summary>
    /// Devuelve las notificaciones propias del usuario autenticado, ordenadas por fecha descendente.
    /// </summary>
    /// <param name="leida">Filtro opcional por estado de lectura (true = leídas, false = no leídas).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificacionDto>>> GetMisNotificaciones(
        [FromQuery] bool? leida = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _notificacionesService.GetMisNotificacionesAsync(leida, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Marca una notificación propia como leída.
    /// </summary>
    /// <param name="id">Identificador de la notificación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPatch("{id:int}/leer")]
    [ProducesResponseType(typeof(NotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificacionDto>> MarcarComoLeida(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _notificacionesService.MarcarComoLeidaAsync(id, cancellationToken);
        return Ok(result);
    }
}
