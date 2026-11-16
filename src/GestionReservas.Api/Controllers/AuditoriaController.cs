using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Bitacora;
using GestionReservas.Application.Features.Bitacora.DTOs;
using GestionReservas.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionReservas.Api.Controllers;

[ApiController]
[Route("api/auditoria")]
[Authorize(Policy = PermisosDefinidos.AuditoriaVer)]
public sealed class AuditoriaController : ControllerBase
{
    private readonly IAuditoriaService _auditoriaService;

    public AuditoriaController(IAuditoriaService auditoriaService) => _auditoriaService = auditoriaService;

    /// <summary>Consulta la bitácora en orden cronológico inverso, con filtros y paginación.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<AuditoriaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResultDto<AuditoriaDto>>> Get(
        [FromQuery] AuditoriaQueryParameters query,
        CancellationToken cancellationToken)
        => Ok(await _auditoriaService.GetAuditoriaAsync(query, cancellationToken));
}
