namespace GestionReservas.Application.Features.Catalogos.DTOs;

/// <summary>
/// Solicitud para registrar una nueva tipología o clasificación de espacio físico.
/// </summary>
public class CrearTipoEspacioRequestDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
