namespace GestionReservas.Application.Features.Espacios.DTOs;

/// <summary>
/// DTO de entrada para asociar o actualizar equipamiento en un espacio físico.
/// </summary>
public class EspacioEquipamientoInputDto
{
    public int EquipamientoId { get; set; }
    public int Cantidad { get; set; } = 1;
    public string? Especificacion { get; set; }
}
