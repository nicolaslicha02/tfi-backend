using GestionReservas.Application.Features.Catalogos;
using GestionReservas.Application.Features.Catalogos.DTOs;
using GestionReservas.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionReservas.Api.Controllers;

/// <summary>
/// Catálogo de departamentos y secretarías académicas institucionales.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PermisosDefinidos.CatalogosVer)]
public class DepartamentosController : ControllerBase
{
    private readonly ICatalogosService _catalogosService;

    public DepartamentosController(ICatalogosService catalogosService)
    {
        _catalogosService = catalogosService;
    }

    /// <summary>
    /// Obtiene el catálogo completo de departamentos y secretarías.
    /// </summary>
    /// <response code="200">Listado de departamentos retornado con éxito.</response>
    /// <response code="401">El usuario no está autenticado.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DepartamentoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<DepartamentoDto>>> GetAll(CancellationToken cancellationToken)
    {
        var departamentos = await _catalogosService.GetDepartamentosAsync(cancellationToken);
        return Ok(departamentos);
    }

    /// <summary>Registra un departamento institucional.</summary>
    [HttpPost]
    [Authorize(Policy = PermisosDefinidos.UsuariosGestionar)]
    [ProducesResponseType(typeof(DepartamentoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DepartamentoDto>> Create([FromBody] GuardarDepartamentoRequestDto request, CancellationToken ct)
    {
        var department = await _catalogosService.CrearDepartamentoAsync(request, ct);
        return Created("/api/departamentos", department);
    }

    /// <summary>Actualiza un departamento institucional.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = PermisosDefinidos.UsuariosGestionar)]
    [ProducesResponseType(typeof(DepartamentoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DepartamentoDto>> Update(int id, [FromBody] GuardarDepartamentoRequestDto request, CancellationToken ct)
        => Ok(await _catalogosService.ActualizarDepartamentoAsync(id, request, ct));

    /// <summary>Elimina un departamento que no esté asociado a datos institucionales.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = PermisosDefinidos.UsuariosGestionar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _catalogosService.EliminarDepartamentoAsync(id, ct);
        return NoContent();
    }
}
