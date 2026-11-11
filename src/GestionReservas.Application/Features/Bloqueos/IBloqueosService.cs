using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Bloqueos.DTOs;

namespace GestionReservas.Application.Features.Bloqueos;

/// <summary>
/// Contrato del servicio de gestión de bloqueos temporales de espacios físicos.
/// </summary>
public interface IBloqueosService
{
    /// <summary>
    /// Crea un nuevo bloqueo temporal sobre un espacio físico.
    /// Si existen reservas afectadas y cancelarReservasAfectadas = false, arroja ConflictException (409) con el listado.
    /// Si cancelarReservasAfectadas = true, cancela en una sola transacción todas las reservas afectadas,
    /// audita cada cancelación y notifica a los solicitantes.
    /// </summary>
    Task<CrearBloqueoResponseDto> CrearBloqueoAsync(CrearBloqueoRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve el listado paginado de bloqueos según filtros y alcance departamental del usuario.
    /// </summary>
    Task<PagedResultDto<BloqueoListadoDto>> GetBloqueosAsync(BloqueosQueryParameters queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Levanta o elimina un bloqueo existente y registra la acción en la bitácora de auditoría.
    /// Nota: Las reservas canceladas durante la creación del bloqueo NO son restauradas.
    /// </summary>
    Task EliminarBloqueoAsync(int id, CancellationToken cancellationToken = default);
}
