namespace GestionReservas.Application.Features.Espacios.DTOs;

/// <summary>
/// DTO con el detalle de un equipamiento asignado a un espacio físico.
/// </summary>
public class EspacioEquipamientoDetalleDto
{
    public int EquipamientoId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public int Cantidad { get; init; }
    public string? Especificacion { get; init; }
}
