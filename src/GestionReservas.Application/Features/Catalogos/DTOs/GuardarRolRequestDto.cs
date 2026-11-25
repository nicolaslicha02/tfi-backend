namespace GestionReservas.Application.Features.Catalogos.DTOs;

/// <summary>Datos editables del catálogo de roles.</summary>
public sealed class GuardarRolRequestDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
