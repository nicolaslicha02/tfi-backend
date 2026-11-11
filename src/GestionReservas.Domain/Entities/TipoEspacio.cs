namespace GestionReservas.Domain.Entities;

/// <summary>
/// Clasificación funcional del espacio (ej: Aula Teórica, Laboratorio de Computación, Auditorio, Sala de Reuniones).
/// </summary>
public class TipoEspacio
{
    public int Id { get; set; }

    /// <summary>
    /// Denominación de la tipología del espacio.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Descripción o criterios de uso del tipo de espacio.
    /// </summary>
    public string? Descripcion { get; set; }

    // Relaciones
    public ICollection<Espacio> Espacios { get; set; } = new List<Espacio>();
}
