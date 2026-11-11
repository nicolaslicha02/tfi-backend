using GestionReservas.Application.Common.Exceptions;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Features.Auth;
using GestionReservas.Application.Features.Auth.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionReservas.Api.Controllers;

/// <summary>
/// Controlador de autenticación y consulta de perfil de usuario.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IAuthService _authService, ICurrentUserService currentUserService)
    {
        this._authService = _authService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Inicia sesión en el sistema con credenciales de usuario y emite un token JWT con sus roles y permisos.
    /// </summary>
    /// <param name="request">Credenciales de acceso (email y contraseña).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Token JWT, fecha de expiración y datos básicos del usuario autenticado.</returns>
    /// <response code="200">Inicio de sesión exitoso.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="401">Credenciales inválidas o cuenta de usuario inhabilitada.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var response = await _authService.LoginAsync(request, ipAddress, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Devuelve el perfil completo, roles, permisos y departamento del usuario autenticado actual.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Información consolidada de identidad del usuario.</returns>
    /// <response code="200">Datos del usuario autenticado.</response>
    /// <response code="401">No autenticado o token JWT inválido/expirado.</response>
    /// <response code="404">Usuario no encontrado.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentUserProfileDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var usuarioId = _currentUserService.UsuarioId;
        if (!usuarioId.HasValue)
        {
            throw new UnauthorizedException("No se encontró el identificador del usuario en el token.");
        }

        var profile = await _authService.GetCurrentUserProfileAsync(usuarioId.Value, cancellationToken);
        return Ok(profile);
    }
}
