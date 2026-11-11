using GestionReservas.Application.Common.Exceptions;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Features.Notificaciones.DTOs;
using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionReservas.Application.Features.Notificaciones.Services;

/// <summary>
/// Implementación del servicio de notificaciones para usuarios.
/// </summary>
public class NotificacionesService : INotificacionesService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public NotificacionesService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<NotificacionDto>> GetMisNotificacionesAsync(bool? leida = null, CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId
            ?? throw new UnauthorizedException("Usuario no autenticado.");

        var query = _context.Notificaciones
            .AsNoTracking()
            .Where(n => n.UsuarioId == usuarioId);

        if (leida.HasValue)
        {
            query = query.Where(n => n.Leida == leida.Value);
        }

        return await query
            .OrderByDescending(n => n.Fecha)
            .ThenByDescending(n => n.Id)
            .Select(n => new NotificacionDto
            {
                Id = n.Id,
                Mensaje = n.Mensaje,
                Leida = n.Leida,
                Fecha = n.Fecha
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<NotificacionDto> MarcarComoLeidaAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId
            ?? throw new UnauthorizedException("Usuario no autenticado.");

        var notificacion = await _context.Notificaciones
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        if (notificacion == null)
        {
            throw new NotFoundException($"No se encontró la notificación con identificador {id}.");
        }

        if (notificacion.UsuarioId != usuarioId)
        {
            throw new ForbiddenException("No tiene permiso para acceder ni modificar notificaciones de otro usuario.");
        }

        if (!notificacion.Leida)
        {
            notificacion.Leida = true;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new NotificacionDto
        {
            Id = notificacion.Id,
            Mensaje = notificacion.Mensaje,
            Leida = notificacion.Leida,
            Fecha = notificacion.Fecha
        };
    }

    public void EncolarNotificacion(int usuarioId, string mensaje)
    {
        _context.Notificaciones.Add(new Notificacion
        {
            UsuarioId = usuarioId,
            Mensaje = mensaje,
            Leida = false,
            Fecha = DateTime.UtcNow
        });
    }

    public async Task EncolarNotificacionResponsablesDepartamentoAsync(
        int departamentoId,
        string mensaje,
        int? excluirUsuarioId = null,
        CancellationToken cancellationToken = default)
    {
        var responsablesIds = await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.Activo &&
                        u.DepartamentoId == departamentoId &&
                        (!excluirUsuarioId.HasValue || u.Id != excluirUsuarioId.Value) &&
                        u.UsuarioRoles.Any(ur => ur.Rol.Nombre == "ResponsableEspacio"))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var responsableId in responsablesIds)
        {
            EncolarNotificacion(responsableId, mensaje);
        }
    }
}
