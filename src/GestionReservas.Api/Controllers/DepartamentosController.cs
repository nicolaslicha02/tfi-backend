using GestionReservas.Application.Features.Catalogos;
using GestionReservas.Application.Features.Catalogos.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionReservas.Api.Controllers;

/// <summary>
/// Catálogo de departamentos y secretarías académicas institucionales.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<DepartamentoDto>>> GetAll(CancellationToken cancellationToken)
    {
        var departamentos = await _catalogosService.GetDepartamentosAsync(cancellationToken);
        return Ok(departamentos);
    }
}
