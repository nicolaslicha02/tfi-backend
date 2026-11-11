using FluentValidation;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Features.Auth;
using GestionReservas.Application.Features.Auth.Services;
using GestionReservas.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GestionReservas.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();

        // Registra automáticamente todos los validadores FluentValidation del ensamblado
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
