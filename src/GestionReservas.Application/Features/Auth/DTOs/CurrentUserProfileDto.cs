namespace GestionReservas.Application.Features.Auth.DTOs;

/// <summary>
/// Perfil completo del usuario autenticado devuelto por el endpoint GET /api/auth/me.
/// </summary>
public record CurrentUserProfileDto
{
    public int Id { get; init; }
    public string NombreCompleto { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Telefono { get; init; }
    public bool Activo { get; init; }
    public DateTime FechaCreacion { get; init; }

    /// <summary>
    /// Roles asignados al usuario.
    /// </summary>
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Permisos atómicos efectivos consolidados de todos sus roles.
    /// </summary>
    public IReadOnlyList<string> Permisos { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Información del departamento académico asociado o a cargo, si aplica.
    /// </summary>
    public DepartamentoSimpleDto? Departamento { get; init; }
}

/// <summary>
/// Resumen del departamento para incluir en el perfil del usuario.
/// </summary>
public record DepartamentoSimpleDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
}
