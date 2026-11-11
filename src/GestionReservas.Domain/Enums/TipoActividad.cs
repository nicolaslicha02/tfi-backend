using System.Text.Json.Serialization;

namespace GestionReservas.Domain.Enums;

/// <summary>
/// Tipos de actividad para las cuales se puede solicitar la reserva de un espacio.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TipoActividad
{
    /// <summary>
    /// Dictado de clase habitual, consulta o recuperación.
    /// </summary>
    Clase = 1,

    /// <summary>
    /// Examen parcial, recuperatorio o mesa final.
    /// </summary>
    Examen = 2,

    /// <summary>
    /// Reunión de cátedra, departamento o comisión académica.
    /// </summary>
    Reunion = 3,

    /// <summary>
    /// Jornada, congreso, acto o ceremonia institucional.
    /// </summary>
    ActividadInstitucional = 4,

    /// <summary>
    /// Otra actividad académica o administrativa especificada en el motivo.
    /// </summary>
    Otra = 5
}
