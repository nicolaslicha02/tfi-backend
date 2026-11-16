using FluentValidation;
using GestionReservas.Application.Common.Helpers;
using GestionReservas.Application.Features.Calendario.DTOs;

namespace GestionReservas.Application.Features.Calendario.Validators;

/// <summary>
/// Validador para los parámetros de consulta de GET /api/calendario.
/// </summary>
public class CalendarioQueryParametersValidator : AbstractValidator<CalendarioQueryParameters>
{
    public const int MaxDiasRango = 31;

    public CalendarioQueryParametersValidator()
    {
        RuleFor(x => x.Desde)
            .NotNull().WithMessage("La fecha de inicio ('desde') es obligatoria.");

        RuleFor(x => x.Hasta)
            .NotNull().WithMessage("La fecha de fin ('hasta') es obligatoria.");

        RuleFor(x => x)
            .Must(x =>
            {
                if (!x.Desde.HasValue || !x.Hasta.HasValue)
                    return true;

                var desdeUtc = DateTimeZoneHelper.NormalizeToUtc(x.Desde.Value);
                var hastaUtc = DateTimeZoneHelper.NormalizeToUtc(x.Hasta.Value);
                return hastaUtc >= desdeUtc;
            })
            .WithName("Hasta")
            .WithMessage("La fecha 'hasta' debe ser mayor o igual a la fecha 'desde'.");

        RuleFor(x => x)
            .Must(x =>
            {
                if (!x.Desde.HasValue || !x.Hasta.HasValue)
                    return true;

                var desdeUtc = DateTimeZoneHelper.NormalizeToUtc(x.Desde.Value);
                var hastaUtc = DateTimeZoneHelper.NormalizeToUtc(x.Hasta.Value);
                if (hastaUtc < desdeUtc)
                    return true;

                return (hastaUtc - desdeUtc).TotalDays <= MaxDiasRango;
            })
            .WithName("Hasta")
            .WithMessage($"El rango de consulta del calendario no puede superar los {MaxDiasRango} días.");

        RuleFor(x => x.EspacioId)
            .GreaterThan(0)
            .When(x => x.EspacioId.HasValue)
            .WithMessage("El identificador de espacio debe ser mayor a cero.");

        RuleFor(x => x.TipoEspacioId)
            .GreaterThan(0)
            .When(x => x.TipoEspacioId.HasValue)
            .WithMessage("El identificador de tipo de espacio debe ser mayor a cero.");

        RuleFor(x => x.DepartamentoId)
            .GreaterThan(0)
            .When(x => x.DepartamentoId.HasValue)
            .WithMessage("El identificador de departamento debe ser mayor a cero.");

        RuleFor(x => x.Edificio)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Edificio))
            .WithMessage("El filtro de edificio no puede superar los 100 caracteres.");

        RuleFor(x => x.Sector)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Sector))
            .WithMessage("El filtro de sector no puede superar los 100 caracteres.");
    }
}
