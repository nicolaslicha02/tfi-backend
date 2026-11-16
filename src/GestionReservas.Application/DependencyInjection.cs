using FluentValidation;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Features.Auth;
using GestionReservas.Application.Features.Auth.Services;
using GestionReservas.Application.Features.Bitacora;
using GestionReservas.Application.Features.Bitacora.Services;
using GestionReservas.Application.Features.Catalogos;
using GestionReservas.Application.Features.Catalogos.Services;
using GestionReservas.Application.Features.Espacios;
using GestionReservas.Application.Features.Espacios.Services;
using GestionReservas.Application.Features.Usuarios;
using GestionReservas.Application.Features.Usuarios.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GestionReservas.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuditoriaService, AuditoriaService>();
        services.AddScoped<ICatalogosService, CatalogosService>();
        services.AddScoped<IUsuariosService, UsuariosService>();
        services.AddScoped<IEspacioAuthorizationService, EspacioAuthorizationService>();
        services.AddScoped<IDisponibilidadService, DisponibilidadService>();
        services.AddScoped<IEspaciosService, EspaciosService>();
        services.AddScoped<GestionReservas.Application.Features.Notificaciones.INotificacionesService, GestionReservas.Application.Features.Notificaciones.Services.NotificacionesService>();
        services.AddScoped<GestionReservas.Application.Features.Reservas.IReservasService, GestionReservas.Application.Features.Reservas.Services.ReservasService>();
        services.AddScoped<GestionReservas.Application.Features.Bloqueos.IBloqueosService, GestionReservas.Application.Features.Bloqueos.Services.BloqueosService>();
        services.AddScoped<GestionReservas.Application.Features.Calendario.ICalendarioService, GestionReservas.Application.Features.Calendario.Services.CalendarioService>();

        // Registra automáticamente todos los validadores FluentValidation del ensamblado
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
