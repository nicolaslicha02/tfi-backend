using FluentValidation;
using GestionReservas.Application.Common.Helpers;
using GestionReservas.Application.Features.Bitacora.DTOs;

namespace GestionReservas.Application.Features.Bitacora.Validators;

public sealed class AuditoriaQueryParametersValidator : AbstractValidator<AuditoriaQueryParameters>
{
    public AuditoriaQueryParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Page).LessThanOrEqualTo(1_000_000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.UsuarioId).GreaterThan(0).When(x => x.UsuarioId.HasValue);
        RuleFor(x => x.Accion).MaximumLength(100);
        RuleFor(x => x.Entidad).MaximumLength(100);
        RuleFor(x => x).Must(x => !x.Desde.HasValue || !x.Hasta.HasValue ||
                                  DateTimeZoneHelper.NormalizeToUtc(x.Desde.Value) <= DateTimeZoneHelper.NormalizeToUtc(x.Hasta.Value))
            .WithMessage("La fecha 'desde' debe ser anterior o igual a 'hasta'.");
    }
}
