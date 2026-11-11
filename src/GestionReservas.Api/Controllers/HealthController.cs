using GestionReservas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace GestionReservas.Api.Controllers;

/// <summary>
/// Controlador de diagnóstico y comprobación de estado de salud del servicio (Health Check).
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ApplicationDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Devuelve el estado operativo de la API y la conectividad con la base de datos PostgreSQL.
    /// </summary>
    /// <returns>Objeto con estado de salud, conectividad a base de datos y marca temporal.</returns>
    /// <response code="200">El servicio y la base de datos se encuentran operativos.</response>
    /// <response code="503">El servicio no puede conectarse a la base de datos.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();

            if (canConnect)
            {
                return Ok(new
                {
                    status = "Healthy",
                    database = "Connected",
                    databaseProvider = "PostgreSQL",
                    timestamp = DateTime.UtcNow,
                    service = "Sistema de Gestión y Reservas de Espacios de la Facultad",
                    version = "1.0.0"
                });
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Degraded",
                database = "Disconnected",
                timestamp = DateTime.UtcNow,
                service = "Sistema de Gestión y Reservas de Espacios de la Facultad"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al comprobar conectividad con la base de datos.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
