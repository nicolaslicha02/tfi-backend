namespace GestionReservas.Application.Common.Exceptions;

/// <summary>
/// Excepción lanzada cuando las credenciales no son válidas o falta autenticación (HTTP 401).
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "No autorizado o credenciales inválidas.")
        : base(message)
    {
    }
}
