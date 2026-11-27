using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestionReservas.Application.Common.Helpers;
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

/// <summary>
/// Suite de pruebas de aceptación para la defensa del proyecto (Sección 13 de docs/ENUNCIADO.md).
/// Cada test lleva el nombre explícito del criterio de aceptación que verifica.
/// </summary>
public class CriteriosAceptacionDefensaTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static int _slotOffset = 0;

    public CriteriosAceptacionDefensaTests(CustomWebApplicationFactory factory)
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
    private Task<string> ResponsableSistemasAsync() => TokenAsync("responsable.sistemas@facultad.edu.ar", "Responsable123!");

    private Task<HttpResponseMessage> SendAsync(string token, HttpMethod method, string url, object? body = null)
    {
        var msg = new HttpRequestMessage(method, url);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body != null) msg.Content = JsonContent.Create(body);
        return _client.SendAsync(msg);
    }

    /// <summary>
    /// Devuelve una fecha futura determinista (a partir de diciembre 2026) única por invocación.
    /// </summary>
    private static DateOnly FechaDeterminista()
    {
        var offset = Interlocked.Increment(ref _slotOffset);
        return new DateOnly(2026, 12, 1).AddDays(offset);
    }

    private async Task<int> CrearEspacioAisladoAsync(int departamentoId = 2, int capacidad = 40, EstadoEspacio estado = EstadoEspacio.Disponible)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var espacio = new Espacio
        {
            Codigo = $"CA-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            Nombre = "Aula Criterio de Aceptación",
            Edificio = "Pabellón Defensa",
            Piso = 1,
            Sector = "A",
            CapacidadMaxima = capacidad,
            Estado = estado,
            DepartamentoId = departamentoId,
            TipoEspacioId = 1
        };
        espacio.Equipamientos.Add(new EspacioEquipamiento { EquipamientoId = 1, Cantidad = 2 });
        db.Espacios.Add(espacio);
        await db.SaveChangesAsync();
        return espacio.Id;
    }

    private static CrearReservaRequestDto CrearRequestReserva(int espacioId, DateOnly fecha, int horaInicio, int horaFin, int asistentes = 20) => new()
    {
        EspacioId = espacioId,
        FechaHoraInicio = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(horaInicio, 0)),
        FechaHoraFin = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(horaFin, 0)),
        TipoActividad = TipoActividad.Clase,
        AsistentesEstimados = asistentes,
        Motivo = "Actividad académica de evaluación",
        EquipamientoRequerido = new() { new() { EquipamientoId = 1, Cantidad = 1 } }
    };

    [Fact]
    public async Task CriterioAceptacion_OcupadoDe10a12_Solicitud11a13_EsRechazada()
    {
        var espacioId = await CrearEspacioAisladoAsync();
        var fecha = FechaDeterminista();
        var admin = await AdminAsync();
        var docente = await DocenteAsync();

        // 1. Reserva aprobada de 10:00 a 12:00
        var r10a12 = await SendAsync(admin, HttpMethod.Post, "/api/reservas", CrearRequestReserva(espacioId, fecha, 10, 12));
        Assert.Equal(HttpStatusCode.Created, r10a12.StatusCode);

        // 2. Solicitud superpuesta de 11:00 a 13:00 -> Rechazada con 409 Conflict
        var r11a13 = await SendAsync(docente, HttpMethod.Post, "/api/reservas", CrearRequestReserva(espacioId, fecha, 11, 13));
        Assert.Equal(HttpStatusCode.Conflict, r11a13.StatusCode);

        // 3. Solicitud en horario contiguo de 12:00 a 14:00 -> Permitida (201 Created)
        var r12a14 = await SendAsync(docente, HttpMethod.Post, "/api/reservas", CrearRequestReserva(espacioId, fecha, 12, 14));
        Assert.Equal(HttpStatusCode.Created, r12a14.StatusCode);
    }

    [Fact]
    public async Task CriterioAceptacion_ConflictoSimultaneo_SoloUnaReservaSeAprueba()
    {
        var espacioId = await CrearEspacioAisladoAsync();
        var fecha = FechaDeterminista();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();

        var pendientesIds = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            var r = await SendAsync(docente, HttpMethod.Post, "/api/reservas", CrearRequestReserva(espacioId, fecha, 10, 12));
            Assert.Equal(HttpStatusCode.Created, r.StatusCode);
            pendientesIds.Add((await r.Content.ReadFromJsonAsync<ReservaDetalleDto>())!.Id);
        }

        var respuestas = await Task.WhenAll(pendientesIds.Select(id =>
            SendAsync(admin, new HttpMethod("PATCH"), $"/api/reservas/{id}", new { accion = "aprobar" })));

        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(3, respuestas.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task CriterioAceptacion_CapacidadExcedida_SolicitudEsRechazada()
    {
        var espacioId = await CrearEspacioAisladoAsync(capacidad: 30);
        var fecha = FechaDeterminista();
        var docente = await DocenteAsync();

        var response = await SendAsync(docente, HttpMethod.Post, "/api/reservas", CrearRequestReserva(espacioId, fecha, 10, 12, asistentes: 45));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CriterioAceptacion_EspacioBloqueadoOFueraDeServicio_NoPermiteReservar()
    {
        var espacioBloqueadoId = await CrearEspacioAisladoAsync();
        var espacioFueraServicioId = await CrearEspacioAisladoAsync(estado: EstadoEspacio.FueraDeServicio);
        var fecha = FechaDeterminista();
        var admin = await AdminAsync();
        var docente = await DocenteAsync();

        var bloqueoReq = new CrearBloqueoRequestDto
        {
            EspacioId = espacioBloqueadoId,
            Desde = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(9, 0)),
            Hasta = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(13, 0)),
            Motivo = "Mantenimiento eléctrico"
        };
        var blqResp = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", bloqueoReq);
        Assert.Equal(HttpStatusCode.Created, blqResp.StatusCode);

        var resEnBloqueado = await SendAsync(docente, HttpMethod.Post, "/api/reservas", CrearRequestReserva(espacioBloqueadoId, fecha, 10, 12));
        Assert.Equal(HttpStatusCode.Conflict, resEnBloqueado.StatusCode);

        var resEnFueraServicio = await SendAsync(docente, HttpMethod.Post, "/api/reservas", CrearRequestReserva(espacioFueraServicioId, fecha, 10, 12));
        Assert.Equal(HttpStatusCode.Conflict, resEnFueraServicio.StatusCode);
    }

    [Fact]
    public async Task CriterioAceptacion_ColisionConCursadaOficial_NoPermiteReservarNiBloquear()
    {
        int aula101Id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            aula101Id = (await db.Espacios.FirstAsync(e => e.Codigo == "AULA-101")).Id;
        }

        // Próximo martes de 18:30 a 20:00 (superpuesto con cursada oficial A1011 Martes 18:00-21:00)
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var diasHastaMartes = ((int)DayOfWeek.Tuesday - (int)hoy.DayOfWeek + 7) % 7;
        var fechaMartes = hoy.AddDays(diasHastaMartes == 0 ? 7 : diasHastaMartes);
        var docente = await DocenteAsync();
        var admin = await AdminAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await AcademicTestData.AgregarAsignacionAsync(
                db, aula101Id, fechaMartes, DayOfWeek.Tuesday, new TimeOnly(18, 0), new TimeOnly(21, 0));
        }

        var resResp = await SendAsync(docente, HttpMethod.Post, "/api/reservas", new CrearReservaRequestDto
        {
            EspacioId = aula101Id,
            FechaHoraInicio = DateTimeZoneHelper.ToUtc(fechaMartes, new TimeOnly(18, 30)),
            FechaHoraFin = DateTimeZoneHelper.ToUtc(fechaMartes, new TimeOnly(20, 0)),
            TipoActividad = TipoActividad.Clase,
            AsistentesEstimados = 30,
            Motivo = "Intento de reserva sobre cursada",
            EquipamientoRequerido = new() { new() { EquipamientoId = 1, Cantidad = 1 } }
        });
        Assert.Equal(HttpStatusCode.Conflict, resResp.StatusCode);

        var blqResp = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", new CrearBloqueoRequestDto
        {
            EspacioId = aula101Id,
            Desde = DateTimeZoneHelper.ToUtc(fechaMartes, new TimeOnly(19, 0)),
            Hasta = DateTimeZoneHelper.ToUtc(fechaMartes, new TimeOnly(20, 30)),
            Motivo = "Intento de bloqueo sobre cursada"
        });
        Assert.Equal(HttpStatusCode.Conflict, blqResp.StatusCode);
    }

    [Fact]
    public async Task CriterioAceptacion_ModificarReservaAprobada_VuelveAPendienteAprobacion()
    {
        var espacioId = await CrearEspacioAisladoAsync();
        var fecha = FechaDeterminista();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();

        var crearResp = await SendAsync(docente, HttpMethod.Post, "/api/reservas", CrearRequestReserva(espacioId, fecha, 10, 12));
        var creada = await crearResp.Content.ReadFromJsonAsync<ReservaDetalleDto>();

        var aprobarResp = await SendAsync(admin, new HttpMethod("PATCH"), $"/api/reservas/{creada!.Id}", new { accion = "aprobar" });
        Assert.Equal(HttpStatusCode.OK, aprobarResp.StatusCode);

        var modResp = await SendAsync(docente, HttpMethod.Put, $"/api/reservas/{creada.Id}", new ActualizarReservaRequestDto
        {
            FechaHoraInicio = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(14, 0)),
            FechaHoraFin = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(16, 0)),
            TipoActividad = TipoActividad.Examen,
            AsistentesEstimados = 25,
            Motivo = "Cambio de horario por mesa de examen",
            EquipamientoRequerido = new() { new() { EquipamientoId = 1, Cantidad = 1 } }
        });
        Assert.Equal(HttpStatusCode.OK, modResp.StatusCode);
        var modificada = await modResp.Content.ReadFromJsonAsync<ReservaDetalleDto>();
        Assert.Equal(EstadoReserva.PendienteAprobacion, modificada!.Estado);
    }

    [Fact]
    public async Task CriterioAceptacion_BloquearEspacioConReservasFuturas_CancelaNotificaYAudita()
    {
        var espacioId = await CrearEspacioAisladoAsync();
        var fecha = FechaDeterminista();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();

        var resResp = await SendAsync(docente, HttpMethod.Post, "/api/reservas", CrearRequestReserva(espacioId, fecha, 10, 12));
        var reserva = await resResp.Content.ReadFromJsonAsync<ReservaDetalleDto>();

        // 1. Con CancelarReservasAfectadas = false -> 409 Conflict
        var blqFalse = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", new CrearBloqueoRequestDto
        {
            EspacioId = espacioId,
            Desde = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(9, 0)),
            Hasta = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(13, 0)),
            Motivo = "Obra edilicia urgente",
            CancelarReservasAfectadas = false
        });
        Assert.Equal(HttpStatusCode.Conflict, blqFalse.StatusCode);

        using var reservasJson = System.Text.Json.JsonDocument.Parse(await blqFalse.Content.ReadAsStringAsync());
        var reservasAfectadas = System.Text.Json.JsonSerializer.Deserialize<List<ReservaAfectadaDto>>(
            reservasJson.RootElement.GetProperty("reservasAfectadas").GetRawText(),
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        // 2. Confirmar explícitamente IDs y versiones antes de cancelar.
        var blqTrue = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", new CrearBloqueoRequestDto
        {
            EspacioId = espacioId,
            Desde = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(9, 0)),
            Hasta = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(13, 0)),
            Motivo = "Obra edilicia urgente",
            CancelarReservasAfectadas = true,
            ReservasConfirmadas = reservasAfectadas.Select(r => r.Id).ToList(),
            VersionesReservasConfirmadas = reservasAfectadas
                .Select(r => new ReservaConfirmadaVersionDto { Id = r.Id, Version = r.Version }).ToList()
        });
        Assert.Equal(HttpStatusCode.Created, blqTrue.StatusCode);
        var resultado = await blqTrue.Content.ReadFromJsonAsync<CrearBloqueoResponseDto>();
        Assert.Single(resultado!.ReservasCanceladas);
        Assert.Equal(reserva!.Id, resultado.ReservasCanceladas[0].Id);

        var detalle = await (await SendAsync(docente, HttpMethod.Get, $"/api/reservas/{reserva.Id}")).Content.ReadFromJsonAsync<ReservaDetalleDto>();
        Assert.Equal(EstadoReserva.Cancelada, detalle!.Estado);
        Assert.Contains(detalle.HistorialCambios, h => h.Accion == "CancelarReserva" && h.Motivo != null && h.Motivo.Contains("Cancelada por bloqueo del espacio"));

        var notifsPage = await (await SendAsync(docente, HttpMethod.Get, "/api/notificaciones?leida=false&pageSize=100")).Content.ReadFromJsonAsync<GestionReservas.Application.Common.Models.PagedResultDto<NotificacionDto>>();
        Assert.Contains(notifsPage!.Items, n => n.Mensaje.Contains($"#{reserva.Id}"));
    }

    [Fact]
    public async Task CriterioAceptacion_BajaDeEspacioConReservasFuturas_EsRechazada()
    {
        var espacioId = await CrearEspacioAisladoAsync();
        var fecha = FechaDeterminista();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();

        var resResp = await SendAsync(docente, HttpMethod.Post, "/api/reservas", CrearRequestReserva(espacioId, fecha, 10, 12));
        Assert.Equal(HttpStatusCode.Created, resResp.StatusCode);

        var deleteResp = await SendAsync(admin, HttpMethod.Delete, $"/api/espacios/{espacioId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteResp.StatusCode);
    }

    [Fact]
    public async Task CriterioAceptacion_ResponsableEnDepartamentoAjeno_Devuelve403Forbidden()
    {
        // Espacio en Departamento 2 (Ciencias Básicas); responsable.sistemas pertenece al Departamento 1 (Sistemas)
        var espacioDepto2Id = await CrearEspacioAisladoAsync(departamentoId: 2);
        var fecha = FechaDeterminista();
        var docente = await DocenteAsync();
        var responsableSistemas = await ResponsableSistemasAsync();

        var resResp = await SendAsync(docente, HttpMethod.Post, "/api/reservas", CrearRequestReserva(espacioDepto2Id, fecha, 10, 12));
        var reserva = await resResp.Content.ReadFromJsonAsync<ReservaDetalleDto>();

        var aprobarResp = await SendAsync(responsableSistemas, new HttpMethod("PATCH"), $"/api/reservas/{reserva!.Id}", new { accion = "aprobar" });
        Assert.Equal(HttpStatusCode.Forbidden, aprobarResp.StatusCode);

        var bloquearResp = await SendAsync(responsableSistemas, HttpMethod.Post, "/api/bloqueos", new CrearBloqueoRequestDto
        {
            EspacioId = espacioDepto2Id,
            Desde = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(14, 0)),
            Hasta = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(16, 0)),
            Motivo = "Intento de bloqueo en departamento ajeno"
        });
        Assert.Equal(HttpStatusCode.Forbidden, bloquearResp.StatusCode);
    }

    [Fact]
    public async Task CriterioAceptacion_OperacionSinPermisosPorSolicitante_Devuelve403Forbidden()
    {
        var espacioId = await CrearEspacioAisladoAsync();
        var fecha = FechaDeterminista();
        var docente = await DocenteAsync();

        var resResp = await SendAsync(docente, HttpMethod.Post, "/api/reservas", CrearRequestReserva(espacioId, fecha, 10, 12));
        var reserva = await resResp.Content.ReadFromJsonAsync<ReservaDetalleDto>();

        var aprobarResp = await SendAsync(docente, new HttpMethod("PATCH"), $"/api/reservas/{reserva!.Id}", new { accion = "aprobar" });
        Assert.Equal(HttpStatusCode.Forbidden, aprobarResp.StatusCode);

        var auditoriaResp = await SendAsync(docente, HttpMethod.Get, "/api/auditoria");
        Assert.Equal(HttpStatusCode.Forbidden, auditoriaResp.StatusCode);
    }

    [Fact]
    public async Task CriterioAceptacion_RechazoYCancelacionDeAprobada_ExigenMotivoYDejanAuditoria()
    {
        var espacioId = await CrearEspacioAisladoAsync();
        var fecha = FechaDeterminista();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();

        var r1Resp = await SendAsync(docente, HttpMethod.Post, "/api/reservas", CrearRequestReserva(espacioId, fecha, 8, 10));
        var r1 = await r1Resp.Content.ReadFromJsonAsync<ReservaDetalleDto>();

        // Rechazar sin motivo -> 400 Bad Request
        var rechazarSinMotivo = await SendAsync(admin, new HttpMethod("PATCH"), $"/api/reservas/{r1!.Id}", new { accion = "rechazar" });
        Assert.Equal(HttpStatusCode.BadRequest, rechazarSinMotivo.StatusCode);

        // Rechazar con motivo -> 200 OK y queda en auditoría
        var rechazarConMotivo = await SendAsync(admin, new HttpMethod("PATCH"), $"/api/reservas/{r1.Id}", new { accion = "rechazar", motivo = "Superposición con acto institucional" });
        Assert.Equal(HttpStatusCode.OK, rechazarConMotivo.StatusCode);
        var det1 = await rechazarConMotivo.Content.ReadFromJsonAsync<ReservaDetalleDto>();
        Assert.Contains(det1!.HistorialCambios, h => h.Accion == "RechazarReserva" && h.Motivo == "Superposición con acto institucional");

        // Crear y aprobar segunda reserva, luego cancelar sin motivo (400) y con motivo (200)
        var r2Resp = await SendAsync(docente, HttpMethod.Post, "/api/reservas", CrearRequestReserva(espacioId, fecha, 14, 16));
        var r2 = await r2Resp.Content.ReadFromJsonAsync<ReservaDetalleDto>();
        await SendAsync(admin, new HttpMethod("PATCH"), $"/api/reservas/{r2!.Id}", new { accion = "aprobar" });

        var cancelarSinMotivo = await SendAsync(docente, new HttpMethod("PATCH"), $"/api/reservas/{r2.Id}", new { accion = "cancelar" });
        Assert.Equal(HttpStatusCode.BadRequest, cancelarSinMotivo.StatusCode);

        var cancelarConMotivo = await SendAsync(docente, new HttpMethod("PATCH"), $"/api/reservas/{r2.Id}", new { accion = "cancelar", motivo = "Cambio de fecha de parcial" });
        Assert.Equal(HttpStatusCode.OK, cancelarConMotivo.StatusCode);
        var det2 = await cancelarConMotivo.Content.ReadFromJsonAsync<ReservaDetalleDto>();
        Assert.Contains(det2!.HistorialCambios, h => h.Accion == "CancelarReserva" && h.Motivo == "Cambio de fecha de parcial");
    }
}
