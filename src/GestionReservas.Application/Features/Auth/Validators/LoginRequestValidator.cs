using FluentValidation;
using GestionReservas.Application.Features.Auth.DTOs;

namespace GestionReservas.Application.Features.Auth.Validators;

/// <summary>
/// Validador FluentValidation para el modelo de inicio de sesión.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.")
            .MaximumLength(150).WithMessage("El correo electrónico no puede superar los 150 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");
    }
}
