using GestionReservas.Application.Features.Dashboard;
using GestionReservas.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionReservas.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = PermisosDefinidos.ReservasVerPendientes)]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;
    public DashboardController(IDashboardService service) => _service = service;

    [HttpGet("resumen")]
    public async Task<ActionResult<DashboardResumenDto>> Resumen([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, CancellationToken ct)
        => Ok(await _service.ObtenerResumenAsync(desde, hasta, ct));
}
