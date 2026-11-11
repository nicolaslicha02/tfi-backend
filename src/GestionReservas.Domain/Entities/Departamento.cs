namespace GestionReservas.Domain.Entities;

/// <summary>
/// Representa una unidad académica, secretaría o departamento responsable de administrar espacios.
/// </summary>
public class Departamento
{
    public int Id { get; set; }

    /// <summary>
    /// Nombre oficial del departamento o secretaría (ej: Departamento de Sistemas e Informática).
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Detalle adicional o función del departamento.
    /// </summary>
    public string? Descripcion { get; set; }

    // Relaciones
    public ICollection<Espacio> Espacios { get; set; } = new List<Espacio>();
    public ICollection<Materia> Materias { get; set; } = new List<Materia>();
}
