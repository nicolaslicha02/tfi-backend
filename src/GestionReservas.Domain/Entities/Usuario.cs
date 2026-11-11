namespace GestionReservas.Domain.Entities;

/// <summary>
/// Representa a un usuario del sistema (docente, solicitante, responsable de espacio, secretaría o administrador).
/// </summary>
public class Usuario
{
    public int Id { get; set; }

    /// <summary>
    /// Nombre y apellido completo del usuario.
    /// </summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>
    /// Dirección de correo electrónico institucional (única).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hash seguro de la contraseña.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Número de teléfono o interno de contacto opcional.
    /// </summary>
    public string? Telefono { get; set; }

    /// <summary>
    /// Indica si el usuario se encuentra habilitado para operar en el sistema.
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>
    /// Fecha y hora UTC de registro del usuario.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Relaciones de seguridad
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();

    // Relaciones operativas
    public ICollection<Reserva> ReservasSolicitadas { get; set; } = new List<Reserva>();
    public ICollection<Reserva> ReservasResueltas { get; set; } = new List<Reserva>();
    public ICollection<Bloqueo> BloqueosCreados { get; set; } = new List<Bloqueo>();
    public ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
    public ICollection<Auditoria> Auditorias { get; set; } = new List<Auditoria>();
}
