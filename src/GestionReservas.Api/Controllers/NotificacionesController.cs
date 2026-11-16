using GestionReservas.Application.Common.Models;
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
    /// Devuelve las notificaciones propias del usuario autenticado de forma paginada, ordenadas por fecha descendente.
    /// </summary>
    /// <param name="leida">Filtro opcional por estado de lectura (true = leídas, false = no leídas).</param>
    /// <param name="page">Número de página (por defecto 1).</param>
    /// <param name="pageSize">Cantidad de registros por página (por defecto 20, máximo 100).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<NotificacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<NotificacionDto>>> GetMisNotificaciones(
        [FromQuery] bool? leida = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _notificacionesService.GetMisNotificacionesAsync(leida, page, pageSize, cancellationToken);
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
