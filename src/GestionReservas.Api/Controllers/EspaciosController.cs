using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Espacios;
using GestionReservas.Application.Features.Espacios.DTOs;
using GestionReservas.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionReservas.Api.Controllers;

/// <summary>
/// Gestión de infraestructura de espacios físicos (aulas, laboratorios, auditorios), asignación departamental y disponibilidad.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EspaciosController : ControllerBase
{
    private readonly IEspaciosService _espaciosService;

    public EspaciosController(IEspaciosService espaciosService)
    {
        _espaciosService = espaciosService;
    }

    /// <summary>
    /// Obtiene un listado paginado de espacios físicos con filtros opcionales.
    /// Excluye por defecto los espacios dados de baja.
    /// </summary>
    /// <param name="parameters">Filtros de búsqueda, capacidad, ubicación, departamento y paginación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="200">Listado paginado de espacios retornado con éxito.</response>
    /// <response code="400">Parámetros de consulta inválidos.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No posee el permiso requerido (espacios.ver).</response>
    [HttpGet]
    [Authorize(Policy = PermisosDefinidos.EspaciosVer)]
    [ProducesResponseType(typeof(PagedResultDto<EspacioListadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<EspacioListadoDto>>> GetAll(
        [FromQuery] EspaciosQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var espacios = await _espaciosService.GetEspaciosAsync(parameters, cancellationToken);
        return Ok(espacios);
    }

    /// <summary>
    /// Consulta los espacios físicos que se encuentran disponibles (sin bloqueos ni reservas aprobadas)
    /// para una fecha y franja horaria determinadas, cumpliendo con criterios de capacidad y recursos.
    /// </summary>
    /// <param name="parameters">Fecha, hora desde, hora hasta y filtros adicionales de búsqueda.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="200">Listado de espacios disponibles retornado con éxito.</response>
    /// <response code="400">Fecha u horas no especificadas o franja horaria inconsistente (hasta &lt;= desde).</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No posee el permiso requerido (espacios.ver).</response>
    [HttpGet("disponibles")]
    [Authorize(Policy = PermisosDefinidos.EspaciosVer)]
    [ProducesResponseType(typeof(IReadOnlyList<EspacioListadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<EspacioListadoDto>>> GetDisponibles(
        [FromQuery] DisponibilidadEspacioQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var espacios = await _espaciosService.GetEspaciosDisponiblesAsync(parameters, cancellationToken);
        return Ok(espacios);
    }

    /// <summary>
    /// Obtiene el detalle completo de un espacio físico, su capacidad, tipología y equipamiento asignado.
    /// </summary>
    /// <param name="id">Identificador único del espacio.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="200">Detalle del espacio retornado con éxito.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No posee el permiso requerido (espacios.ver).</response>
    /// <response code="404">Espacio no encontrado o dado de baja.</response>
    [HttpGet("{id:int}")]
    [Authorize(Policy = PermisosDefinidos.EspaciosVer)]
    [ProducesResponseType(typeof(EspacioDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EspacioDetalleDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var espacio = await _espaciosService.GetEspacioByIdAsync(id, cancellationToken);
        return Ok(espacio);
    }

    /// <summary>
    /// Registra un nuevo espacio físico institucional.
    /// </summary>
    /// <param name="request">Datos de identificación, ubicación, capacidad, departamento y dotación de equipamiento.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="201">Espacio creado exitosamente.</response>
    /// <response code="400">Datos inválidos o faltantes.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No autorizado para administrar espacios del departamento asignado.</response>
    /// <response code="404">Tipo de espacio, departamento o equipamiento no encontrado.</response>
    /// <response code="409">El código de espacio ya existe en el sistema.</response>
    [HttpPost]
    [Authorize(Policy = PermisosDefinidos.EspaciosCrear)]
    [ProducesResponseType(typeof(EspacioDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EspacioDetalleDto>> Create(
        [FromBody] CrearEspacioRequestDto request,
        CancellationToken cancellationToken)
    {
        var espacio = await _espaciosService.CrearEspacioAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = espacio.Id }, espacio);
    }

    /// <summary>
    /// Modifica integralmente un espacio físico existente y su dotación de equipamiento.
    /// </summary>
    /// <param name="id">Identificador del espacio a modificar.</param>
    /// <param name="request">Nuevos datos del espacio y lista reemplazante de equipamientos.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="200">Espacio modificado exitosamente.</response>
    /// <response code="400">Datos inválidos o faltantes.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No autorizado para administrar espacios en dicho departamento.</response>
    /// <response code="404">Espacio, tipo, departamento o equipamiento no encontrado.</response>
    /// <response code="409">El nuevo código de espacio ya se encuentra utilizado por otro espacio.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = PermisosDefinidos.EspaciosEditar)]
    [ProducesResponseType(typeof(EspacioDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EspacioDetalleDto>> Update(
        int id,
        [FromBody] ActualizarEspacioRequestDto request,
        CancellationToken cancellationToken)
    {
        var espacio = await _espaciosService.ActualizarEspacioAsync(id, request, cancellationToken);
        return Ok(espacio);
    }

    /// <summary>
    /// Da de baja lógica a un espacio físico.
    /// No se permite la baja si el espacio posee reservas futuras pendientes o aprobadas.
    /// </summary>
    /// <param name="id">Identificador del espacio a dar de baja.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="204">Baja lógica realizada con éxito.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No autorizado para dar de baja espacios de este departamento.</response>
    /// <response code="404">Espacio no encontrado o ya dado de baja.</response>
    /// <response code="409">El espacio posee reservas futuras pendientes o aprobadas que impiden su baja.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = PermisosDefinidos.EspaciosEliminar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        await _espaciosService.EliminarEspacioAsync(id, cancellationToken);
        return NoContent();
    }
}
