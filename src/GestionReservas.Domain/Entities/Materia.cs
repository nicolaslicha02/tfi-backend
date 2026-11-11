namespace GestionReservas.Domain.Entities;

/// <summary>
/// Representa una asignatura o materia dictada en la facultad.
/// </summary>
public class Materia
{
    public int Id { get; set; }

    /// <summary>
    /// Código de la asignatura según el plan de estudios (ej: "K2051").
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la asignatura.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    public int DepartamentoId { get; set; }
    public Departamento Departamento { get; set; } = null!;

    // Relaciones
    public ICollection<Cursada> Cursadas { get; set; } = new List<Cursada>();
}
