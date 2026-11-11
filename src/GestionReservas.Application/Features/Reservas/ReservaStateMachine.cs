using GestionReservas.Application.Common.Exceptions;
using GestionReservas.Domain.Enums;

namespace GestionReservas.Application.Features.Reservas;

/// <summary>
/// Máquina de estados centralizada que gobierna el ciclo de vida y las transiciones válidas de una reserva.
/// </summary>
public static class ReservaStateMachine
{
    public const string AccionAprobar = "aprobar";
    public const string AccionRechazar = "rechazar";
    public const string AccionCancelar = "cancelar";

    /// <summary>
    /// Determina el estado inicial al crear una reserva según si aplica auto-aprobación institucional.
    /// </summary>
    public static EstadoReserva DeterminarEstadoInicial(bool autoAprobar)
    {
        return autoAprobar ? EstadoReserva.Aprobada : EstadoReserva.PendienteAprobacion;
    }

    /// <summary>
    /// Evalúa y ejecuta la transición de estado solicitada mediante una acción explícita ("aprobar", "rechazar", "cancelar").
    /// Arroja ConflictException (409) si la transición no es válida desde el estado actual.
    /// </summary>
    public static EstadoReserva ResolverTransicion(EstadoReserva estadoActual, string accion)
    {
        var accionNormalizada = accion.Trim().ToLowerInvariant();

        if (estadoActual is EstadoReserva.Rechazada or EstadoReserva.Cancelada)
        {
            throw new ConflictException(
                $"La reserva se encuentra en estado final '{estadoActual}' y no admite nuevas transiciones (acción solicitada: '{accionNormalizada}').");
        }

        return (estadoActual, accionNormalizada) switch
        {
            (EstadoReserva.PendienteAprobacion, AccionAprobar) => EstadoReserva.Aprobada,
            (EstadoReserva.PendienteAprobacion, AccionRechazar) => EstadoReserva.Rechazada,
            (EstadoReserva.PendienteAprobacion, AccionCancelar) => EstadoReserva.Cancelada,
            (EstadoReserva.Solicitada, AccionAprobar) => EstadoReserva.Aprobada,
            (EstadoReserva.Solicitada, AccionRechazar) => EstadoReserva.Rechazada,
            (EstadoReserva.Solicitada, AccionCancelar) => EstadoReserva.Cancelada,
            (EstadoReserva.Aprobada, AccionCancelar) => EstadoReserva.Cancelada,
            _ => throw new ConflictException(
                $"Transición de estado inválida: no se puede ejecutar la acción '{accionNormalizada}' cuando la reserva está en estado '{estadoActual}'.")
        };
    }

    /// <summary>
    /// Determina el estado resultante al modificar los datos de una reserva existente (PUT).
    /// Si estaba Aprobada, vuelve a PendienteAprobacion para su revalidación y nueva revisión.
    /// Si estaba en estado final (Rechazada o Cancelada), arroja ConflictException (409).
    /// </summary>
    public static EstadoReserva ResolverTransicionPorModificacion(EstadoReserva estadoActual)
    {
        if (estadoActual is EstadoReserva.Rechazada or EstadoReserva.Cancelada)
        {
            throw new ConflictException(
                $"No se puede modificar una reserva que se encuentra en estado final '{estadoActual}'.");
        }

        return EstadoReserva.PendienteAprobacion;
    }

    /// <summary>
    /// Indica si la transición hacia el nuevo estado exige obligatoriamente especificar un motivo.
    /// - Rechazar desde PendienteAprobacion: motivo obligatorio.
    /// - Cancelar desde Aprobada: motivo obligatorio.
    /// </summary>
    public static bool RequiereMotivoObligatorio(EstadoReserva estadoActual, string accion)
    {
        var accionNormalizada = accion.Trim().ToLowerInvariant();

        if (accionNormalizada == AccionRechazar)
        {
            return true;
        }

        if (accionNormalizada == AccionCancelar && estadoActual == EstadoReserva.Aprobada)
        {
            return true;
        }

        return false;
    }
}
