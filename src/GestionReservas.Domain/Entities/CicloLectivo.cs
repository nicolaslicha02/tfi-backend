namespace GestionReservas.Domain.Entities;

/// <summary>
/// Representa un año académico o período lectivo de la facultad.
/// </summary>
public class CicloLectivo
{
    public int Id { get; set; }

    /// <summary>
    /// Año del ciclo lectivo (ej: 2026).
    /// </summary>
    public int Anio { get; set; }

    /// <summary>
    /// Denominación del ciclo lectivo (ej: "Ciclo Lectivo 2026").
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    public bool Activo { get; set; } = true;

    // Relaciones
    public ICollection<Cursada> Cursadas { get; set; } = new List<Cursada>();
}
