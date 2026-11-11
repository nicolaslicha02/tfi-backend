using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Features.Catalogos.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GestionReservas.Application.Features.Catalogos.Services;

/// <summary>
/// Implementación del servicio de consulta de catálogos maestros.
/// </summary>
public class CatalogosService : ICatalogosService
{
    private readonly IApplicationDbContext _context;

    public CatalogosService(IApplicationDbContext _context)
    {
        this._context = _context;
    }

    public async Task<IReadOnlyList<DepartamentoDto>> GetDepartamentosAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Departamentos
            .AsNoTracking()
            .OrderBy(d => d.Nombre)
            .Select(d => new DepartamentoDto
            {
                Id = d.Id,
                Nombre = d.Nombre,
                Descripcion = d.Descripcion
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EquipamientoDto>> GetEquipamientosAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Equipamientos
            .AsNoTracking()
            .OrderBy(e => e.Nombre)
            .Select(e => new EquipamientoDto
            {
                Id = e.Id,
                Nombre = e.Nombre,
                Descripcion = e.Descripcion
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TipoEspacioDto>> GetTiposEspacioAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TiposEspacio
            .AsNoTracking()
            .OrderBy(t => t.Nombre)
            .Select(t => new TipoEspacioDto
            {
                Id = t.Id,
                Nombre = t.Nombre,
                Descripcion = t.Descripcion
            })
            .ToListAsync(cancellationToken);
    }
}
