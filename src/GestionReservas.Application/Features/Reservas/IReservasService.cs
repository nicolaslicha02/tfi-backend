using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Reservas.DTOs;
using GestionReservas.Domain.Enums;

namespace GestionReservas.Application.Features.Reservas;

/// <summary>
/// Contrato del servicio de negocio para la gestión integral de reservas de espacios.
/// </summary>
public interface IReservasService
{
    /// <summary>
    /// Crea una nueva solicitud de reserva validando disponibilidad, capacidad, equipamiento y bloqueos.
    /// Si quien solicita administra el departamento del espacio (Responsable del depto, Secretaría o Admin),
    /// se auto-aprueba con control de concurrencia y trazabilidad en auditoría.
    /// </summary>
    Task<ReservaDetalleDto> CreateReservaAsync(CrearReservaRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el listado paginado de reservas pertenecientes al usuario autenticado (/api/mis-reservas).
    /// </summary>
    Task<PagedResultDto<ReservaListadoDto>> GetMisReservasAsync(MisReservasQueryParameters queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene las reservas filtradas por estado (PendienteAprobacion, Cancelada o Rechazada) según alcance departamental:
    /// Administrador y Secretaría Académica ven todas; Responsable de Espacio solo las de su departamento.
    /// </summary>
    Task<IReadOnlyList<ReservaListadoDto>> GetReservasPorEstadoAsync(EstadoReserva estado, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el detalle completo de una reserva junto con su historial de cambios de estado leído de Auditoría.
    /// Solo accesible para el solicitante, el responsable del espacio, Secretaría Académica o Administrador.
    /// </summary>
    Task<ReservaDetalleDto> GetReservaByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta un cambio de estado sobre una reserva ("aprobar", "rechazar", "cancelar") aplicando la máquina de estados,
    /// reglas de permisos departamentales, control de concurrencia pesimista/optimista y notificaciones.
    /// </summary>
    Task<ReservaDetalleDto> CambiarEstadoReservaAsync(int id, CambiarEstadoReservaRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Modifica los datos de una reserva existente (PUT). Si estaba Aprobada, vuelve a PendienteAprobacion y revalida todo.
    /// </summary>
    Task<ReservaDetalleDto> UpdateReservaAsync(int id, ActualizarReservaRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el listado administrativo general de reservas con paginación y filtros opcionales,
    /// respetando el alcance departamental (Admin/Secretaría ven todas; Responsable solo su departamento).
    /// </summary>
    Task<PagedResultDto<ReservaListadoDto>> GetReservasAdministrativasAsync(ReservasQueryParameters queryParams, CancellationToken cancellationToken = default);
}

