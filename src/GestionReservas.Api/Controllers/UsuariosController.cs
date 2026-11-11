using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Usuarios;
using GestionReservas.Application.Features.Usuarios.DTOs;
using GestionReservas.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionReservas.Api.Controllers;

/// <summary>
/// Administración institucional de cuentas de usuario, roles y estados de habilitación.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PermisosDefinidos.UsuariosGestionar)]
public class UsuariosController : ControllerBase
{
    private readonly IUsuariosService _usuariosService;

    public UsuariosController(IUsuariosService usuariosService)
    {
        _usuariosService = usuariosService;
    }

    /// <summary>
    /// Crea un nuevo usuario en el sistema.
    /// </summary>
    /// <param name="request">Datos del usuario, contraseña inicial y roles a asignar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="201">Usuario creado exitosamente.</response>
    /// <response code="400">Datos inválidos o política de contraseña no cumplida.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No posee el permiso requerido (usuarios.gestionar).</response>
    /// <response code="404">Rol o departamento especificado no existe.</response>
    /// <response code="409">El correo electrónico ya se encuentra registrado.</response>
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioDetalleDto>> Create(
        [FromBody] CrearUsuarioRequestDto request,
        CancellationToken cancellationToken)
    {
        var usuario = await _usuariosService.CrearUsuarioAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, usuario);
    }

    /// <summary>
    /// Obtiene un listado paginado de usuarios con opciones de búsqueda y filtrado.
    /// </summary>
    /// <param name="parameters">Parámetros de paginación y filtros.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="200">Listado paginado de usuarios retornado con éxito.</response>
    /// <response code="400">Parámetros de paginación inválidos.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No posee el permiso requerido.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<UsuarioListadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<UsuarioListadoDto>>> GetAll(
        [FromQuery] UsuariosQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var pagedUsers = await _usuariosService.GetUsuariosAsync(parameters, cancellationToken);
        return Ok(pagedUsers);
    }

    /// <summary>
    /// Obtiene el detalle completo de un usuario por su identificador.
    /// </summary>
    /// <param name="id">Identificador numérico del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="200">Detalle del usuario retornado con éxito.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No posee el permiso requerido.</response>
    /// <response code="404">Usuario no encontrado.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UsuarioDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioDetalleDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var usuario = await _usuariosService.GetUsuarioByIdAsync(id, cancellationToken);
        return Ok(usuario);
    }

    /// <summary>
    /// Actualiza parcialmente los datos, roles o estado de un usuario existente.
    /// </summary>
    /// <param name="id">Identificador del usuario a modificar.</param>
    /// <param name="request">Campos a actualizar (solo los presentes serán modificados).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="200">Usuario modificado exitosamente.</response>
    /// <response code="400">Datos inválidos o política de contraseña no cumplida.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No posee el permiso requerido.</response>
    /// <response code="404">Usuario, rol o departamento no encontrado.</response>
    /// <response code="409">Conflicto con reglas de negocio (ej. auto-desactivación de administrador o dejar al sistema sin administradores).</response>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(UsuarioDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioDetalleDto>> Update(
        int id,
        [FromBody] ActualizarUsuarioRequestDto request,
        CancellationToken cancellationToken)
    {
        var usuario = await _usuariosService.ActualizarUsuarioAsync(id, request, cancellationToken);
        return Ok(usuario);
    }
}
