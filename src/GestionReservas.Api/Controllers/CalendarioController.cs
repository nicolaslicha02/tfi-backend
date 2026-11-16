using GestionReservas.Application.Features.Calendario;
using GestionReservas.Application.Features.Calendario.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionReservas.Api.Controllers;

/// <summary>
/// Controlador para la consulta unificada de agenda y calendario de espacios físicos.
/// </summary>
[ApiController]
[Route("api/calendario")]
[Authorize]
public class CalendarioController : ControllerBase
{
    private readonly ICalendarioService _calendarioService;

    public CalendarioController(ICalendarioService calendarioService)
    {
        _calendarioService = calendarioService;
    }

    /// <summary>
    /// Devuelve la lista unificada de eventos (Reservas, Bloqueos y Cursadas) ordenada por inicio dentro de un rango de hasta 31 días.
    /// Las vistas diaria, semanal y mensual se resuelven parametrizando el rango desde/hasta.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventoCalendarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCalendario(
        [FromQuery] CalendarioQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var eventos = await _calendarioService.GetEventosCalendarioAsync(parameters, cancellationToken);
        return Ok(eventos);
    }
}
