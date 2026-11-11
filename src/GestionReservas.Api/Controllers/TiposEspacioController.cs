using GestionReservas.Application.Features.Catalogos;
using GestionReservas.Application.Features.Catalogos.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionReservas.Api.Controllers;

/// <summary>
/// Catálogo de tipos funcionales de espacios físicos.
/// </summary>
[ApiController]
[Route("api/tipos-espacio")]
[Authorize]
public class TiposEspacioController : ControllerBase
{
    private readonly ICatalogosService _catalogosService;

    public TiposEspacioController(ICatalogosService catalogosService)
    {
        _catalogosService = catalogosService;
    }

    /// <summary>
    /// Obtiene el catálogo de tipos y clasificaciones funcionales de espacios.
    /// </summary>
    /// <response code="200">Listado de tipos de espacio retornado con éxito.</response>
    /// <response code="401">El usuario no está autenticado.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TipoEspacioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TipoEspacioDto>>> GetAll(CancellationToken cancellationToken)
    {
        var tipos = await _catalogosService.GetTiposEspacioAsync(cancellationToken);
        return Ok(tipos);
    }
}
