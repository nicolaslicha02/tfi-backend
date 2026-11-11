namespace GestionReservas.Domain.Entities;

/// <summary>
/// Representa una comisión o grupo de cursada para una asignatura.
/// </summary>
public class Comision
{
    public int Id { get; set; }

    public int CursadaId { get; set; }
    public Cursada Cursada { get; set; } = null!;

    /// <summary>
    /// Código identificatorio de la comisión (ej: "K2051-N1").
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Turno de cursada (ej: "Mañana", "Tarde", "Noche").
    /// </summary>
    public string Turno { get; set; } = string.Empty;

    /// <summary>
    /// Cantidad inicial de alumnos inscriptos.
    /// </summary>
    public int CantidadInscriptos { get; set; }

    // Relaciones
    public ICollection<AsignacionEspacio> Asignaciones { get; set; } = new List<AsignacionEspacio>();
    public ICollection<HistorialMatricula> HistorialMatriculas { get; set; } = new List<HistorialMatricula>();
}
