using GestionReservas.Domain.Enums;

namespace GestionReservas.Application.Features.Reservas.DTOs;

/// <summary>
/// Entrada en el historial de cambios de estado de una reserva (extraído de Auditoría).
/// </summary>
public class HistorialEstadoReservaDto
{
    public int Id { get; set; }
    public DateTime FechaHora { get; set; }
    public string Accion { get; set; } = string.Empty;
    public int? UsuarioId { get; set; }
    public string? UsuarioNombre { get; set; }
    public string? Motivo { get; set; }
    public string? Detalle { get; set; }
}

/// <summary>
/// Representación resumida de una reserva para listados.
/// </summary>
public class ReservaListadoDto
{
    public int Id { get; set; }
    public int EspacioId { get; set; }
    public string EspacioCodigo { get; set; } = string.Empty;
    public string EspacioNombre { get; set; } = string.Empty;
    public int DepartamentoId { get; set; }
    public string DepartamentoNombre { get; set; } = string.Empty;
    public int SolicitanteId { get; set; }
    public string SolicitanteNombre { get; set; } = string.Empty;
    public string SolicitanteEmail { get; set; } = string.Empty;
    public DateTime FechaHoraInicio { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public TipoActividad TipoActividad { get; set; }
    public int AsistentesEstimados { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public EstadoReserva Estado { get; set; }
    public string? MotivoRechazo { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaResolucion { get; set; }
    public int? ResueltoPorUsuarioId { get; set; }
    public string? ResueltoPorNombre { get; set; }
}

/// <summary>
/// Detalle completo de una reserva incluyendo su equipamiento requerido e historial de cambios de estado.
/// </summary>
public class ReservaDetalleDto : ReservaListadoDto
{
    public List<ReservaEquipamientoDetalleDto> EquipamientoRequerido { get; set; } = new();
    public List<HistorialEstadoReservaDto> HistorialCambios { get; set; } = new();
}

/// <summary>
/// Parámetros de filtrado y paginación para consultar reservas propias (/api/mis-reservas).
/// </summary>
public class MisReservasQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public EstadoReserva? Estado { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}
