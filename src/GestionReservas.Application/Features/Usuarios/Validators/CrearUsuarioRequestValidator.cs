using FluentValidation;
using GestionReservas.Application.Features.Usuarios.DTOs;

namespace GestionReservas.Application.Features.Usuarios.Validators;

public class CrearUsuarioRequestValidator : AbstractValidator<CrearUsuarioRequestDto>
{
    private const string PasswordSpecialCharactersRegex = @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?~`]";

    public CrearUsuarioRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.Apellido)
            .NotEmpty().WithMessage("El apellido es obligatorio.")
            .MaximumLength(100).WithMessage("El apellido no puede superar los 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.")
            .MaximumLength(150).WithMessage("El correo electrónico no puede superar los 150 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña inicial es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .Matches(@"[A-Z]").WithMessage("La contraseña debe contener al menos una letra mayúscula.")
            .Matches(@"[a-z]").WithMessage("La contraseña debe contener al menos una letra minúscula.")
            .Matches(@"[0-9]").WithMessage("La contraseña debe contener al menos un dígito numérico.")
            .Matches(PasswordSpecialCharactersRegex).WithMessage("La contraseña debe contener al menos un símbolo o carácter especial.");

        RuleFor(x => x.Telefono)
            .MaximumLength(30).WithMessage("El teléfono no puede superar los 30 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Telefono));

        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("Debe asignar al menos un rol al usuario.")
            .Must(roles => roles != null && roles.Count > 0).WithMessage("Debe asignar al menos un rol al usuario.");

        RuleForEach(x => x.Roles)
            .NotEmpty().WithMessage("El nombre del rol no puede estar vacío.");
    }
}
