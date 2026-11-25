using GestionReservas.Application.Features.Catalogos;
using GestionReservas.Application.Features.Catalogos.DTOs;
using GestionReservas.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionReservas.Api.Controllers;

/// <summary>
/// Catálogo de roles de usuario del sistema.
/// </summary>
[ApiController]
[Route("api/roles")]
[Authorize(Policy = PermisosDefinidos.UsuariosGestionar)]
public class RolesController : ControllerBase
{
    private readonly ICatalogosService _catalogosService;

    public RolesController(ICatalogosService catalogosService)
    {
        _catalogosService = catalogosService;
    }

    /// <summary>
    /// Obtiene el catálogo de roles definidos en el sistema.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RolDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<RolDto>>> GetAll(CancellationToken cancellationToken)
    {
        var roles = await _catalogosService.GetRolesAsync(cancellationToken);
        return Ok(roles);
    }

    /// <summary>Registra un rol personalizado sin permisos asignados.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(RolDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RolDto>> Create([FromBody] GuardarRolRequestDto request, CancellationToken cancellationToken)
    {
        var role = await _catalogosService.CrearRolAsync(request, cancellationToken);
        return Created("/api/roles", role);
    }

    /// <summary>Actualiza nombre y descripción de un rol personalizado.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(RolDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RolDto>> Update(int id, [FromBody] GuardarRolRequestDto request, CancellationToken cancellationToken)
    {
        var role = await _catalogosService.ActualizarRolAsync(id, request, cancellationToken);
        return Ok(role);
    }

    /// <summary>Elimina un rol personalizado que no esté asignado a usuarios.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _catalogosService.EliminarRolAsync(id, cancellationToken);
        return NoContent();
    }
}
