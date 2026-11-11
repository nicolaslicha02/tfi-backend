using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GestionReservas.Infrastructure.Security;

/// <summary>
/// Servicio encargado de la generación y validación criptográfica de tokens JWT.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly byte[] _keyBytes;

    public JwtTokenService(IOptions<JwtOptions> options, ILogger<JwtTokenService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.Key))
        {
            throw new InvalidOperationException("La clave secreta JWT ('Jwt:Key') no ha sido configurada.");
        }

        _keyBytes = Encoding.UTF8.GetBytes(_options.Key);
    }

    public string GenerateToken(
        Usuario usuario,
        IEnumerable<string> roles,
        IEnumerable<string> permisos,
        out DateTime expiresAt)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.NombreCompleto),
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        foreach (var rol in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, rol));
        }

        foreach (var permiso in permisos)
        {
            claims.Add(new Claim("permiso", permiso));
        }

        expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpireMinutes);

        var key = new SymmetricSecurityKey(_keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(_keyBytes),
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // Reducción de tolerancia para precisión en la expiración
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            if (validatedToken is JwtSecurityToken jwtToken &&
                jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return principal;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Falló la validación del token JWT.");
            return null;
        }
    }
}
