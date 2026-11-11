using FluentValidation;
using GestionReservas.Application.Features.Usuarios.DTOs;

namespace GestionReservas.Application.Features.Usuarios.Validators;

public class ActualizarUsuarioRequestValidator : AbstractValidator<ActualizarUsuarioRequestDto>
{
    private const string PasswordSpecialCharactersRegex = @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?~`]";

    public ActualizarUsuarioRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Nombre));

        RuleFor(x => x.Apellido)
            .MaximumLength(100).WithMessage("El apellido no puede superar los 100 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Apellido));

        RuleFor(x => x.Telefono)
            .MaximumLength(30).WithMessage("El teléfono no puede superar los 30 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Telefono));

        RuleFor(x => x.Roles)
            .Must(roles => roles == null || roles.Count > 0)
            .WithMessage("Si especifica la lista de roles, debe incluir al menos un rol.");

        RuleForEach(x => x.Roles)
            .NotEmpty().WithMessage("El nombre del rol no puede estar vacío.")
            .When(x => x.Roles != null);

        RuleFor(x => x.Password)
            .MinimumLength(8).WithMessage("La nueva contraseña debe tener al menos 8 caracteres.")
            .Matches(@"[A-Z]").WithMessage("La nueva contraseña debe contener al menos una letra mayúscula.")
            .Matches(@"[a-z]").WithMessage("La nueva contraseña debe contener al menos una letra minúscula.")
            .Matches(@"[0-9]").WithMessage("La nueva contraseña debe contener al menos un dígito numérico.")
            .Matches(PasswordSpecialCharactersRegex).WithMessage("La nueva contraseña debe contener al menos un símbolo o carácter especial.")
            .When(x => !string.IsNullOrEmpty(x.Password));
    }
}
