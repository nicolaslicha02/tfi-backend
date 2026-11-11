namespace GestionReservas.Application.Features.Catalogos.DTOs;

/// <summary>
/// DTO para la bitácora institucional de auditoría y trazabilidad.
/// </summary>
public record AuditoriaDto
{
    public int Id { get; init; }
    public int? UsuarioId { get; init; }
    public string? UsuarioEmail { get; init; }
    public string? UsuarioNombre { get; init; }
    public DateTime FechaHora { get; init; }
    public string Accion { get; init; } = string.Empty;
    public string Entidad { get; init; } = string.Empty;
    public string? EntidadId { get; init; }
    public string? Motivo { get; init; }
    public string? Detalle { get; init; }
}
