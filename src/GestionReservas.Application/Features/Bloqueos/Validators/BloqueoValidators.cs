using FluentValidation;
using GestionReservas.Application.Common.Helpers;
using GestionReservas.Application.Features.Bloqueos.DTOs;

namespace GestionReservas.Application.Features.Bloqueos.Validators;

/// <summary>
/// Validador para la solicitud de creación de un bloqueo temporal sobre un espacio.
/// </summary>
public class CrearBloqueoRequestValidator : AbstractValidator<CrearBloqueoRequestDto>
{
    public CrearBloqueoRequestValidator()
    {
        RuleFor(x => x.EspacioId)
            .GreaterThan(0).WithMessage("Debe especificar un espacio físico válido.");

        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("El motivo o justificación del bloqueo es obligatorio.")
            .MaximumLength(300).WithMessage("El motivo no puede exceder los 300 caracteres.");

        RuleFor(x => x.Desde)
            .NotEmpty().WithMessage("La fecha y hora de inicio del bloqueo es obligatoria.")
            .Must(desde => DateTimeZoneHelper.NormalizeToUtc(desde) > DateTime.UtcNow.AddMinutes(-2))
            .WithMessage("No se pueden registrar bloqueos en el pasado.");

        RuleFor(x => x.Hasta)
            .NotEmpty().WithMessage("La fecha y hora de finalización del bloqueo es obligatoria.")
            .Must((dto, hasta) => DateTimeZoneHelper.NormalizeToUtc(hasta) > DateTimeZoneHelper.NormalizeToUtc(dto.Desde))
            .WithMessage("La fecha y hora de finalización debe ser posterior a la fecha y hora de inicio.");
    }
}

/// <summary>
/// Validador para los parámetros de consulta y paginación de bloqueos.
/// </summary>
public class BloqueosQueryParametersValidator : AbstractValidator<BloqueosQueryParameters>
{
    public BloqueosQueryParametersValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("El número de página debe ser mayor o igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar comprendido entre 1 y 100.");
    }
}
