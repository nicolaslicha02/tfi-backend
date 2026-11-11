using FluentValidation;
using GestionReservas.Application.Features.Espacios.DTOs;

namespace GestionReservas.Application.Features.Espacios.Validators;

public class DisponibilidadEspacioQueryParametersValidator : AbstractValidator<DisponibilidadEspacioQueryParameters>
{
    public DisponibilidadEspacioQueryParametersValidator()
    {
        RuleFor(x => x.Fecha)
            .NotNull().WithMessage("La fecha de consulta es obligatoria.");

        RuleFor(x => x.HoraInicio)
            .NotNull().WithMessage("La hora de inicio es obligatoria.");

        RuleFor(x => x.HoraFin)
            .NotNull().WithMessage("La hora de finalización es obligatoria.");

        RuleFor(x => x.HoraFin)
            .Must((dto, horaFin) =>
                dto.HoraInicio == null ||
                horaFin == null ||
                horaFin > dto.HoraInicio)
            .WithMessage("La hora de finalización debe ser posterior a la hora de inicio.");

        RuleFor(x => x.CapacidadMinima)
            .GreaterThan(0).WithMessage("La capacidad mínima debe ser mayor a 0.")
            .When(x => x.CapacidadMinima.HasValue);
    }
}
