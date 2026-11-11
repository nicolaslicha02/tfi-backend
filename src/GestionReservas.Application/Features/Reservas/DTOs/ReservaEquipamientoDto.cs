namespace GestionReservas.Application.Features.Reservas.DTOs;

/// <summary>
/// Elemento de equipamiento solicitado al crear o modificar una reserva.
/// </summary>
public class ReservaEquipamientoInputDto
{
    public int EquipamientoId { get; set; }
    public int Cantidad { get; set; } = 1;
    public string? Observaciones { get; set; }
}

/// <summary>
/// Detalle de un equipamiento requerido dentro de una reserva.
/// </summary>
public class ReservaEquipamientoDetalleDto
{
    public int EquipamientoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public string? Observaciones { get; set; }
}
