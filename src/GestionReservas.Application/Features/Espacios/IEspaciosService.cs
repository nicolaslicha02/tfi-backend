using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Espacios.DTOs;

namespace GestionReservas.Application.Features.Espacios;

/// <summary>
/// Contrato del servicio principal para la administración de infraestructura de espacios físicos y cálculo de disponibilidad.
/// </summary>
public interface IEspaciosService
{
    /// <summary>
    /// Obtiene un listado paginado y filtrado de espacios físicos.
    /// </summary>
    Task<PagedResultDto<EspacioListadoDto>> GetEspaciosAsync(EspaciosQueryParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el detalle completo de un espacio físico por su identificador.
    /// </summary>
    Task<EspacioDetalleDto> GetEspacioByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra un nuevo espacio físico con su dotación de equipamiento.
    /// </summary>
    Task<EspacioDetalleDto> CrearEspacioAsync(CrearEspacioRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Modifica integralmente un espacio existente y su dotación de equipamiento.
    /// </summary>
    Task<EspacioDetalleDto> ActualizarEspacioAsync(int id, ActualizarEspacioRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Da de baja lógica a un espacio físico validando que no posea reservas futuras pendientes o aprobadas.
    /// </summary>
    Task EliminarEspacioAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta los espacios físicos disponibles para una fecha, franja horaria y criterios de capacidad y recursos.
    /// </summary>
    Task<IReadOnlyList<EspacioListadoDto>> GetEspaciosDisponiblesAsync(DisponibilidadEspacioQueryParameters parameters, CancellationToken cancellationToken = default);
}
