using FluentValidation;
using GestionReservas.Application.Features.Usuarios.DTOs;

namespace GestionReservas.Application.Features.Usuarios.Validators;

public class UsuariosQueryParametersValidator : AbstractValidator<UsuariosQueryParameters>
{
    public UsuariosQueryParametersValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("El número de página debe ser mayor o igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar comprendido entre 1 y 100.");
    }
}
