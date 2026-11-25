namespace GestionReservas.Application.Features.Catalogos.DTOs;

/// <summary>Datos para registrar o actualizar un departamento.</summary>
public sealed class GuardarDepartamentoRequestDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
