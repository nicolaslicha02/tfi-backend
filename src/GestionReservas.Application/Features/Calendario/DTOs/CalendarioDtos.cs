using GestionReservas.Domain.Enums;

namespace GestionReservas.Application.Features.Calendario.DTOs;

/// <summary>
/// Parámetros de consulta para la agenda/calendario unificado de espacios físicos.
/// </summary>
public class CalendarioQueryParameters
{
    /// <summary>
    /// Inicio del rango de consulta (obligatorio). Se normaliza a UTC.
    /// </summary>
    public DateTime? Desde { get; set; }

    /// <summary>
    /// Fin del rango de consulta (obligatorio, mayor o igual a Desde, rango máximo 31 días). Se normaliza a UTC.
    /// </summary>
    public DateTime? Hasta { get; set; }

    /// <summary>
    /// Filtro opcional por identificador de espacio físico.
    /// </summary>
    public int? EspacioId { get; set; }

    /// <summary>
    /// Filtro opcional por edificio.
    /// </summary>
    public string? Edificio { get; set; }

    /// <summary>
    /// Filtro opcional por sector o ala.
    /// </summary>
    public string? Sector { get; set; }

    /// <summary>
    /// Filtro opcional por tipo de espacio físico.
    /// </summary>
    public int? TipoEspacioId { get; set; }

    /// <summary>
    /// Filtro opcional por departamento responsable del espacio.
    /// </summary>
    public int? DepartamentoId { get; set; }
}

/// <summary>
/// Representa un evento unificado dentro del calendario de ocupación (Reserva, Bloqueo o Cursada).
/// </summary>
public class EventoCalendarioDto
{
    /// <summary>
    /// Tipo de evento: "Reserva", "Bloqueo" o "Cursada".
    /// </summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>
    /// Identificador de la entidad de origen (ReservaId, BloqueoId o AsignacionEspacioId).
    /// </summary>
    public int ReferenciaId { get; set; }

    public int EspacioId { get; set; }
    public string EspacioCodigo { get; set; } = string.Empty;
    public string EspacioNombre { get; set; } = string.Empty;
    public string Edificio { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public int DepartamentoId { get; set; }
    public string Departamento { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora de inicio del evento en UTC.
    /// </summary>
    public DateTime Inicio { get; set; }

    /// <summary>
    /// Fecha y hora de fin del evento en UTC.
    /// </summary>
    public DateTime Fin { get; set; }

    /// <summary>
    /// Estado de la reserva (solo para eventos de tipo "Reserva"; nulo en "Bloqueo" y "Cursada").
    /// </summary>
    public EstadoReserva? Estado { get; set; }

    /// <summary>
    /// Título descriptivo del evento aplicando reglas de privacidad según el rol del usuario.
    /// </summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>
    /// Identificador del solicitante (oculto/null en reservas ajenas para el rol Solicitante).
    /// </summary>
    public int? SolicitanteId { get; set; }

    /// <summary>
    /// Nombre del solicitante (oculto/null en reservas ajenas para el rol Solicitante).
    /// </summary>
    public string? Solicitante { get; set; }

    /// <summary>
    /// Tipo de actividad de la reserva (oculto/null en reservas ajenas para el rol Solicitante).
    /// </summary>
    public TipoActividad? TipoActividad { get; set; }

    /// <summary>
    /// Motivo de la reserva o del bloqueo (oculto/null según reglas de privacidad).
    /// </summary>
    public string? Motivo { get; set; }

    /// <summary>
    /// Observaciones adicionales de la reserva (ocultas/null en reservas ajenas para el rol Solicitante).
    /// </summary>
    public string? Observaciones { get; set; }
}
