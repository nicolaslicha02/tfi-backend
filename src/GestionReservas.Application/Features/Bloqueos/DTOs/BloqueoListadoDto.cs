namespace GestionReservas.Application.Features.Bloqueos.DTOs;

/// <summary>
/// DTO de representación de un bloqueo para listados y consultas.
/// </summary>
public class BloqueoListadoDto
{
    public int Id { get; set; }
    public int EspacioId { get; set; }
    public string EspacioCodigo { get; set; } = string.Empty;
    public string EspacioNombre { get; set; } = string.Empty;
    public int DepartamentoId { get; set; }
    public string DepartamentoNombre { get; set; } = string.Empty;
    public DateTime FechaHoraDesde { get; set; }
    public DateTime FechaHoraHasta { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public int CreadoPorUsuarioId { get; set; }
    public string CreadoPorUsuarioNombre { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public bool Vigente => FechaHoraHasta > DateTime.UtcNow;
}
