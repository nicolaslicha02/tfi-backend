namespace GestionReservas.Domain.Entities;

/// <summary>
/// Representa un elemento de equipamiento o recurso tecnológico disponible para asociar a los espacios físicos.
/// </summary>
public class Equipamiento
{
    public int Id { get; set; }

    /// <summary>
    /// Denominación del recurso (ej: "Proyector Láser Full HD", "Pizarra Digital").
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Descripción técnica o especificaciones del recurso.
    /// </summary>
    public string? Descripcion { get; set; }

    // Relaciones
    public ICollection<EspacioEquipamiento> EspaciosEquipamiento { get; set; } = new List<EspacioEquipamiento>();
    public ICollection<ReservaEquipamiento> ReservasEquipamiento { get; set; } = new List<ReservaEquipamiento>();
}
