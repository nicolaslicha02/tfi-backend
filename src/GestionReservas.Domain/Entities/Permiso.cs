namespace GestionReservas.Domain.Entities;

/// <summary>
/// Representa una acción atómica o privilegio del sistema.
/// </summary>
public class Permiso
{
    public int Id { get; set; }

    /// <summary>
    /// Código único del permiso (ej: espacios.ver, reservas.aprobar).
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Descripción funcional de la acción que autoriza este permiso.
    /// </summary>
    public string? Descripcion { get; set; }

    // Relaciones
    public ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
}
