namespace GestionReservas.Application.Features.Catalogos.DTOs;

/// <summary>
/// Solicitud para registrar un nuevo equipamiento en el catálogo tecnológico.
/// </summary>
public class CrearEquipamientoRequestDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
