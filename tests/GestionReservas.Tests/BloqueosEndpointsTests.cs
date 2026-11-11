using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Auth.DTOs;
using GestionReservas.Application.Features.Bloqueos.DTOs;
using GestionReservas.Application.Features.Notificaciones.DTOs;
using GestionReservas.Application.Features.Reservas.DTOs;
using GestionReservas.Domain.Entities;
using GestionReservas.Domain.Enums;
using GestionReservas.Infrastructure.Persistence;
using GestionReservas.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionReservas.Tests;

public class BloqueosEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static int _dayCounter = 200;

    public BloqueosEndpointsTests(CustomWebApplicationFactory factory)
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

    private static DateTime Dia() => DateTime.UtcNow.Date.AddDays(Interlocked.Increment(ref _dayCounter));

    private async Task<int> CrearEspacioAsync(int departamentoId = 2, EstadoEspacio estado = EstadoEspacio.Disponible)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var espacio = new Espacio
        {
            Codigo = $"BLQ-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            Nombre = "Espacio para pruebas de bloqueos",
            Edificio = "Edificio Bloqueos",
            Piso = 1,
            Sector = "Ala Este",
            CapacidadMaxima = 50,
            Estado = estado,
            DepartamentoId = departamentoId,
            TipoEspacioId = 1
        };
        espacio.Equipamientos.Add(new EspacioEquipamiento { EquipamientoId = 1, Cantidad = 1 });
        db.Espacios.Add(espacio);
        await db.SaveChangesAsync();
        return espacio.Id;
    }

    [Fact]
    public async Task Crear_SinReservasAfectadas_Devuelve201()
    {
        var espacioId = await CrearEspacioAsync();
        var d = Dia();
        var admin = await AdminAsync();

        var body = new CrearBloqueoRequestDto
        {
            EspacioId = espacioId,
            Desde = d.AddHours(8),
            Hasta = d.AddHours(14),
            Motivo = "Fumigación y control preventivo",
            CancelarReservasAfectadas = false
        };

        var response = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var bloqueo = await response.Content.ReadFromJsonAsync<CrearBloqueoResponseDto>();
        Assert.NotNull(bloqueo);
        Assert.Equal(espacioId, bloqueo.EspacioId);
        Assert.Empty(bloqueo.ReservasCanceladas);
    }

    [Fact]
    public async Task Crear_ConAfectadas_FlagFalse_Devuelve409ConLista()
    {
        var espacioId = await CrearEspacioAsync();
        var d = Dia();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();

        // 1. Docente crea una reserva 10:00 a 12:00
        var reservaReq = new CrearReservaRequestDto
        {
            EspacioId = espacioId,
            FechaHoraInicio = d.AddHours(10),
            FechaHoraFin = d.AddHours(12),
            TipoActividad = TipoActividad.Clase,
            AsistentesEstimados = 20,
            Motivo = "Clase afectada",
            EquipamientoRequerido = new() { new() { EquipamientoId = 1, Cantidad = 1 } }
        };
        var resResp = await SendAsync(docente, HttpMethod.Post, "/api/reservas", reservaReq);
        Assert.Equal(HttpStatusCode.Created, resResp.StatusCode);
        var reservaCreada = await resResp.Content.ReadFromJsonAsync<ReservaDetalleDto>();

        // 2. Intentar crear bloqueo de 09:00 a 13:00 con CancelarReservasAfectadas = false
        var bloqueoReq = new CrearBloqueoRequestDto
        {
            EspacioId = espacioId,
            Desde = d.AddHours(9),
            Hasta = d.AddHours(13),
            Motivo = "Pintura general",
            CancelarReservasAfectadas = false
        };

        var conflictResp = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", bloqueoReq);
        Assert.Equal(HttpStatusCode.Conflict, conflictResp.StatusCode);

        // Verificar que la respuesta JSON incluya la lista de reservas afectadas
        var content = await conflictResp.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        Assert.True(jsonDoc.RootElement.TryGetProperty("reservasAfectadas", out var afectadasElem));
        Assert.Equal(JsonValueKind.Array, afectadasElem.ValueKind);
        Assert.True(afectadasElem.GetArrayLength() > 0);
        Assert.Contains(afectadasElem.EnumerateArray(), elem => elem.GetProperty("id").GetInt32() == reservaCreada!.Id);
    }

    [Fact]
    public async Task Crear_ConAfectadas_FlagTrue_Devuelve201YCancelaReservasYNotifica()
    {
        var espacioId = await CrearEspacioAsync();
        var d = Dia();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();

        // 1. Docente solicita reserva
        var reservaReq = new CrearReservaRequestDto
        {
            EspacioId = espacioId,
            FechaHoraInicio = d.AddHours(14),
            FechaHoraFin = d.AddHours(16),
            TipoActividad = TipoActividad.Clase,
            AsistentesEstimados = 25,
            Motivo = "Clase de laboratorio",
            EquipamientoRequerido = new() { new() { EquipamientoId = 1, Cantidad = 1 } }
        };
        var resResp = await SendAsync(docente, HttpMethod.Post, "/api/reservas", reservaReq);
        Assert.Equal(HttpStatusCode.Created, resResp.StatusCode);
        var reserva = await resResp.Content.ReadFromJsonAsync<ReservaDetalleDto>();

        // 2. Crear bloqueo con CancelarReservasAfectadas = true
        var bloqueoReq = new CrearBloqueoRequestDto
        {
            EspacioId = espacioId,
            Desde = d.AddHours(13),
            Hasta = d.AddHours(18),
            Motivo = "Reparación eléctrica de urgencia",
            CancelarReservasAfectadas = true
        };

        var response = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", bloqueoReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var resultado = await response.Content.ReadFromJsonAsync<CrearBloqueoResponseDto>();
        Assert.NotNull(resultado);
        Assert.Single(resultado.ReservasCanceladas);
        Assert.Equal(reserva!.Id, resultado.ReservasCanceladas[0].Id);
        Assert.Equal(EstadoReserva.Cancelada, resultado.ReservasCanceladas[0].Estado);

        // 3. Verificar que la reserva esté cancelada en su detalle
        var detalleRes = await (await SendAsync(docente, HttpMethod.Get, $"/api/reservas/{reserva.Id}")).Content.ReadFromJsonAsync<ReservaDetalleDto>();
        Assert.Equal(EstadoReserva.Cancelada, detalleRes!.Estado);
        Assert.Contains("Cancelada por bloqueo del espacio: Reparación eléctrica de urgencia", detalleRes.MotivoRechazo);

        // 4. Verificar que el docente recibió notificación
        var notifs = await (await SendAsync(docente, HttpMethod.Get, "/api/notificaciones?leida=false")).Content.ReadFromJsonAsync<List<NotificacionDto>>();
        Assert.Contains(notifs!, n => n.Mensaje.Contains($"#{reserva.Id}") && n.Mensaje.Contains("CANCELADA"));
    }

    [Fact]
    public async Task Reservar_EnEspacioBloqueado_Devuelve409()
    {
        var espacioId = await CrearEspacioAsync();
        var d = Dia();
        var admin = await AdminAsync();
        var docente = await DocenteAsync();

        // 1. Admin bloquea el espacio de 10:00 a 16:00
        var bloqueoReq = new CrearBloqueoRequestDto
        {
            EspacioId = espacioId,
            Desde = d.AddHours(10),
            Hasta = d.AddHours(16),
            Motivo = "Mantenimiento preventivo",
            CancelarReservasAfectadas = false
        };
        (await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", bloqueoReq)).EnsureSuccessStatusCode();

        // 2. Docente intenta reservar de 11:00 a 13:00 -> 409 Conflict
        var reservaReq = new CrearReservaRequestDto
        {
            EspacioId = espacioId,
            FechaHoraInicio = d.AddHours(11),
            FechaHoraFin = d.AddHours(13),
            TipoActividad = TipoActividad.Clase,
            AsistentesEstimados = 15,
            Motivo = "Intento durante bloqueo",
            EquipamientoRequerido = new() { new() { EquipamientoId = 1, Cantidad = 1 } }
        };
        var r = await SendAsync(docente, HttpMethod.Post, "/api/reservas", reservaReq);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Crear_PorSolicitante_Devuelve403()
    {
        var espacioId = await CrearEspacioAsync();
        var d = Dia();
        var docente = await DocenteAsync();

        var body = new CrearBloqueoRequestDto
        {
            EspacioId = espacioId,
            Desde = d.AddHours(10),
            Hasta = d.AddHours(12),
            Motivo = "Bloqueo no autorizado por solicitante"
        };

        var response = await SendAsync(docente, HttpMethod.Post, "/api/bloqueos", body);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Crear_PorResponsableDeOtroDepartamento_Devuelve403()
    {
        // Espacio asignado a departamento 2 (Básicas), responsable es de departamento 1 (Sistemas)
        var espacioId = await CrearEspacioAsync(departamentoId: 2);
        var d = Dia();
        var responsable = await ResponsableAsync();

        var body = new CrearBloqueoRequestDto
        {
            EspacioId = espacioId,
            Desde = d.AddHours(10),
            Hasta = d.AddHours(12),
            Motivo = "Bloqueo departamental ajeno"
        };

        var response = await SendAsync(responsable, HttpMethod.Post, "/api/bloqueos", body);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Sin embargo, sobre un espacio de su propio departamento (depto 1) debe funcionar (201)
        var espacioSistemas = await CrearEspacioAsync(departamentoId: 1);
        body.EspacioId = espacioSistemas;
        var okResp = await SendAsync(responsable, HttpMethod.Post, "/api/bloqueos", body);
        Assert.Equal(HttpStatusCode.Created, okResp.StatusCode);
    }

    [Fact]
    public async Task BloqueosSuperpuestos_MismoEspacio_Devuelve409()
    {
        var espacioId = await CrearEspacioAsync();
        var d = Dia();
        var admin = await AdminAsync();

        // 1. Bloqueo 10:00 a 14:00
        var b1 = new CrearBloqueoRequestDto
        {
            EspacioId = espacioId,
            Desde = d.AddHours(10),
            Hasta = d.AddHours(14),
            Motivo = "Obras Fase 1"
        };
        var r1 = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", b1);
        Assert.Equal(HttpStatusCode.Created, r1.StatusCode);

        // 2. Bloqueo superpuesto 12:00 a 16:00 -> 409
        var b2 = new CrearBloqueoRequestDto
        {
            EspacioId = espacioId,
            Desde = d.AddHours(12),
            Hasta = d.AddHours(16),
            Motivo = "Obras Fase 2 superpuestas"
        };
        var r2 = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", b2);
        Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
    }

    [Fact]
    public async Task GetBloqueos_ConFiltros_FuncionaCorrectamente()
    {
        var espacioId = await CrearEspacioAsync();
        var d = Dia();
        var admin = await AdminAsync();

        var b = new CrearBloqueoRequestDto
        {
            EspacioId = espacioId,
            Desde = d.AddHours(8),
            Hasta = d.AddHours(12),
            Motivo = "Bloqueo para filtro"
        };
        var res = await (await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", b)).Content.ReadFromJsonAsync<CrearBloqueoResponseDto>();

        // Consultar con filtro por espacioId
        var getResp = await SendAsync(admin, HttpMethod.Get, $"/api/bloqueos?espacioId={espacioId}&vigentes=true");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        var paginado = await getResp.Content.ReadFromJsonAsync<PagedResultDto<BloqueoListadoDto>>();
        Assert.NotNull(paginado);
        Assert.Contains(paginado.Items, x => x.Id == res!.Id);
    }

    [Fact]
    public async Task EliminarBloqueo_LevantaBloqueoYAudita()
    {
        var espacioId = await CrearEspacioAsync();
        var d = Dia();
        var admin = await AdminAsync();

        var b = new CrearBloqueoRequestDto
        {
            EspacioId = espacioId,
            Desde = d.AddHours(8),
            Hasta = d.AddHours(12),
            Motivo = "Bloqueo temporal a eliminar"
        };
        var res = await (await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", b)).Content.ReadFromJsonAsync<CrearBloqueoResponseDto>();

        // DELETE
        var delResp = await SendAsync(admin, HttpMethod.Delete, $"/api/bloqueos/{res!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        // Verificar que ya no existe en la base de datos y que se registró en Auditoría
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var existe = await db.Bloqueos.AnyAsync(x => x.Id == res.Id);
        Assert.False(existe);

        var audito = await db.Auditorias.AnyAsync(a => a.Entidad == "Bloqueo" && a.EntidadId == res.Id.ToString() && a.Accion == "LevantarBloqueo");
        Assert.True(audito);
    }
}
