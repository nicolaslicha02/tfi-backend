using GestionReservas.Application.Features.Comisiones;
using GestionReservas.Application.Features.Comisiones.DTOs;
using GestionReservas.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionReservas.Api.Controllers;

[ApiController]
[Route("api/comisiones")]
[Authorize(Policy = PermisosDefinidos.EspaciosVer)]
public sealed class ComisionesController : ControllerBase
{
    private readonly IComisionesService _service;

    public ComisionesController(IComisionesService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ComisionListadoDto>>> Listar([FromQuery] int departamentoId, CancellationToken ct)
    {
        if (departamentoId <= 0) return BadRequest("departamentoId debe ser mayor que cero.");
        return Ok(await _service.ListarAsync(departamentoId, ct));
    }

    [HttpPost("{id:int}/asignaciones")]
    [Authorize(Policy = PermisosDefinidos.EspaciosEditar)]
    public async Task<ActionResult<AsignacionComisionDto>> Asignar(int id, [FromBody] GuardarAsignacionComisionRequest request, CancellationToken ct)
    {
        var result = await _service.AsignarAsync(id, request, ct);
        return Created($"/api/comisiones/{id}/asignaciones/{result.Id}", result);
    }

    [HttpPut("{id:int}/asignaciones/{asignacionId:int}")]
    [Authorize(Policy = PermisosDefinidos.EspaciosEditar)]
    public async Task<ActionResult<AsignacionComisionDto>> Reasignar(int id, int asignacionId, [FromBody] GuardarAsignacionComisionRequest request, CancellationToken ct)
        => Ok(await _service.ReasignarAsync(id, asignacionId, request, ct));

    [HttpDelete("{id:int}/asignaciones/{asignacionId:int}")]
    [Authorize(Policy = PermisosDefinidos.EspaciosEditar)]
    public async Task<IActionResult> Quitar(int id, int asignacionId, CancellationToken ct)
    {
        await _service.QuitarAsync(id, asignacionId, ct);
        return NoContent();
    }
}
