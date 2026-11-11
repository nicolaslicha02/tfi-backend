using GestionReservas.Domain.Enums;

namespace GestionReservas.Application.Features.Espacios.DTOs;

/// <summary>
/// DTO resumido para listados de espacios físicos con soporte de filtros y paginación.
/// </summary>
public class EspacioListadoDto
{
    public int Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public int TipoEspacioId { get; init; }
    public string TipoEspacioNombre { get; init; } = string.Empty;
    public string Edificio { get; init; } = string.Empty;
    public int Piso { get; init; }
    public string Sector { get; init; } = string.Empty;
    public int CapacidadMaxima { get; init; }
    public int DepartamentoId { get; init; }
    public string DepartamentoNombre { get; init; } = string.Empty;
    public EstadoEspacio Estado { get; init; }
    public bool EsBaja { get; init; }
    public IReadOnlyList<EspacioEquipamientoDetalleDto> Equipamientos { get; init; } = Array.Empty<EspacioEquipamientoDetalleDto>();
}
