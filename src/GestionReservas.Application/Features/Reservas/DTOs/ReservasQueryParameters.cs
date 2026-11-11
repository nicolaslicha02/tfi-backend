using GestionReservas.Domain.Enums;

namespace GestionReservas.Application.Features.Reservas.DTOs;

/// <summary>
/// Parámetros de consulta y filtrado para el listado administrativo general de reservas.
/// </summary>
public class ReservasQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public EstadoReserva? Estado { get; set; }
    public int? EspacioId { get; set; }
    public int? DepartamentoId { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}
