using System.Security.Claims;
using GestionReservas.Domain.Entities;

namespace GestionReservas.Application.Common.Interfaces;

/// <summary>
/// Contrato para el servicio de generación y validación de tokens JWT.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Genera un token JWT firmado que incluye los claims de identidad, roles y permisos del usuario.
    /// </summary>
    /// <param name="usuario">Entidad de usuario autenticado.</param>
    /// <param name="roles">Colección de nombres de roles asignados.</param>
    /// <param name="permisos">Colección de códigos de permisos asignados.</param>
    /// <param name="expiresAt">Fecha y hora UTC de expiración calculada.</param>
    /// <returns>Cadena del token JWT compacto.</returns>
    string GenerateToken(Usuario usuario, IEnumerable<string> roles, IEnumerable<string> permisos, out DateTime expiresAt);

    /// <summary>
    /// Valida un token JWT y devuelve el ClaimsPrincipal si es válido, o null si expiró o es inválido.
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);
}
