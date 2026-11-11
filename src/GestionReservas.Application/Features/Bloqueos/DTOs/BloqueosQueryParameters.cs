namespace GestionReservas.Application.Features.Bloqueos.DTOs;

/// <summary>
/// Parámetros de consulta y filtrado para el listado paginado de bloqueos.
/// </summary>
public class BloqueosQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? EspacioId { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public bool? Vigentes { get; set; }
}
