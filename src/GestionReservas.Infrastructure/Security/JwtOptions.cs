namespace GestionReservas.Infrastructure.Security;

/// <summary>
/// Opciones de configuración para la emisión y validación de tokens JWT.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Emisor del token (Issuer).
    /// </summary>
    public string Issuer { get; set; } = "GestionReservasApi";

    /// <summary>
    /// Audiencia esperada del token (Audience).
    /// </summary>
    public string Audience { get; set; } = "GestionReservasClient";

    /// <summary>
    /// Clave criptográfica simétrica secreta para firma HMAC-SHA256 (mínimo 256 bits / 32 caracteres).
    /// Debe configurarse mediante user-secrets o variables de entorno en producción.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Minutos de validez del token JWT.
    /// </summary>
    public int ExpireMinutes { get; set; } = 120;
}
