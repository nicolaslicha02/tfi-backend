namespace GestionReservas.Application.Features.Usuarios.DTOs;

/// <summary>
/// DTO resumido para listados paginados de usuarios en administración.
/// </summary>
public class UsuarioListadoDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Apellido { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Telefono { get; init; }
    public bool Activo { get; init; }
    public int? DepartamentoId { get; init; }
    public string? DepartamentoNombre { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public DateTime FechaAlta { get; init; }
}
