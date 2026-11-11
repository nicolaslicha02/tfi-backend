namespace GestionReservas.Application.Features.Usuarios.DTOs;

/// <summary>
/// Solicitud de actualización parcial de un usuario existente.
/// Solo los campos especificados con valor serán modificados en el sistema.
/// </summary>
public class ActualizarUsuarioRequestDto
{
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Telefono { get; set; }
    public int? DepartamentoId { get; set; }
    public List<string>? Roles { get; set; }
    public bool? Activo { get; set; }
    public string? Password { get; set; }
}
