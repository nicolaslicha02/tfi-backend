namespace GestionReservas.Application.Features.Catalogos.DTOs;

/// <summary>
/// DTO de representación de un rol de usuario del sistema.
/// </summary>
public class RolDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
