namespace GestionReservas.Application.Common.Interfaces;

/// <summary>
/// Servicio para generación y verificación de contraseñas hasheadas.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Genera un hash seguro para la contraseña en texto plano.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifica si una contraseña en texto plano coincide con el hash almacenado.
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);
}
