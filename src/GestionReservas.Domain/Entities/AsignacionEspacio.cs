namespace GestionReservas.Domain.Entities;

/// <summary>
/// Representa una asignación física recurrente u oficial para una comisión en el cronograma académico.
/// </summary>
public class AsignacionEspacio
{
    public int Id { get; set; }

    public int ComisionId { get; set; }
    public Comision Comision { get; set; } = null!;

    public int EspacioId { get; set; }
    public Espacio Espacio { get; set; } = null!;

    /// <summary>
    /// Día de la semana en el que se dicta la clase recurrente.
    /// </summary>
    public DayOfWeek DiaSemana { get; set; }

    /// <summary>
    /// Hora de inicio del bloque horario fijo.
    /// </summary>
    public TimeSpan HoraInicio { get; set; }

    /// <summary>
    /// Hora de fin del bloque horario fijo.
    /// </summary>
    public TimeSpan HoraFin { get; set; }
}
