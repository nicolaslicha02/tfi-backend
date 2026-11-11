namespace GestionReservas.Application.Features.Catalogos.DTOs;

/// <summary>
/// DTO de equipamiento tecnológico para listas desplegables y catálogos de recursos.
/// </summary>
public record EquipamientoDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
}
