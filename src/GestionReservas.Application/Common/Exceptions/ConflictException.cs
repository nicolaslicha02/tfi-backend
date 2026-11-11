namespace GestionReservas.Application.Common.Exceptions;

/// <summary>
/// Excepción lanzada cuando ocurre un conflicto con el estado actual del recurso (HTTP 409).
/// Por ejemplo: solapamiento de reservas, código duplicado, colisión de bloqueo.
/// </summary>
public class ConflictException : Exception
{
    /// <summary>
    /// Información complementaria sobre el conflicto (ej: listado de reservas afectadas por un bloqueo).
    /// </summary>
    public object? DetalleExtra { get; }

    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, object? detalleExtra) : base(message)
    {
        DetalleExtra = detalleExtra;
    }
}

