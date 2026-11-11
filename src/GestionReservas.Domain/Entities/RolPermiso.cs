namespace GestionReservas.Domain.Entities;

/// <summary>
/// Tabla intermedia para la relación muchos a muchos entre Rol y Permiso.
/// </summary>
public class RolPermiso
{
    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;

    public int PermisoId { get; set; }
    public Permiso Permiso { get; set; } = null!;
}
