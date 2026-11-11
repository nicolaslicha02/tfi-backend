namespace GestionReservas.Application.Features.Usuarios.DTOs;

/// <summary>
/// Solicitud de registro de un nuevo usuario por parte de un administrador.
/// </summary>
public class CrearUsuarioRequestDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Password { get; set; } = string.Empty;
    public int? DepartamentoId { get; set; }
    public List<string> Roles { get; set; } = new();
}
