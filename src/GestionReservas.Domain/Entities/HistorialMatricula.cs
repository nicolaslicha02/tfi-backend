namespace GestionReservas.Domain.Entities;

/// <summary>
/// Registra las mediciones temporales de cantidad de alumnos presentes en una comisión
/// para analizar desgranamiento y optimización de espacios físicos.
/// </summary>
public class HistorialMatricula
{
    public int Id { get; set; }

    public int ComisionId { get; set; }
    public Comision Comision { get; set; } = null!;

    /// <summary>
    /// Fecha de la medición o relevamiento.
    /// </summary>
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cantidad real de alumnos presentes en la fecha de registro.
    /// </summary>
    public int CantidadAlumnosPresentes { get; set; }

    /// <summary>
    /// Observaciones o notas del docente o relevador.
    /// </summary>
    public string? Observaciones { get; set; }
}
