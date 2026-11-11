namespace GestionReservas.Application.Features.Auth.DTOs;

/// <summary>
/// Modelo de entrada para la autenticación de usuarios.
/// </summary>
public record LoginRequestDto
{
    /// <summary>
    /// Correo electrónico institucional registrado.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Contraseña en texto plano.
    /// </summary>
    public string Password { get; init; } = string.Empty;
}
