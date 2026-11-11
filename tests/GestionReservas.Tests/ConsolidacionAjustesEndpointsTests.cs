using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestionReservas.Application.Common.Helpers;
using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Auth.DTOs;
using GestionReservas.Application.Features.Catalogos.DTOs;
using GestionReservas.Application.Features.Reservas.DTOs;
using GestionReservas.Domain.Entities;
using GestionReservas.Domain.Enums;
using GestionReservas.Infrastructure.Persistence;
using GestionReservas.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionReservas.Tests;

public class ConsolidacionAjustesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ConsolidacionAjustesEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> TokenAsync(string email, string password)
    {
        var r = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Email = email, Password = password });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<LoginResponseDto>())!.Token;
    }

    private Task<string> DocenteAsync() => TokenAsync("docente.perez@facultad.edu.ar", "Docente123!");
    private Task<string> AdminAsync() => TokenAsync("admin@facultad.edu.ar", "Admin123!");
    private Task<string> ResponsableAsync() => TokenAsync("responsable.sistemas@facultad.edu.ar", "Responsable123!");

    private Task<HttpResponseMessage> SendAsync(string token, HttpMethod method, string url, object? body = null)
    {
        var msg = new HttpRequestMessage(method, url);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body != null) msg.Content = JsonContent.Create(body);
        return _client.SendAsync(msg);
    }

    [Fact]
    public async Task Reserva_EnHorarioCursadaOficial_Devuelve409Conflict()
    {
        var docente = await DocenteAsync();

        // Obtener Id de AULA-101 sembrada
        int aula101Id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var aula101 = await db.Espacios.FirstAsync(e => e.Codigo == "AULA-101");
            aula101Id = aula101.Id;
        }

        // Martes 17 de Noviembre de 2026 de 18:30 a 20:30 (solapado con Álgebra A1011: Martes 18:00 a 21:00)
        var fechaMartes = new DateOnly(2026, 11, 17);
        var inicioUtc = DateTimeZoneHelper.ToUtc(fechaMartes, new TimeOnly(18, 30));
        var finUtc = DateTimeZoneHelper.ToUtc(fechaMartes, new TimeOnly(20, 30));

        var request = new CrearReservaRequestDto
        {
            EspacioId = aula101Id,
            FechaHoraInicio = inicioUtc,
            FechaHoraFin = finUtc,
            TipoActividad = TipoActividad.Clase,
            AsistentesEstimados = 35,
            Motivo = "Clase de consulta extraordinaria",
            EquipamientoRequerido = new() { new() { EquipamientoId = 1, Cantidad = 1 } }
        };

        var response = await SendAsync(docente, HttpMethod.Post, "/api/reservas", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("ocupado por la cursada oficial", content);
        Assert.Contains("A1011", content);
    }

    [Fact]
    public async Task Reserva_EnMismoEspacio_HorarioContiguoODiaDistinto_Devuelve201()
    {
        var docente = await DocenteAsync();

        int aula101Id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var aula101 = await db.Espacios.FirstAsync(e => e.Codigo == "AULA-101");
            aula101Id = aula101.Id;
        }

        // 1. Horario contiguo en el mismo martes: 21:00 a 22:30 (la cursada oficial finaliza exactamente a las 21:00)
        var fechaMartes = new DateOnly(2026, 11, 17);
        var inicioContiguo = DateTimeZoneHelper.ToUtc(fechaMartes, new TimeOnly(21, 0));
        var finContiguo = DateTimeZoneHelper.ToUtc(fechaMartes, new TimeOnly(22, 30));

        var reqContiguo = new CrearReservaRequestDto
        {
            EspacioId = aula101Id,
            FechaHoraInicio = inicioContiguo,
            FechaHoraFin = finContiguo,
            TipoActividad = TipoActividad.Clase,
            AsistentesEstimados = 30,
            Motivo = "Clase contigua post-cursada",
            EquipamientoRequerido = new() { new() { EquipamientoId = 1, Cantidad = 1 } }
        };

        var respContigua = await SendAsync(docente, HttpMethod.Post, "/api/reservas", reqContiguo);
        Assert.Equal(HttpStatusCode.Created, respContigua.StatusCode);

        // 2. Día distinto: Miércoles 18 de Noviembre de 2026 de 18:00 a 20:00 (sin cursada oficial en ese espacio)
        var fechaMiercoles = new DateOnly(2026, 11, 18);
        var inicioOtroDia = DateTimeZoneHelper.ToUtc(fechaMiercoles, new TimeOnly(18, 0));
        var finOtroDia = DateTimeZoneHelper.ToUtc(fechaMiercoles, new TimeOnly(20, 0));

        var reqOtroDia = new CrearReservaRequestDto
        {
            EspacioId = aula101Id,
            FechaHoraInicio = inicioOtroDia,
            FechaHoraFin = finOtroDia,
            TipoActividad = TipoActividad.Clase,
            AsistentesEstimados = 30,
            Motivo = "Clase en día libre de cursadas",
            EquipamientoRequerido = new() { new() { EquipamientoId = 1, Cantidad = 1 } }
        };

        var respOtroDia = await SendAsync(docente, HttpMethod.Post, "/api/reservas", reqOtroDia);
        Assert.Equal(HttpStatusCode.Created, respOtroDia.StatusCode);
    }

    [Fact]
    public async Task GetRoles_Devuelve200Con4Roles()
    {
        var docente = await DocenteAsync();

        var response = await SendAsync(docente, HttpMethod.Get, "/api/roles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var roles = await response.Content.ReadFromJsonAsync<List<RolDto>>();
        Assert.NotNull(roles);
        Assert.True(roles.Count >= 4);
        Assert.Contains(roles, r => r.Nombre == "Solicitante");
        Assert.Contains(roles, r => r.Nombre == "ResponsableEspacio");
        Assert.Contains(roles, r => r.Nombre == "SecretariaAcademica");
        Assert.Contains(roles, r => r.Nombre == "Administrador");
    }

    [Fact]
    public async Task PostEquipamiento_PorAdmin_Devuelve201_YDocente_Devuelve403()
    {
        var admin = await AdminAsync();
        var docente = await DocenteAsync();

        var uniqueName = $"Equipamiento Test {Guid.NewGuid().ToString("N")[..6]}";
        var request = new CrearEquipamientoRequestDto
        {
            Nombre = uniqueName,
            Descripcion = "Descripción técnica para pruebas"
        };

        try
        {
            // 1. Docente intenta crear equipamiento -> 403 Forbidden
            var forbiddenResp = await SendAsync(docente, HttpMethod.Post, "/api/equipamientos", request);
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenResp.StatusCode);

            // 2. Administrador crea equipamiento -> 201 Created
            var createdResp = await SendAsync(admin, HttpMethod.Post, "/api/equipamientos", request);
            Assert.Equal(HttpStatusCode.Created, createdResp.StatusCode);

            var eqCreado = await createdResp.Content.ReadFromJsonAsync<EquipamientoDto>();
            Assert.NotNull(eqCreado);
            Assert.Equal(uniqueName, eqCreado.Nombre);

            // 3. Intento de duplicado por nombre -> 409 Conflict
            var conflictResp = await SendAsync(admin, HttpMethod.Post, "/api/equipamientos", request);
            Assert.Equal(HttpStatusCode.Conflict, conflictResp.StatusCode);
        }
        finally
        {
            // Limpieza inmediata para mantener intacto el catálogo semilla de 8 equipamientos
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var creados = await db.Equipamientos.Where(e => e.Nombre.StartsWith("Equipamiento Test")).ToListAsync();
            if (creados.Any())
            {
                db.Equipamientos.RemoveRange(creados);
                await db.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task GetReservas_Administrativas_ConFiltrosYAlcanceDepartamental()
    {
        var docente = await DocenteAsync();
        var admin = await AdminAsync();
        var responsable = await ResponsableAsync();

        // 1. Solicitante intenta acceder al endpoint administrativo -> 403 Forbidden
        var forbiddenResp = await SendAsync(docente, HttpMethod.Get, "/api/reservas");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResp.StatusCode);

        // 2. Administrador accede con filtros -> 200 OK
        var adminResp = await SendAsync(admin, HttpMethod.Get, "/api/reservas?page=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, adminResp.StatusCode);
        var adminResult = await adminResp.Content.ReadFromJsonAsync<PagedResultDto<ReservaListadoDto>>();
        Assert.NotNull(adminResult);

        // 3. Responsable accede -> 200 OK y solo ve espacios de su departamento (departamento 1 = Sistemas)
        var respResp = await SendAsync(responsable, HttpMethod.Get, "/api/reservas?page=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, respResp.StatusCode);
        var respResult = await respResp.Content.ReadFromJsonAsync<PagedResultDto<ReservaListadoDto>>();
        Assert.NotNull(respResult);
        Assert.All(respResult.Items, r => Assert.Equal(1, r.DepartamentoId));
    }
}

