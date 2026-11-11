namespace GestionReservas.Application.Common.Exceptions;

/// <summary>
/// Excepción lanzada cuando el usuario autenticado no posee los permisos requeridos para realizar la acción (HTTP 403).
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "No posee los permisos necesarios para realizar esta acción.")
        : base(message)
    {
    }
}
