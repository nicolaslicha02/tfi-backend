namespace GestionReservas.Application.Features.Espacios.DTOs;

/// <summary>
/// Parámetros para la búsqueda y filtrado de espacios disponibles en un rango horario.
/// La fecha y horas se interpretan en la zona horaria de la facultad (America/Argentina/Buenos_Aires).
/// </summary>
public class DisponibilidadEspacioQueryParameters
{
    /// <summary>
    /// Fecha de la actividad (formato YYYY-MM-DD). Obligatoria.
    /// </summary>
    public DateOnly? Fecha { get; set; }

    /// <summary>
    /// Hora de inicio de la actividad (formato HH:mm). Obligatoria.
    /// </summary>
    public TimeOnly? HoraInicio { get; set; }

    /// <summary>
    /// Hora de finalización de la actividad (formato HH:mm). Obligatoria (debe ser mayor a HoraInicio).
    /// </summary>
    public TimeOnly? HoraFin { get; set; }

    /// <summary>
    /// Cantidad mínima de asistentes requerida.
    /// </summary>
    public int? CapacidadMinima { get; set; }

    /// <summary>
    /// Identificadores de equipamiento que el espacio DEBE poseer obligatoriamente (todos requeridos).
    /// </summary>
    public List<int>? EquipamientoIds { get; set; }

    /// <summary>
    /// Filtro por tipo o categoría de espacio.
    /// </summary>
    public int? TipoEspacioId { get; set; }

    /// <summary>
    /// Filtro por edificio.
    /// </summary>
    public string? Edificio { get; set; }

    /// <summary>
    /// Filtro por sector o ala.
    /// </summary>
    public string? Sector { get; set; }

    /// <summary>
    /// Filtro por departamento responsable del espacio.
    /// </summary>
    public int? DepartamentoId { get; set; }
}
