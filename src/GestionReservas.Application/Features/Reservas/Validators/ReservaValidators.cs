using FluentValidation;
using GestionReservas.Application.Common.Helpers;
using GestionReservas.Application.Features.Reservas.DTOs;

namespace GestionReservas.Application.Features.Reservas.Validators;

/// <summary>
/// Constantes de reglas de negocio temporales para las reservas.
/// </summary>
public static class ReservaValidationRules
{
    public const int DuracionMinimaMinutos = 30;
    public const int DuracionMaximaHoras = 12;
}

/// <summary>
/// Validador para la solicitud de creación de una reserva.
/// </summary>
public class CrearReservaRequestValidator : AbstractValidator<CrearReservaRequestDto>
{
    public CrearReservaRequestValidator()
    {
        RuleFor(x => x.EspacioId)
            .GreaterThan(0).WithMessage("Debe especificar un espacio válido.");

        RuleFor(x => x.FechaHoraInicio)
            .NotEmpty().WithMessage("La fecha y hora de inicio es obligatoria.")
            .Must(inicio => DateTimeZoneHelper.NormalizeToUtc(inicio) > DateTime.UtcNow.AddMinutes(-2))
            .WithMessage("No se permiten crear reservas en el pasado.");

        RuleFor(x => x.FechaHoraFin)
            .NotEmpty().WithMessage("La fecha y hora de finalización es obligatoria.")
            .Must((dto, fin) => DateTimeZoneHelper.NormalizeToUtc(fin) > DateTimeZoneHelper.NormalizeToUtc(dto.FechaHoraInicio))
            .WithMessage("La fecha y hora de finalización debe ser posterior a la fecha y hora de inicio.");

        RuleFor(x => x)
            .Must(dto =>
            {
                var inicioUtc = DateTimeZoneHelper.NormalizeToUtc(dto.FechaHoraInicio);
                var finUtc = DateTimeZoneHelper.NormalizeToUtc(dto.FechaHoraFin);
                if (finUtc <= inicioUtc) return true; // ya cubierto por la regla anterior
                var duracion = finUtc - inicioUtc;
                return duracion >= TimeSpan.FromMinutes(ReservaValidationRules.DuracionMinimaMinutos);
            })
            .WithMessage($"La duración mínima de una reserva es de {ReservaValidationRules.DuracionMinimaMinutos} minutos.")
            .Must(dto =>
            {
                var inicioUtc = DateTimeZoneHelper.NormalizeToUtc(dto.FechaHoraInicio);
                var finUtc = DateTimeZoneHelper.NormalizeToUtc(dto.FechaHoraFin);
                if (finUtc <= inicioUtc) return true;
                var duracion = finUtc - inicioUtc;
                return duracion <= TimeSpan.FromHours(ReservaValidationRules.DuracionMaximaHoras);
            })
            .WithMessage($"La duración máxima permitida para una reserva eventual es de {ReservaValidationRules.DuracionMaximaHoras} horas.");

        RuleFor(x => x.TipoActividad)
            .IsInEnum().WithMessage("El tipo de actividad especificado no es válido.");

        RuleFor(x => x.AsistentesEstimados)
            .GreaterThan(0).WithMessage("La cantidad estimada de asistentes debe ser mayor a 0.");

        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("El motivo de la solicitud es obligatorio.")
            .MaximumLength(300).WithMessage("El motivo no puede superar los 300 caracteres.");

        RuleFor(x => x.Observaciones)
            .MaximumLength(500).WithMessage("Las observaciones no pueden superar los 500 caracteres.");

        RuleFor(x => x.EquipamientoRequerido)
            .Must(list => list == null || list.Select(e => e.EquipamientoId).Distinct().Count() == list.Count)
            .WithMessage("No se puede repetir el mismo equipamiento en la lista de requerimientos.");

        RuleForEach(x => x.EquipamientoRequerido).ChildRules(eq =>
        {
            eq.RuleFor(e => e.EquipamientoId)
                .GreaterThan(0).WithMessage("El identificador de equipamiento debe ser mayor a 0.");
            eq.RuleFor(e => e.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad de equipamiento requerida debe ser al menos 1.");
        });
    }
}

/// <summary>
/// Validador para la modificación de una reserva existente (PUT).
/// </summary>
public class ActualizarReservaRequestValidator : AbstractValidator<ActualizarReservaRequestDto>
{
    public ActualizarReservaRequestValidator()
    {
        RuleFor(x => x.FechaHoraInicio)
            .NotEmpty().WithMessage("La fecha y hora de inicio es obligatoria.")
            .Must(inicio => DateTimeZoneHelper.NormalizeToUtc(inicio) > DateTime.UtcNow.AddMinutes(-2))
            .WithMessage("No se permiten programar reservas en el pasado.");

        RuleFor(x => x.FechaHoraFin)
            .NotEmpty().WithMessage("La fecha y hora de finalización es obligatoria.")
            .Must((dto, fin) => DateTimeZoneHelper.NormalizeToUtc(fin) > DateTimeZoneHelper.NormalizeToUtc(dto.FechaHoraInicio))
            .WithMessage("La fecha y hora de finalización debe ser posterior a la fecha y hora de inicio.");

        RuleFor(x => x)
            .Must(dto =>
            {
                var inicioUtc = DateTimeZoneHelper.NormalizeToUtc(dto.FechaHoraInicio);
                var finUtc = DateTimeZoneHelper.NormalizeToUtc(dto.FechaHoraFin);
                if (finUtc <= inicioUtc) return true;
                var duracion = finUtc - inicioUtc;
                return duracion >= TimeSpan.FromMinutes(ReservaValidationRules.DuracionMinimaMinutos);
            })
            .WithMessage($"La duración mínima de una reserva es de {ReservaValidationRules.DuracionMinimaMinutos} minutos.")
            .Must(dto =>
            {
                var inicioUtc = DateTimeZoneHelper.NormalizeToUtc(dto.FechaHoraInicio);
                var finUtc = DateTimeZoneHelper.NormalizeToUtc(dto.FechaHoraFin);
                if (finUtc <= inicioUtc) return true;
                var duracion = finUtc - inicioUtc;
                return duracion <= TimeSpan.FromHours(ReservaValidationRules.DuracionMaximaHoras);
            })
            .WithMessage($"La duración máxima permitida para una reserva eventual es de {ReservaValidationRules.DuracionMaximaHoras} horas.");

        RuleFor(x => x.TipoActividad)
            .IsInEnum().WithMessage("El tipo de actividad especificado no es válido.");

        RuleFor(x => x.AsistentesEstimados)
            .GreaterThan(0).WithMessage("La cantidad estimada de asistentes debe ser mayor a 0.");

        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("El motivo de la solicitud es obligatorio.")
            .MaximumLength(300).WithMessage("El motivo no puede superar los 300 caracteres.");

        RuleFor(x => x.Observaciones)
            .MaximumLength(500).WithMessage("Las observaciones no pueden superar los 500 caracteres.");

        RuleFor(x => x.EquipamientoRequerido)
            .Must(list => list == null || list.Select(e => e.EquipamientoId).Distinct().Count() == list.Count)
            .WithMessage("No se puede repetir el mismo equipamiento en la lista de requerimientos.");

        RuleForEach(x => x.EquipamientoRequerido).ChildRules(eq =>
        {
            eq.RuleFor(e => e.EquipamientoId)
                .GreaterThan(0).WithMessage("El identificador de equipamiento debe ser mayor a 0.");
            eq.RuleFor(e => e.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad de equipamiento requerida debe ser al menos 1.");
        });
    }
}

/// <summary>
/// Validador para el cambio de estado de una reserva (PATCH).
/// </summary>
public class CambiarEstadoReservaRequestValidator : AbstractValidator<CambiarEstadoReservaRequestDto>
{
    private static readonly string[] AccionesPermitidas =
    {
        ReservaStateMachine.AccionAprobar,
        ReservaStateMachine.AccionRechazar,
        ReservaStateMachine.AccionCancelar
    };

    public CambiarEstadoReservaRequestValidator()
    {
        RuleFor(x => x.Accion)
            .NotEmpty().WithMessage("La acción es obligatoria.")
            .Must(a => AccionesPermitidas.Contains(a.Trim().ToLowerInvariant()))
            .WithMessage("La acción debe ser 'aprobar', 'rechazar' o 'cancelar'.");

        RuleFor(x => x.Motivo)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.Accion) &&
                       x.Accion.Trim().Equals(ReservaStateMachine.AccionRechazar, StringComparison.OrdinalIgnoreCase))
            .WithMessage("El motivo es obligatorio al rechazar una solicitud de reserva.")
            .MaximumLength(500).WithMessage("El motivo no puede superar los 500 caracteres.");
    }
}

/// <summary>
/// Validador para parámetros de consulta de mis-reservas.
/// </summary>
public class MisReservasQueryValidator : AbstractValidator<MisReservasQueryParameters>
{
    public MisReservasQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("El número de página debe ser mayor o igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar entre 1 y 100.");

        RuleFor(x => x.Estado)
            .IsInEnum().When(x => x.Estado.HasValue)
            .WithMessage("El estado de filtro especificado no es válido.");
    }
}
