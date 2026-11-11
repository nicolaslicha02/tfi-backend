namespace GestionReservas.Domain.Entities;

/// <summary>
/// Equipamiento específico requerido por una solicitud de reserva.
/// </summary>
public class ReservaEquipamiento
{
    public int ReservaId { get; set; }
    public Reserva Reserva { get; set; } = null!;

    public int EquipamientoId { get; set; }
    public Equipamiento Equipamiento { get; set; } = null!;

    /// <summary>
    /// Cantidad de unidades requeridas para la actividad.
    /// </summary>
    public int CantidadRequerida { get; set; } = 1;

    /// <summary>
    /// Observaciones o requerimiento técnico puntual (ej: cables adicionales, adaptador).
    /// </summary>
    public string? Observaciones { get; set; }
}
