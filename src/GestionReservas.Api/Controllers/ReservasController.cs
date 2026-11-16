using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Reservas;
using GestionReservas.Application.Features.Reservas.DTOs;
using GestionReservas.Domain.Constants;
using GestionReservas.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionReservas.Api.Controllers;

/// <summary>
/// Controlador de reservas de espacios. Toda la lógica de negocio reside en <see cref="IReservasService"/>.
/// </summary>
[ApiController]
[Authorize]
public class ReservasController : ControllerBase
{
    private readonly IReservasService _reservasService;

    public ReservasController(IReservasService reservasService)
    {
        _reservasService = reservasService;
    }

    /// <summary>Crea una solicitud de reserva (auto-aprobada si el solicitante administra el espacio).</summary>
    [HttpPost("api/reservas")]
    [Authorize(Policy = PermisosDefinidos.ReservasSolicitar)]
    [ProducesResponseType(typeof(ReservaDetalleDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ReservaDetalleDto>> Crear([FromBody] CrearReservaRequestDto request, CancellationToken ct)
    {
        var result = await _reservasService.CreateReservaAsync(request, ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = result.Id }, result);
    }

    /// <summary>Reservas del usuario autenticado, paginadas.</summary>
    [HttpGet("api/mis-reservas")]
    [Authorize(Policy = PermisosDefinidos.ReservasVerPropias)]
    public async Task<ActionResult<PagedResultDto<ReservaListadoDto>>> MisReservas([FromQuery] MisReservasQueryParameters query, CancellationToken ct)
        => Ok(await _reservasService.GetMisReservasAsync(query, ct));

    /// <summary>Listado administrativo general de reservas con filtros y alcance departamental.</summary>
    [HttpGet("api/reservas")]
    [Authorize(Policy = PermisosDefinidos.ReservasVerPendientes)]
    [ProducesResponseType(typeof(PagedResultDto<ReservaListadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<ReservaListadoDto>>> GetReservas([FromQuery] ReservasQueryParameters query, CancellationToken ct)
        => Ok(await _reservasService.GetReservasAdministrativasAsync(query, ct));

    /// <summary>Solicitudes pendientes de aprobación (paginadas).</summary>
    [HttpGet("api/reservas/pendientes")]
    [Authorize(Policy = PermisosDefinidos.ReservasVerPendientes)]
    [ProducesResponseType(typeof(PagedResultDto<ReservaListadoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ReservaListadoDto>>> Pendientes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _reservasService.GetReservasPorEstadoAsync(EstadoReserva.PendienteAprobacion, page, pageSize, ct));

    /// <summary>Reservas canceladas (paginadas).</summary>
    [HttpGet("api/reservas/canceladas")]
    [Authorize(Policy = PermisosDefinidos.ReservasVerPendientes)]
    [ProducesResponseType(typeof(PagedResultDto<ReservaListadoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ReservaListadoDto>>> Canceladas(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _reservasService.GetReservasPorEstadoAsync(EstadoReserva.Cancelada, page, pageSize, ct));

    /// <summary>Reservas rechazadas (paginadas).</summary>
    [HttpGet("api/reservas/rechazadas")]
    [Authorize(Policy = PermisosDefinidos.ReservasVerPendientes)]
    [ProducesResponseType(typeof(PagedResultDto<ReservaListadoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ReservaListadoDto>>> Rechazadas(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _reservasService.GetReservasPorEstadoAsync(EstadoReserva.Rechazada, page, pageSize, ct));

    /// <summary>Detalle de una reserva con su historial de cambios (solicitante, responsable, Secretaría o Admin).</summary>
    [HttpGet("api/reservas/{id:int}")]
    public async Task<ActionResult<ReservaDetalleDto>> ObtenerPorId(int id, CancellationToken ct)
        => Ok(await _reservasService.GetReservaByIdAsync(id, ct));

    /// <summary>Cambia el estado: accion = aprobar | rechazar | cancelar.</summary>
    [HttpPatch("api/reservas/{id:int}")]
    public async Task<ActionResult<ReservaDetalleDto>> CambiarEstado(int id, [FromBody] CambiarEstadoReservaRequestDto request, CancellationToken ct)
        => Ok(await _reservasService.CambiarEstadoReservaAsync(id, request, ct));

    /// <summary>Modifica una reserva; si estaba aprobada vuelve a pendiente.</summary>
    [HttpPut("api/reservas/{id:int}")]
    [Authorize(Policy = PermisosDefinidos.ReservasModificar)]
    public async Task<ActionResult<ReservaDetalleDto>> Modificar(int id, [FromBody] ActualizarReservaRequestDto request, CancellationToken ct)
        => Ok(await _reservasService.UpdateReservaAsync(id, request, ct));
}
