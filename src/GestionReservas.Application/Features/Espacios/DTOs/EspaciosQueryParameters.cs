using GestionReservas.Domain.Enums;

namespace GestionReservas.Application.Features.Espacios.DTOs;

/// <summary>
/// Parámetros de consulta y filtrado para el listado paginado de espacios.
/// </summary>
public class EspaciosQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public int? TipoEspacioId { get; set; }
    public string? Edificio { get; set; }
    public int? Piso { get; set; }
    public string? Sector { get; set; }
    public int? CapacidadMinima { get; set; }
    public int? DepartamentoId { get; set; }
    public EstadoEspacio? Estado { get; set; }
    public List<int>? EquipamientoIds { get; set; }
    public bool IncluirBajas { get; set; } = false;
}
