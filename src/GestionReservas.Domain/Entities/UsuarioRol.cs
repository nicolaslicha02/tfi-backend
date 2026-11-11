namespace GestionReservas.Domain.Entities;

/// <summary>
/// Tabla intermedia para la relación muchos a muchos entre Usuario y Rol.
/// </summary>
public class UsuarioRol
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;

    /// <summary>
    /// Fecha y hora en la que se le asignó el rol al usuario.
    /// </summary>
    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
}
