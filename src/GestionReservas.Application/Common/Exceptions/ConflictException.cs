namespace GestionReservas.Application.Common.Exceptions;

/// <summary>
/// Excepción lanzada cuando ocurre un conflicto con el estado actual del recurso (HTTP 409).
/// Por ejemplo: solapamiento de reservas, código duplicado, colisión de bloqueo.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
