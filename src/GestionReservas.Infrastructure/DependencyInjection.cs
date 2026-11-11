using System.Text;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Domain.Constants;
using GestionReservas.Infrastructure.Persistence;
using GestionReservas.Infrastructure.Persistence.Seed;
using GestionReservas.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using Microsoft.Extensions.Hosting;

namespace GestionReservas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        // 1. Base de datos PostgreSQL
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no fue encontrada en la configuración.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<DatabaseSeeder>();

        // 2. Configuración JWT
        const string fallbackJwtKey = "ClaveSuperSecretaExtremadamenteLargaParaJwtGestionDeEspacios2026!#";
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtKey = jwtSection["Key"];

        var isDevelopmentOrTesting = (environment?.IsDevelopment() ?? false)
            || (environment?.IsEnvironment("Testing") ?? false)
            || string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            if (isDevelopmentOrTesting)
            {
                jwtKey = fallbackJwtKey;
            }
            else
            {
                throw new InvalidOperationException(
                    "Falta la configuración 'Jwt:Key'. En entornos fuera de Development es obligatoria y debe ser una clave secreta segura de al menos 32 caracteres.");
            }
        }

        services.Configure<JwtOptions>(options =>
        {
            jwtSection.Bind(options);
            options.Key = jwtKey;
        });

        var jwtIssuer = jwtSection["Issuer"] ?? "GestionReservasApi";
        var jwtAudience = jwtSection["Audience"] ?? "GestionReservasClient";

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // 3. Autenticación JWT Bearer
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // Reducción de tolerancia para precisión en la expiración
            };
        });

        // 4. Autorización dinámica basada en permisos
        // Registra automáticamente una política por cada permiso del catálogo del sistema
        services.AddAuthorization(options =>
        {
            foreach (var permiso in PermisosDefinidos.Todos)
            {
                options.AddPolicy(permiso, policy => policy.RequireClaim("permiso", permiso));
            }
        });

        return services;
    }
}

