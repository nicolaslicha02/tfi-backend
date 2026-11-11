namespace GestionReservas.Application.Features.Catalogos.DTOs;

/// <summary>
/// DTO de departamento/secretaría para listas desplegables y consultas de catálogo.
/// </summary>
public record DepartamentoDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public int CantidadEspacios { get; init; }
}
