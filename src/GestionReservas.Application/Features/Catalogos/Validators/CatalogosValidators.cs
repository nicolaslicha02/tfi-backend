using FluentValidation;
using GestionReservas.Application.Features.Catalogos.DTOs;

namespace GestionReservas.Application.Features.Catalogos.Validators;

public class CrearEquipamientoRequestValidator : AbstractValidator<CrearEquipamientoRequestDto>
{
    public CrearEquipamientoRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del equipamiento es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(300).WithMessage("La descripción no puede superar los 300 caracteres.");
    }
}

public class CrearTipoEspacioRequestValidator : AbstractValidator<CrearTipoEspacioRequestDto>
{
    public CrearTipoEspacioRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del tipo de espacio es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(300).WithMessage("La descripción no puede superar los 300 caracteres.");
    }
}
