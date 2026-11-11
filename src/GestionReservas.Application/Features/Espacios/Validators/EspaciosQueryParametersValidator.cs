using FluentValidation;
using GestionReservas.Application.Features.Espacios.DTOs;

namespace GestionReservas.Application.Features.Espacios.Validators;

public class EspaciosQueryParametersValidator : AbstractValidator<EspaciosQueryParameters>
{
    public EspaciosQueryParametersValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("El número de página debe ser mayor o igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar comprendido entre 1 y 100.");

        RuleFor(x => x.CapacidadMinima)
            .GreaterThan(0).WithMessage("La capacidad mínima debe ser mayor a 0.")
            .When(x => x.CapacidadMinima.HasValue);
    }
}
