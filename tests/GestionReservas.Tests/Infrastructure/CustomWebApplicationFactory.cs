using GestionReservas.Infrastructure.Persistence;
using GestionReservas.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestionReservas.Tests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly object _dbLock = new();
    private static bool _dbInitialized = false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Port=5432;Database=GestionReservasDb_Test;Username=postgres;Password=postgres");
        builder.UseSetting("Jwt:Key", "ClaveSuperSecretaExtremadamenteLargaParaJwtGestionDeEspacios2026!#");
        builder.UseSetting("Jwt:Issuer", "GestionReservasApi");
        builder.UseSetting("Jwt:Audience", "GestionReservasClient");
        builder.UseSetting("Jwt:ExpireMinutes", "60");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Host=localhost;Port=5432;Database=GestionReservasDb_Test;Username=postgres;Password=postgres" },
                { "Jwt:Key", "ClaveSuperSecretaExtremadamenteLargaParaJwtGestionDeEspacios2026!#" },
                { "Jwt:Issuer", "GestionReservasApi" },
                { "Jwt:Audience", "GestionReservasClient" },
                { "Jwt:ExpireMinutes", "60" }
            };
            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            // Filtro para solucionar incompatibilidad de TestServer BodyWriter en .NET 10
            services.AddTransient<IStartupFilter, TestHostPipeWriterFixStartupFilter>();

            // Reemplazar la registración de ApplicationDbContext para apuntar explícitamente a la base de pruebas (GestionReservasDb_Test)
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(ApplicationDbContext)).ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            const string testConnectionString = "Host=localhost;Port=5432;Database=GestionReservasDb_Test;Username=postgres;Password=postgres";

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(testConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                });
            });

            // Asegurar que la base de datos de test esté migrada y sembrada exactamente una vez
            lock (_dbLock)
            {
                if (!_dbInitialized)
                {
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                    var seeder = scopedServices.GetRequiredService<DatabaseSeeder>();

                    db.Database.Migrate();
                    seeder.SeedAsync().GetAwaiter().GetResult();

                    // Limpieza preventiva de equipamientos de prueba para mantener inalterado el conteo del seed (8)
                    var equipamientosPrueba = db.Equipamientos.Where(e => e.Nombre.StartsWith("Equipamiento Test")).ToList();
                    if (equipamientosPrueba.Any())
                    {
                        db.Equipamientos.RemoveRange(equipamientosPrueba);
                        db.SaveChanges();
                    }

                    _dbInitialized = true;
                }
            }
        });
    }
}

/// <summary>
/// StartupFilter para compatibilidad entre TestServer de ASP.NET Core y System.Text.Json en .NET 10.
/// </summary>
public class TestHostPipeWriterFixStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                var originalBody = context.Response.Body;
                using var memoryStream = new MemoryStream();
                context.Response.Body = memoryStream;
                try
                {
                    await nextMiddleware();
                }
                finally
                {
                    memoryStream.Position = 0;
                    await memoryStream.CopyToAsync(originalBody);
                    context.Response.Body = originalBody;
                }
            });
            next(app);
        };
    }
}

