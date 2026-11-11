namespace GestionReservas.Application.Features.Espacios;

/// <summary>
/// Contrato del servicio de cálculo y verificación de disponibilidad de espacios físicos.
/// Evalúa superposiciones temporales de reservas aprobadas y bloqueos institucionales.
/// </summary>
public interface IDisponibilidadService
{
    /// <summary>
    /// Determina si un espacio específico se encuentra disponible en un rango horario en UTC.
    /// Ejecuta la comprobación a nivel de base de datos.
    /// </summary>
    /// <param name="espacioId">Identificador del espacio a verificar.</param>
    /// <param name="desdeUtc">Inicio del intervalo en UTC.</param>
    /// <param name="hastaUtc">Fin del intervalo en UTC.</param>
    /// <param name="excluirReservaId">Identificador de reserva a excluir (útil en modificaciones de reservas).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>True si el espacio está disponible; false en caso contrario.</returns>
    Task<bool> EstaDisponibleAsync(
        int espacioId,
        DateTime desdeUtc,
        DateTime hastaUtc,
        int? excluirReservaId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera la subconsulta IQueryable de identificadores de espacios que NO están disponibles en el rango horario UTC dado.
    /// Esta consulta se compone directamente en la consulta principal de base de datos sin cargar elementos a memoria.
    /// </summary>
    IQueryable<int> ObtenerEspaciosNoDisponiblesQuery(
        DateTime desdeUtc,
        DateTime hastaUtc,
        int? excluirReservaId = null);
}
