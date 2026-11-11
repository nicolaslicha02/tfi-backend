namespace GestionReservas.Application.Features.Bloqueos.DTOs;

/// <summary>
/// Respuesta tras la creación exitosa de un bloqueo, incluyendo las reservas que fueron canceladas si correspondía.
/// </summary>
public class CrearBloqueoResponseDto
{
    public int Id { get; set; }
    public int EspacioId { get; set; }
    public string EspacioCodigo { get; set; } = string.Empty;
    public string EspacioNombre { get; set; } = string.Empty;
    public DateTime FechaHoraDesde { get; set; }
    public DateTime FechaHoraHasta { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public int CreadoPorUsuarioId { get; set; }
    public string CreadoPorUsuarioNombre { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public List<ReservaAfectadaDto> ReservasCanceladas { get; set; } = new();
}
