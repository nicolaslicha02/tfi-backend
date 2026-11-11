namespace GestionReservas.Domain.Entities;

/// <summary>
/// Representa un rol dentro del esquema de control de acceso basado en roles (RBAC).
/// </summary>
public class Rol
{
    public int Id { get; set; }

    /// <summary>
    /// Nombre identificador del rol (ej: Solicitante, ResponsableEspacio, SecretariaAcademica, Administrador).
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Descripción del alcance y responsabilidades del rol.
    /// </summary>
    public string? Descripcion { get; set; }

    // Relaciones
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
    public ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
}
