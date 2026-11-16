using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestionReservas.Application.Common.Helpers;
using GestionReservas.Application.Features.Auth.DTOs;
using GestionReservas.Application.Features.Bloqueos.DTOs;
using GestionReservas.Domain.Entities;
using GestionReservas.Domain.Enums;
using GestionReservas.Infrastructure.Persistence;
using GestionReservas.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionReservas.Tests;

/// <summary>
/// Regresión del borde de ciclo lectivo en la detección de cursadas oficiales para rangos de varias semanas
/// (ConstruirTramosLocales consolidaba solo la primera fecha de cada día completo de la semana).
/// Usa un ciclo lectivo propio con fechas fijas en 2050 para no depender de UtcNow ni del seed.
/// </summary>
public class DisponibilidadCicloLectivoTests : IClassFixture<CustomWebApplicationFactory>
{
    private const int AnioCicloTest = 2050;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DisponibilidadCicloLectivoTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> AdminTokenAsync()
    {
        var r = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "admin@facultad.edu.ar",
            Password = "Admin123!"
        });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<LoginResponseDto>())!.Token;
    }

    /// <summary>
    /// Crea un espacio aislado con una cursada oficial los lunes de 18:00 a 21:00 dentro de un ciclo lectivo
    /// activo que va del 2050-03-01 al 2050-11-30 (se reutiliza el ciclo si ya existe por la unicidad de Anio).
    /// </summary>
    private async Task<int> CrearEspacioConCursadaLosLunesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sufijo = Guid.NewGuid().ToString("N")[..8].ToUpper();

        var ciclo = await db.CiclosLectivos.FirstOrDefaultAsync(c => c.Anio == AnioCicloTest);
        if (ciclo == null)
        {
            ciclo = new CicloLectivo
            {
                Anio = AnioCicloTest,
                Nombre = "Ciclo Lectivo 2050 (test)",
                FechaInicio = DateTime.SpecifyKind(new DateTime(2050, 3, 1), DateTimeKind.Utc),
                FechaFin = DateTime.SpecifyKind(new DateTime(2050, 11, 30, 23, 59, 59), DateTimeKind.Utc),
                Activo = true
            };
            db.CiclosLectivos.Add(ciclo);
            await db.SaveChangesAsync();
        }

        var espacio = new Espacio
        {
            Codigo = $"CICLO-{sufijo}",
            Nombre = "Espacio regresión ciclo lectivo",
            Edificio = "Test Ciclo",
            Piso = 1,
            Sector = "A",
            CapacidadMaxima = 40,
            Estado = EstadoEspacio.Disponible,
            DepartamentoId = 1,
            TipoEspacioId = 1
        };
        db.Espacios.Add(espacio);

        var materia = new Materia { Codigo = $"MC-{sufijo}", Nombre = $"Materia ciclo {sufijo}", DepartamentoId = 1 };
        db.Materias.Add(materia);
        await db.SaveChangesAsync();

        var cursada = new Cursada { MateriaId = materia.Id, CicloLectivoId = ciclo.Id, Periodo = "Anual" };
        db.Cursadas.Add(cursada);
        await db.SaveChangesAsync();

        var comision = new Comision { CursadaId = cursada.Id, Codigo = $"C-{sufijo}", Turno = "Noche", CantidadInscriptos = 20 };
        db.Comisiones.Add(comision);
        await db.SaveChangesAsync();

        db.AsignacionesEspacio.Add(new AsignacionEspacio
        {
            ComisionId = comision.Id,
            EspacioId = espacio.Id,
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeSpan(18, 0, 0),
            HoraFin = new TimeSpan(21, 0, 0)
        });
        await db.SaveChangesAsync();

        return espacio.Id;
    }

    private async Task<HttpResponseMessage> CrearBloqueoAsync(string token, int espacioId, DateTime desdeLocal, DateTime hastaLocal)
    {
        var msg = new HttpRequestMessage(HttpMethod.Post, "/api/bloqueos")
        {
            Content = JsonContent.Create(new CrearBloqueoRequestDto
            {
                EspacioId = espacioId,
                Desde = DateTimeZoneHelper.ToUtc(DateOnly.FromDateTime(desdeLocal), TimeOnly.FromDateTime(desdeLocal)),
                Hasta = DateTimeZoneHelper.ToUtc(DateOnly.FromDateTime(hastaLocal), TimeOnly.FromDateTime(hastaLocal)),
                Motivo = "Bloqueo de regresión de ciclo lectivo",
                CancelarReservasAfectadas = false
            })
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(msg);
    }

    [Fact]
    public async Task Bloqueo_QueEmpiezaAntesDelCicloYTerminaDentro_ConLunesRepetidos_DetectaCursadaYDevuelve409()
    {
        // Guardas de las fechas fijas: el ciclo arranca el martes 2050-03-01; los lunes 02-21 y 02-28 quedan
        // fuera del ciclo y los lunes 03-07 y 03-14 dentro.
        Assert.Equal(DayOfWeek.Monday, new DateOnly(2050, 2, 21).DayOfWeek);
        Assert.Equal(DayOfWeek.Monday, new DateOnly(2050, 3, 7).DayOfWeek);

        var espacioId = await CrearEspacioConCursadaLosLunesAsync();
        var token = await AdminTokenAsync();

        // Rango 2050-02-20 10:00 -> 2050-03-16 10:00 (hora local): el primer lunes completo (02-21) cae fuera
        // del ciclo, pero los lunes siguientes (03-07, 03-14) caen dentro y chocan con la cursada 18:00-21:00.
        var response = await CrearBloqueoAsync(
            token,
            espacioId,
            new DateTime(2050, 2, 20, 10, 0, 0),
            new DateTime(2050, 3, 16, 10, 0, 0));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var contenido = await response.Content.ReadAsStringAsync();
        Assert.Contains("cursada oficial", contenido);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Bloqueos.AnyAsync(b => b.EspacioId == espacioId));
    }

    [Fact]
    public async Task Bloqueo_QueEmpiezaAntesDelCicloYTerminaAntesDeSuInicio_SinCursadaSuperpuesta_EsPermitido()
    {
        Assert.Equal(DayOfWeek.Monday, new DateOnly(2050, 2, 21).DayOfWeek);
        Assert.Equal(DayOfWeek.Monday, new DateOnly(2050, 2, 28).DayOfWeek);

        var espacioId = await CrearEspacioConCursadaLosLunesAsync();
        var token = await AdminTokenAsync();

        // Rango 2050-02-20 10:00 -> 2050-02-28 10:00 (hora local): los lunes involucrados (02-21 y 02-28)
        // son anteriores al inicio del ciclo (2050-03-01), por lo que no hay cursada oficial: sin falso positivo.
        var response = await CrearBloqueoAsync(
            token,
            espacioId,
            new DateTime(2050, 2, 20, 10, 0, 0),
            new DateTime(2050, 2, 28, 10, 0, 0));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
