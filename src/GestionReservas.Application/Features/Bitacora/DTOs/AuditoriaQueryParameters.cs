namespace GestionReservas.Application.Features.Bitacora.DTOs;

/// <summary>Filtros y paginación para la consulta de la bitácora.</summary>
public sealed class AuditoriaQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? UsuarioId { get; set; }
    public string? Accion { get; set; }
    public string? Entidad { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}
