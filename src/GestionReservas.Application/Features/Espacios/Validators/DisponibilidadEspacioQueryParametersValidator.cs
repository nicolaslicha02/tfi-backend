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

        RuleFor(x => x.TipoEspacioId)
            .GreaterThan(0).WithMessage("El tipo de espacio debe ser un identificador válido mayor a 0.")
            .When(x => x.TipoEspacioId.HasValue);

        RuleFor(x => x.Edificio)
            .MaximumLength(100).WithMessage("El filtro de edificio no puede superar los 100 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Edificio));

        RuleFor(x => x.Sector)
            .MaximumLength(100).WithMessage("El filtro de sector no puede superar los 100 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Sector));

        RuleForEach(x => x.EquipamientoIds)
            .GreaterThan(0).WithMessage("Cada identificador de equipamiento requerido debe ser mayor a 0.")
            .When(x => x.EquipamientoIds != null && x.EquipamientoIds.Count > 0);
    }
}
