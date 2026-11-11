using GestionReservas.Application.Features.Catalogos;
using GestionReservas.Application.Features.Catalogos.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionReservas.Api.Controllers;

/// <summary>
/// Catálogo flexible de equipamientos tecnológicos e insumos para espacios físicos.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EquipamientosController : ControllerBase
{
    private readonly ICatalogosService _catalogosService;

    public EquipamientosController(ICatalogosService catalogosService)
    {
        _catalogosService = catalogosService;
    }

    /// <summary>
    /// Obtiene el catálogo de equipamientos disponibles.
    /// </summary>
    /// <response code="200">Listado de equipamientos retornado con éxito.</response>
    /// <response code="401">El usuario no está autenticado.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EquipamientoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<EquipamientoDto>>> GetAll(CancellationToken cancellationToken)
    {
        var equipamientos = await _catalogosService.GetEquipamientosAsync(cancellationToken);
        return Ok(equipamientos);
    }

    /// <summary>
    /// Registra un nuevo equipamiento en el catálogo del sistema.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(EquipamientoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EquipamientoDto>> Create([FromBody] CrearEquipamientoRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _catalogosService.CrearEquipamientoAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
