using System.Security.Claims;
using GestionReservas.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GestionReservas.Infrastructure.Security;

/// <summary>
/// Implementación basada en HttpContextAccessor para extraer claims del usuario actual autenticado.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int? UsuarioId
    {
        get
        {
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var id))
            {
                return id;
            }
            return null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    public IReadOnlyList<string> Roles => User?.FindAll(ClaimTypes.Role)
        .Select(c => c.Value)
        .Distinct()
        .ToList() ?? (IReadOnlyList<string>)Array.Empty<string>();

    public IReadOnlyList<string> Permisos => User?.FindAll("permiso")
        .Select(c => c.Value)
        .Distinct()
        .ToList() ?? (IReadOnlyList<string>)Array.Empty<string>();

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool HasPermission(string permiso) => Permisos.Contains(permiso);

    public bool IsInRole(string rol) => User?.IsInRole(rol) ?? false;
}
