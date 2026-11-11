using GestionReservas.Domain.Enums;

namespace GestionReservas.Application.Features.Reservas.DTOs;

/// <summary>
/// Datos requeridos para crear una nueva solicitud de reserva de espacio.
/// </summary>
public class CrearReservaRequestDto
{
    public int EspacioId { get; set; }
    public DateTime FechaHoraInicio { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public TipoActividad TipoActividad { get; set; }
    public int AsistentesEstimados { get; set; }
    public List<ReservaEquipamientoInputDto> EquipamientoRequerido { get; set; } = new();
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}

/// <summary>
/// Datos requeridos para modificar una reserva existente (PUT).
/// </summary>
public class ActualizarReservaRequestDto
{
    public DateTime FechaHoraInicio { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public TipoActividad TipoActividad { get; set; }
    public int AsistentesEstimados { get; set; }
    public List<ReservaEquipamientoInputDto> EquipamientoRequerido { get; set; } = new();
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}

/// <summary>
/// Petición para cambiar el estado de una reserva ("aprobar", "rechazar", "cancelar").
/// </summary>
public class CambiarEstadoReservaRequestDto
{
    public string Accion { get; set; } = string.Empty;
    public string? Motivo { get; set; }
}
