namespace GestionReservas.Application.Common.Interfaces;

/// <summary>
/// Provee acceso a los datos de identidad y claims del usuario que ejecuta la petición HTTP actual.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Identificador numérico del usuario autenticado, o null si es anónimo.
    /// </summary>
    int? UsuarioId { get; }

    /// <summary>
    /// Dirección de email del usuario autenticado.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Lista de roles asignados en el token JWT.
    /// </summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Lista de permisos atómicos asignados en el token JWT.
    /// </summary>
    IReadOnlyList<string> Permisos { get; }

    /// <summary>
    /// Indica si la petición actual cuenta con un usuario autenticado.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Verifica si el usuario actual posee un permiso específico.
    /// </summary>
    bool HasPermission(string permiso);

    /// <summary>
    /// Verifica si el usuario actual pertenece a un rol específico.
    /// </summary>
    bool IsInRole(string rol);
}
