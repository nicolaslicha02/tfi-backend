namespace GestionReservas.Domain.Enums;

/// <summary>
/// Representa los posibles estados por los que atraviesa una solicitud de reserva.
/// </summary>
public enum EstadoReserva
{
    /// <summary>
    /// Solicitud recién creada por el usuario solicitante.
    /// </summary>
    Solicitada = 1,

    /// <summary>
    /// Solicitud en proceso de revisión o derivación.
    /// </summary>
    PendienteAprobacion = 2,

    /// <summary>
    /// Solicitud aprobada formalmente.
    /// </summary>
    Aprobada = 3,

    /// <summary>
    /// Solicitud rechazada con motivo documentado.
    /// </summary>
    Rechazada = 4,

    /// <summary>
    /// Solicitud cancelada por el solicitante o por la administración.
    /// </summary>
    Cancelada = 5
}
