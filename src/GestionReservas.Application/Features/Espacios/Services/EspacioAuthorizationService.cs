using GestionReservas.Application.Common.Exceptions;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionReservas.Application.Features.Espacios.Services;

/// <summary>
/// Implementación del servicio de autorización departamental para la gestión de espacios.
/// </summary>
public class EspacioAuthorizationService : IEspacioAuthorizationService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;

    public EspacioAuthorizationService(ICurrentUserService currentUserService, IApplicationDbContext context)
    {
        _currentUserService = currentUserService;
        _context = context;
    }

    public async Task<bool> PuedeAdministrarDepartamentoAsync(int departamentoId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return false;
        }

        // Administrador y Secretaría Académica pueden gestionar espacios de cualquier departamento
        if (_currentUserService.IsInRole("Administrador") || _currentUserService.IsInRole("SecretariaAcademica"))
        {
            return true;
        }

        // Responsable de Espacio solo puede gestionar los espacios de su departamento
        if (_currentUserService.IsInRole("ResponsableEspacio"))
        {
            var userId = _currentUserService.UsuarioId;
            if (!userId.HasValue)
            {
                return false;
            }

            var usuarioDeptoId = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Id == userId.Value)
                .Select(u => u.DepartamentoId)
                .FirstOrDefaultAsync(cancellationToken);

            return usuarioDeptoId.HasValue && usuarioDeptoId.Value == departamentoId;
        }

        return false;
    }

    public async Task ValidarPuedeAdministrarDepartamentoAsync(int departamentoId, CancellationToken cancellationToken = default)
    {
        var autorizado = await PuedeAdministrarDepartamentoAsync(departamentoId, cancellationToken);
        if (!autorizado)
        {
            throw new ForbiddenException("No posee los permisos necesarios para gestionar espacios en este departamento.");
        }
    }

    public async Task ValidarPuedeAdministrarEspacioAsync(Espacio espacio, CancellationToken cancellationToken = default)
    {
        await ValidarPuedeAdministrarDepartamentoAsync(espacio.DepartamentoId, cancellationToken);
    }
}
