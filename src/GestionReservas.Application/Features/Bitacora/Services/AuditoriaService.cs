using FluentValidation;
using GestionReservas.Application.Common.Exceptions;
using GestionReservas.Application.Common.Helpers;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Bitacora;
using GestionReservas.Application.Features.Bitacora.DTOs;
using Microsoft.EntityFrameworkCore;
using ValidationException = GestionReservas.Application.Common.Exceptions.ValidationException;

namespace GestionReservas.Application.Features.Bitacora.Services;

/// <summary>Consulta de solo lectura de los eventos de auditoría.</summary>
public sealed class AuditoriaService : IAuditoriaService
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<AuditoriaQueryParameters> _validator;

    public AuditoriaService(IApplicationDbContext context, IValidator<AuditoriaQueryParameters> validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<PagedResultDto<AuditoriaDto>> GetAuditoriaAsync(
        AuditoriaQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var eventos = _context.Auditorias.AsNoTracking().AsQueryable();
        if (query.UsuarioId.HasValue)
        {
            eventos = eventos.Where(a => a.UsuarioId == query.UsuarioId.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.Accion))
        {
            var accion = query.Accion.Trim();
            eventos = eventos.Where(a => a.Accion == accion);
        }
        if (!string.IsNullOrWhiteSpace(query.Entidad))
        {
            var entidad = query.Entidad.Trim();
            eventos = eventos.Where(a => a.Entidad == entidad);
        }
        if (query.Desde.HasValue)
        {
            var desdeUtc = DateTimeZoneHelper.NormalizeToUtc(query.Desde.Value);
            eventos = eventos.Where(a => a.FechaHora >= desdeUtc);
        }
        if (query.Hasta.HasValue)
        {
            var hastaUtc = DateTimeZoneHelper.NormalizeToUtc(query.Hasta.Value);
            eventos = eventos.Where(a => a.FechaHora <= hastaUtc);
        }

        var total = await eventos.CountAsync(cancellationToken);
        var items = await eventos
            .OrderByDescending(a => a.FechaHora)
            .ThenByDescending(a => a.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AuditoriaDto
            {
                Id = a.Id,
                UsuarioId = a.UsuarioId,
                UsuarioEmail = a.UsuarioEmail ?? (a.Usuario == null ? null : a.Usuario.Email),
                UsuarioNombre = a.UsuarioNombre ?? (a.Usuario == null ? null : a.Usuario.NombreCompleto),
                Rol = a.Rol,
                FechaHora = a.FechaHora,
                Accion = a.Accion,
                Entidad = a.Entidad,
                EntidadId = a.EntidadId,
                Motivo = a.Motivo,
                Detalle = a.Detalle
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<AuditoriaDto>(items, total, query.Page, query.PageSize);
    }
}
