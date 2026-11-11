using System.Reflection;
using GestionReservas.Api.Middlewares;
using GestionReservas.Application;
using GestionReservas.Infrastructure;
using GestionReservas.Infrastructure.Persistence.Seed;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Carga de User Secrets en desarrollo
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

// 1. Registro de servicios de las capas de la arquitectura
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// 2. Controladores con serialización de enums como texto (JsonStringEnumConverter)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// 3. Swagger / OpenAPI con Swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sistema de Gestión y Reservas de Espacios de la Facultad",
        Version = "v1",
        Description = "API RESTful para la administración de espacios físicos, reservas eventuales, ocupación académica oficial y bloqueos.",
        Contact = new OpenApiContact
        {
            Name = "Facultad - Área de Sistemas",
            Email = "sistemas@facultad.edu.ar"
        }
    });

    // Definición de esquema de seguridad JWT Bearer
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticación JWT usando el esquema Bearer. Ingrese 'Bearer' seguido de un espacio y su token. Ejemplo: \"Bearer eyJhbGciOiJIUzI1...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "Bearer",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            Array.Empty<string>()
        }
    });

    // Incluir comentarios XML para documentación
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// 4. Middleware de captura de excepciones global (ProblemDetails RFC 7807)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 5. Configuración del pipeline HTTP
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema de Gestión y Reservas API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Documentación API - Gestión de Espacios";
});

app.UseHttpsRedirection();

app.UseRouting();

// 6. Autenticación y Autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Ejecución condicional del sembrador de base de datos
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var seeder = services.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "El sembrado automático no pudo completarse en el inicio (la base de datos podría requerir migración previa).");
    }
}

app.Run();

// Clase pública para soporte en WebApplicationFactory de los tests
public partial class Program { }
