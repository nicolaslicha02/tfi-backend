using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Bitacora.DTOs;
using GestionReservas.Application.Features.Auth.DTOs;
using GestionReservas.Domain.Entities;
using GestionReservas.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionReservas.Tests;

public class AuditoriaEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuditoriaEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> ObtenerTokenAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponseDto>())!.Token;
    }

    [Theory]
    [InlineData("admin@facultad.edu.ar", "Admin123!")]
    [InlineData("secretaria@facultad.edu.ar", "Secretaria123!")]
    public async Task GetAuditoria_AdministradorYSecretaria_PuedenConsultar(string email, string password)
    {
        var token = await ObtenerTokenAsync(email, password);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auditoria?page=1&pageSize=5");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<AuditoriaDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.True(result.Items.Count <= 5);
    }

    [Fact]
    public async Task GetAuditoria_Solicitante_Devuelve403()
    {
        var token = await ObtenerTokenAsync("docente.perez@facultad.edu.ar", "Docente123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auditoria");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditoria_AplicaFiltrosYDevuelveRolHistorico()
    {
        var action = $"PruebaAuditoria{Guid.NewGuid():N}";
        var timestamp = DateTime.UtcNow;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GestionReservas.Infrastructure.Persistence.ApplicationDbContext>();
            db.Auditorias.Add(new Auditoria
            {
                UsuarioId = await db.Usuarios.Where(u => u.Email == "admin@facultad.edu.ar").Select(u => (int?)u.Id).SingleAsync(),
                UsuarioNombre = "Administrador General",
                UsuarioEmail = "admin@facultad.edu.ar",
                Rol = "Administrador",
                FechaHora = timestamp,
                Accion = action,
                Entidad = "Espacio",
                EntidadId = "901",
                Motivo = "Prueba de filtros",
                Detalle = "Evento aislado de prueba"
            });
            await db.SaveChangesAsync();
        }

        var token = await ObtenerTokenAsync("admin@facultad.edu.ar", "Admin123!");
        var from = Uri.EscapeDataString(timestamp.AddMinutes(-1).ToString("O"));
        var to = Uri.EscapeDataString(timestamp.AddMinutes(1).ToString("O"));
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/auditoria?accion={action}&entidad=Espacio&desde={from}&hasta={to}&page=1&pageSize=10");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<AuditoriaDto>>();
        var item = Assert.Single(result!.Items);
        Assert.Equal(action, item.Accion);
        Assert.Equal("Espacio", item.Entidad);
        Assert.Equal("Administrador", item.Rol);
        Assert.Equal("Administrador General", item.UsuarioNombre);
        Assert.Equal("admin@facultad.edu.ar", item.UsuarioEmail);
        Assert.Equal("Prueba de filtros", item.Motivo);
    }

    [Fact]
    public async Task GetAuditoria_FechasInvertidas_Devuelve400()
    {
        var token = await ObtenerTokenAsync("admin@facultad.edu.ar", "Admin123!");
        using var request = new HttpRequestMessage(HttpMethod.Get,
            "/api/auditoria?desde=2026-11-10T00:00:00Z&hasta=2026-11-09T00:00:00Z");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

}
