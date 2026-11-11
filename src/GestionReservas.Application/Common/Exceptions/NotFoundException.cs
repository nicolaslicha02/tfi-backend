namespace GestionReservas.Application.Common.Exceptions;

/// <summary>
/// Excepción lanzada cuando una entidad o recurso solicitado no existe (HTTP 404).
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string name, object key)
        : base($"El recurso '{name}' con identificador '{key}' no fue encontrado.")
    {
    }
}
