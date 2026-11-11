using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestionReservas.Application.Features.Auth.DTOs;
using GestionReservas.Application.Features.Notificaciones.DTOs;
using GestionReservas.Application.Features.Reservas.DTOs;
using GestionReservas.Domain.Entities;
using GestionReservas.Domain.Enums;
using GestionReservas.Infrastructure.Persistence;
using GestionReservas.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionReservas.Tests;

public class ReservasEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static int _dayCounter = 40;

    public ReservasEndpointsTests(CustomWebApplicationFactory factory)
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

    /// <summary>Día futuro único por test para evitar interferencias entre tests.</summary>
    private static DateTime Dia() => DateTime.UtcNow.Date.AddDays(Interlocked.Increment(ref _dayCounter));

    /// <summary>Crea un espacio aislado (depto 2 = Ciencias Básicas, capacidad 40, con equipamiento 1).</summary>
    private async Task<int> CrearEspacioAsync(int capacidad = 40, EstadoEspacio estado = EstadoEspacio.Disponible)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var espacio = new Espacio
        {
            Codigo = $"RT-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            Nombre = "Espacio test reservas",
            Edificio = "Test",
            Piso = 1,
            Sector = "T",
            CapacidadMaxima = capacidad,
            Estado = estado,
            DepartamentoId = 2,
            TipoEspacioId = 1
        };
        espacio.Equipamientos.Add(new EspacioEquipamiento { EquipamientoId = 1, Cantidad = 1 });
        db.Espacios.Add(espacio);
        await db.SaveChangesAsync();
        return espacio.Id;
    }

    private static CrearReservaRequestDto Req(int espacioId, DateTime inicio, DateTime fin, int asistentes = 10) => new()
    {
        EspacioId = espacioId,
        FechaHoraInicio = inicio,
        FechaHoraFin = fin,
        TipoActividad = TipoActividad.Clase,
        AsistentesEstimados = asistentes,
        Motivo = "Clase de prueba",
        EquipamientoRequerido = new() { new() { EquipamientoId = 1, Cantidad = 1 } }
    };

    private async Task<ReservaDetalleDto> CrearPendienteAsync(string token, int espacioId, DateTime inicio, DateTime fin)
    {
        var r = await SendAsync(token, HttpMethod.Post, "/api/reservas", Req(espacioId, inicio, fin));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<ReservaDetalleDto>())!;
    }

    [Fact]
    public async Task Crear_Valida_Devuelve201Pendiente()
    {
        var espacio = await CrearEspacioAsync();
        var d = Dia();
        var res = await CrearPendienteAsync(await DocenteAsync(), espacio, d.AddHours(10), d.AddHours(12));
        Assert.Equal(EstadoReserva.PendienteAprobacion, res.Estado);
    }

    [Fact]
    public async Task Superposicion_ConAprobada_Devuelve409_YHorarioContiguoOk()
    {
        var espacio = await CrearEspacioAsync();
        var d = Dia();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();

        // Admin crea auto-aprobada 10-12
        var aprob = await SendAsync(admin, HttpMethod.Post, "/api/reservas", Req(espacio, d.AddHours(10), d.AddHours(12)));
        Assert.Equal(HttpStatusCode.Created, aprob.StatusCode);
        Assert.Equal(EstadoReserva.Aprobada, (await aprob.Content.ReadFromJsonAsync<ReservaDetalleDto>())!.Estado);

        // Admin intenta otra auto-aprobada 11-13 -> 409
        var conflicto = await SendAsync(admin, HttpMethod.Post, "/api/reservas", Req(espacio, d.AddHours(11), d.AddHours(13)));
        Assert.Equal(HttpStatusCode.Conflict, conflicto.StatusCode);

        // Pendiente del docente 11-13 sobre horario ocupado -> 409 (se valida contra aprobadas)
        var docConf = await SendAsync(docente, HttpMethod.Post, "/api/reservas", Req(espacio, d.AddHours(11), d.AddHours(13)));
        Assert.Equal(HttpStatusCode.Conflict, docConf.StatusCode);

        // 12-14 -> OK
        var ok = await SendAsync(docente, HttpMethod.Post, "/api/reservas", Req(espacio, d.AddHours(12), d.AddHours(14)));
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
    }

    [Fact]
    public async Task Crear_CapacidadExcedida_Devuelve400()
    {
        var espacio = await CrearEspacioAsync(capacidad: 20);
        var d = Dia();
        var r = await SendAsync(await DocenteAsync(), HttpMethod.Post, "/api/reservas", Req(espacio, d.AddHours(10), d.AddHours(12), asistentes: 21));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Crear_EspacioBloqueado_Devuelve409()
    {
        var espacio = await CrearEspacioAsync();
        var d = Dia();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var adminId = await db.Usuarios.Where(u => u.Email == "admin@facultad.edu.ar").Select(u => u.Id).FirstAsync();
            db.Bloqueos.Add(new Bloqueo
            {
                EspacioId = espacio,
                FechaHoraDesde = d.AddHours(8),
                FechaHoraHasta = d.AddHours(20),
                Motivo = "Mantenimiento",
                CreadoPorUsuarioId = adminId
            });
            await db.SaveChangesAsync();
        }

        var r = await SendAsync(await DocenteAsync(), HttpMethod.Post, "/api/reservas", Req(espacio, d.AddHours(10), d.AddHours(12)));
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Rechazar_SinMotivo_400_YConMotivoQuedaEnDetalle()
    {
        var espacio = await CrearEspacioAsync();
        var d = Dia();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();
        var res = await CrearPendienteAsync(docente, espacio, d.AddHours(10), d.AddHours(12));

        var sinMotivo = await SendAsync(admin, new HttpMethod("PATCH"), $"/api/reservas/{res.Id}", new { accion = "rechazar" });
        Assert.Equal(HttpStatusCode.BadRequest, sinMotivo.StatusCode);

        var conMotivo = await SendAsync(admin, new HttpMethod("PATCH"), $"/api/reservas/{res.Id}", new { accion = "rechazar", motivo = "Sin disponibilidad de personal" });
        Assert.Equal(HttpStatusCode.OK, conMotivo.StatusCode);

        var detalle = await (await SendAsync(docente, HttpMethod.Get, $"/api/reservas/{res.Id}")).Content.ReadFromJsonAsync<ReservaDetalleDto>();
        Assert.Equal(EstadoReserva.Rechazada, detalle!.Estado);
        Assert.Equal("Sin disponibilidad de personal", detalle.MotivoRechazo);
        Assert.Contains(detalle.HistorialCambios, h => h.Motivo == "Sin disponibilidad de personal");

        // Notificación al solicitante, consultable y marcable como leída
        var notifs = await (await SendAsync(docente, HttpMethod.Get, "/api/notificaciones?leida=false")).Content.ReadFromJsonAsync<List<NotificacionDto>>();
        var n = notifs!.First(x => x.Mensaje.Contains($"#{res.Id}"));
        var leer = await SendAsync(docente, new HttpMethod("PATCH"), $"/api/notificaciones/{n.Id}/leer");
        Assert.Equal(HttpStatusCode.OK, leer.StatusCode);

        // Transición inválida: aprobar una rechazada -> 409
        var invalida = await SendAsync(admin, new HttpMethod("PATCH"), $"/api/reservas/{res.Id}", new { accion = "aprobar" });
        Assert.Equal(HttpStatusCode.Conflict, invalida.StatusCode);
    }

    [Fact]
    public async Task Aprobar_PorSolicitante_Devuelve403()
    {
        var espacio = await CrearEspacioAsync();
        var d = Dia();
        var docente = await DocenteAsync();
        var res = await CrearPendienteAsync(docente, espacio, d.AddHours(10), d.AddHours(12));
        var r = await SendAsync(docente, new HttpMethod("PATCH"), $"/api/reservas/{res.Id}", new { accion = "aprobar" });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Aprobar_PorResponsableDeOtroDepartamento_Devuelve403()
    {
        var espacio = await CrearEspacioAsync(); // depto 2; responsable es de depto 1
        var d = Dia();
        var res = await CrearPendienteAsync(await DocenteAsync(), espacio, d.AddHours(10), d.AddHours(12));
        var r = await SendAsync(await ResponsableAsync(), new HttpMethod("PATCH"), $"/api/reservas/{res.Id}", new { accion = "aprobar" });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Modificar_Aprobada_VuelveAPendiente()
    {
        var espacio = await CrearEspacioAsync();
        var d = Dia();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();
        var res = await CrearPendienteAsync(docente, espacio, d.AddHours(10), d.AddHours(12));
        (await SendAsync(admin, new HttpMethod("PATCH"), $"/api/reservas/{res.Id}", new { accion = "aprobar" })).EnsureSuccessStatusCode();

        var put = await SendAsync(docente, HttpMethod.Put, $"/api/reservas/{res.Id}", new ActualizarReservaRequestDto
        {
            FechaHoraInicio = d.AddHours(14),
            FechaHoraFin = d.AddHours(16),
            TipoActividad = TipoActividad.Clase,
            AsistentesEstimados = 15,
            Motivo = "Cambio de horario"
        });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal(EstadoReserva.PendienteAprobacion, (await put.Content.ReadFromJsonAsync<ReservaDetalleDto>())!.Estado);
    }

    [Fact]
    public async Task Aprobaciones_EnParalelo_SoloUnaQuedaAprobada()
    {
        var espacio = await CrearEspacioAsync();
        var d = Dia();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();

        var ids = new List<int>();
        for (var i = 0; i < 5; i++)
        {
            // Todas superpuestas entre sí (10:00-12:00 desplazadas minutos)
            ids.Add((await CrearPendienteAsync(docente, espacio, d.AddHours(10).AddMinutes(i * 10), d.AddHours(12).AddMinutes(i * 10))).Id);
        }

        var respuestas = await Task.WhenAll(ids.Select(id =>
            SendAsync(admin, new HttpMethod("PATCH"), $"/api/reservas/{id}", new { accion = "aprobar" })));

        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(4, respuestas.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task MisReservas_SoloDevuelveLasPropias()
    {
        var espacio = await CrearEspacioAsync();
        var d = Dia();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();
        var propia = await CrearPendienteAsync(docente, espacio, d.AddHours(10), d.AddHours(11));
        var ajena = await CrearPendienteAsync(admin, await CrearEspacioAsync(), d.AddHours(10), d.AddHours(11));

        var r = await SendAsync(docente, HttpMethod.Get, "/api/mis-reservas?pageSize=100");
        var page = await r.Content.ReadFromJsonAsync<GestionReservas.Application.Common.Models.PagedResultDto<ReservaListadoDto>>();
        Assert.Contains(page!.Items, x => x.Id == propia.Id);
        Assert.DoesNotContain(page.Items, x => x.Id == ajena.Id);
    }
}
