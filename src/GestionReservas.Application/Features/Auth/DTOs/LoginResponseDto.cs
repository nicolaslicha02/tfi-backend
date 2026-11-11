namespace GestionReservas.Application.Features.Auth.DTOs;

/// <summary>
/// Respuesta exitosa de autenticación con token JWT y datos básicos del usuario.
/// </summary>
public record LoginResponseDto
{
    /// <summary>
    /// Token JWT Bearer generado para autorizar las solicitudes subsiguientes.
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Fecha y hora UTC en la que expira el token.
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Datos de perfil resumidos del usuario autenticado.
    /// </summary>
    public UsuarioInfoDto Usuario { get; init; } = null!;
}

/// <summary>
/// Resumen de datos del usuario devuelto tras el inicio de sesión.
/// </summary>
public record UsuarioInfoDto
{
    public int Id { get; init; }
    public string NombreCompleto { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}
