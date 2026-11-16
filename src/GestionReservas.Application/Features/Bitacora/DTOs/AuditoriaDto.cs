namespace GestionReservas.Application.Features.Bitacora.DTOs;

/// <summary>Representación de solo lectura de un evento de la bitácora.</summary>
public record AuditoriaDto
{
    public int Id { get; init; }
    public int? UsuarioId { get; init; }
    public string? UsuarioEmail { get; init; }
    public string? UsuarioNombre { get; init; }
    public string? Rol { get; init; }
    public DateTime FechaHora { get; init; }
    public string Accion { get; init; } = string.Empty;
    public string Entidad { get; init; } = string.Empty;
    public string? EntidadId { get; init; }
    public string? Motivo { get; init; }
    public string? Detalle { get; init; }
}
