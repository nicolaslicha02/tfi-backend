namespace GestionReservas.Domain.Entities;

/// <summary>
/// Representa la instancia de dictado de una materia en un ciclo lectivo y período específico.
/// </summary>
public class Cursada
{
    public int Id { get; set; }

    public int MateriaId { get; set; }
    public Materia Materia { get; set; } = null!;

    public int CicloLectivoId { get; set; }
    public CicloLectivo CicloLectivo { get; set; } = null!;

    /// <summary>
    /// Período lectivo (ej: "1er Cuatrimestre", "2do Cuatrimestre", "Anual").
    /// </summary>
    public string Periodo { get; set; } = string.Empty;

    // Relaciones
    public ICollection<Comision> Comisiones { get; set; } = new List<Comision>();
}
