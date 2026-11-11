using FluentValidation;
using GestionReservas.Application.Features.Espacios.DTOs;

namespace GestionReservas.Application.Features.Espacios.Validators;

public class ActualizarEspacioRequestValidator : AbstractValidator<ActualizarEspacioRequestDto>
{
    public ActualizarEspacioRequestValidator()
    {
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código del espacio es obligatorio.")
            .MaximumLength(50).WithMessage("El código no puede superar los 50 caracteres.");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del espacio es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede superar los 150 caracteres.");

        RuleFor(x => x.TipoEspacioId)
            .GreaterThan(0).WithMessage("Debe seleccionar un tipo de espacio válido.");

        RuleFor(x => x.Edificio)
            .NotEmpty().WithMessage("El edificio es obligatorio.")
            .MaximumLength(100).WithMessage("El edificio no puede superar los 100 caracteres.");

        RuleFor(x => x.Sector)
            .NotEmpty().WithMessage("El sector es obligatorio.")
            .MaximumLength(100).WithMessage("El sector no puede superar los 100 caracteres.");

        RuleFor(x => x.CapacidadMaxima)
            .GreaterThan(0).WithMessage("La capacidad máxima debe ser un valor estrictamente positivo mayor a 0.");

        RuleFor(x => x.DepartamentoId)
            .GreaterThan(0).WithMessage("Debe seleccionar un departamento responsable válido.");

        RuleFor(x => x.Equipamientos)
            .Must(equipamientos => equipamientos == null || equipamientos.Select(e => e.EquipamientoId).Distinct().Count() == equipamientos.Count)
            .WithMessage("No se pueden especificar equipamientos duplicados en la dotación del espacio.");

        RuleForEach(x => x.Equipamientos).ChildRules(item =>
        {
            item.RuleFor(e => e.EquipamientoId)
                .GreaterThan(0).WithMessage("El identificador de equipamiento debe ser válido.");

            item.RuleFor(e => e.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad de equipamiento debe ser mayor a 0.");

            item.RuleFor(e => e.Especificacion)
                .MaximumLength(200).WithMessage("La especificación no puede superar los 200 caracteres.");
        });
    }
}
