using FluentValidation;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Features.Auth;
using GestionReservas.Application.Features.Auth.Services;
using GestionReservas.Application.Features.Catalogos;
using GestionReservas.Application.Features.Catalogos.Services;
using GestionReservas.Application.Features.Usuarios;
using GestionReservas.Application.Features.Usuarios.Services;
using GestionReservas.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GestionReservas.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICatalogosService, CatalogosService>();
        services.AddScoped<IUsuariosService, UsuariosService>();

        // Registra automáticamente todos los validadores FluentValidation del ensamblado
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
