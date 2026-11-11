using GestionReservas.Application.Features.Auth.DTOs;

namespace GestionReservas.Application.Features.Auth;

/// <summary>
/// Contrato del servicio de autenticación y gestión de perfil de usuario.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Autentica al usuario contra la base de datos, valida credenciales y estado activo,
    /// emite el token JWT y registra la auditoría correspondiente.
    /// </summary>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el perfil consolidado del usuario autenticado actual, incluyendo roles, permisos y departamento.
    /// </summary>
    Task<CurrentUserProfileDto> GetCurrentUserProfileAsync(int usuarioId, CancellationToken cancellationToken = default);
}
