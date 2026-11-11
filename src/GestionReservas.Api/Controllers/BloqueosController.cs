using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Bloqueos;
using GestionReservas.Application.Features.Bloqueos.DTOs;
using GestionReservas.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionReservas.Api.Controllers;

/// <summary>
/// Controlador de inhabilitaciones y bloqueos temporales de espacios físicos.
/// </summary>
[ApiController]
[Route("api/bloqueos")]
[Authorize]
public class BloqueosController : ControllerBase
{
    private readonly IBloqueosService _bloqueosService;

    public BloqueosController(IBloqueosService bloqueosService)
    {
        _bloqueosService = bloqueosService;
    }

    /// <summary>
    /// Registra un nuevo bloqueo temporal sobre un espacio.
    /// Si hay reservas afectadas y cancelarReservasAfectadas es false, devuelve 409 con el listado.
    /// Si es true, cancela atómicamente las reservas afectadas y las devuelve en la respuesta.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = PermisosDefinidos.BloqueosCrear)]
    [ProducesResponseType(typeof(CrearBloqueoResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CrearBloqueoResponseDto>> CrearBloqueo(
        [FromBody] CrearBloqueoRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _bloqueosService.CrearBloqueoAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Consulta el listado paginado de bloqueos según filtros opcionales (espacioId, desde, hasta, vigentes)
    /// respetando el alcance departamental del usuario.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = PermisosDefinidos.BloqueosVer)]
    [ProducesResponseType(typeof(PagedResultDto<BloqueoListadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<BloqueoListadoDto>>> GetBloqueos(
        [FromQuery] BloqueosQueryParameters queryParams,
        CancellationToken cancellationToken)
    {
        var result = await _bloqueosService.GetBloqueosAsync(queryParams, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Levanta o elimina un bloqueo existente y registra la acción en la bitácora de auditoría.
    /// Nota: Las reservas canceladas por el bloqueo no son restauradas automáticamente.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = PermisosDefinidos.BloqueosCrear)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarBloqueo(int id, CancellationToken cancellationToken)
    {
        await _bloqueosService.EliminarBloqueoAsync(id, cancellationToken);
        return NoContent();
    }
}
