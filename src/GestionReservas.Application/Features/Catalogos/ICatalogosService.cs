using GestionReservas.Application.Features.Catalogos.DTOs;

namespace GestionReservas.Application.Features.Catalogos;

/// <summary>
/// Contrato del servicio de consulta de catálogos maestros del sistema.
/// </summary>
public interface ICatalogosService
{
    /// <summary>
    /// Obtiene la lista de departamentos y secretarías institucionales.
    /// </summary>
    Task<IReadOnlyList<DepartamentoDto>> GetDepartamentosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el catálogo de equipamientos disponibles para espacios físicos.
    /// </summary>
    Task<IReadOnlyList<EquipamientoDto>> GetEquipamientosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene los tipos y clasificaciones funcionales de espacios físicos.
    /// </summary>
    Task<IReadOnlyList<TipoEspacioDto>> GetTiposEspacioAsync(CancellationToken cancellationToken = default);
}
