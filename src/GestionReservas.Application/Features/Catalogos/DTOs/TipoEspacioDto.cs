namespace GestionReservas.Application.Features.Catalogos.DTOs;

/// <summary>
/// DTO de tipo de espacio para listas desplegables y catálogos de tipologías.
/// </summary>
public record TipoEspacioDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
}
