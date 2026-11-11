using GestionReservas.Domain.Enums;

namespace GestionReservas.Application.Features.Espacios.DTOs;

/// <summary>
/// Solicitud de creación de un nuevo espacio físico.
/// </summary>
public class CrearEspacioRequestDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int TipoEspacioId { get; set; }
    public string Edificio { get; set; } = string.Empty;
    public int Piso { get; set; }
    public string Sector { get; set; } = string.Empty;
    public int CapacidadMaxima { get; set; }
    public int DepartamentoId { get; set; }
    public EstadoEspacio Estado { get; set; } = EstadoEspacio.Disponible;
    public List<EspacioEquipamientoInputDto> Equipamientos { get; set; } = new();
}
