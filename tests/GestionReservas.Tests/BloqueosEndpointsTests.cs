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

    private async Task<List<ReservaAfectadaDto>> ObtenerReservasAfectadasAsync(
        string admin,
        CrearBloqueoRequestDto request)
    {
        request.CancelarReservasAfectadas = false;
        var response = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return JsonSerializer.Deserialize<List<ReservaAfectadaDto>>(
            json.RootElement.GetProperty("reservasAfectadas").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    private static void ConfirmarReservas(CrearBloqueoRequestDto request, IEnumerable<ReservaAfectadaDto> reservas)
    {
        var confirmadas = reservas.ToList();
        request.CancelarReservasAfectadas = true;
        request.ReservasConfirmadas = confirmadas.Select(r => r.Id).ToList();
        request.VersionesReservasConfirmadas = confirmadas
            .Select(r => new ReservaConfirmadaVersionDto { Id = r.Id, Version = r.Version })
            .ToList();
    }

    private async Task<ReservaDetalleDto> CrearReservaAsync(string docente, int espacioId, DateTime desde, DateTime hasta)
    {
        var response = await SendAsync(docente, HttpMethod.Post, "/api/reservas", new CrearReservaRequestDto
        {
            EspacioId = espacioId,
            FechaHoraInicio = desde,
            FechaHoraFin = hasta,
            TipoActividad = TipoActividad.Clase,
            AsistentesEstimados = 20,
            Motivo = "Reserva de prueba para bloqueo",
            EquipamientoRequerido = new() { new() { EquipamientoId = 1, Cantidad = 1 } }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ReservaDetalleDto>())!;
    }

    private async Task<(int Bloqueos, int AuditoriasReserva, int AuditoriasBloqueo, int Notificaciones, string Estados)> ObtenerEfectosAsync(int espacioId, IReadOnlyCollection<int> reservaIds)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var bloqueos = await db.Bloqueos.CountAsync(x => x.EspacioId == espacioId);
        var codigoEspacio = await db.Espacios.Where(x => x.Id == espacioId).Select(x => x.Codigo).SingleAsync();
        var auditoriasBloqueo = await db.Auditorias.CountAsync(x =>
            x.Accion == "CrearBloqueo" && x.Detalle != null && x.Detalle.Contains($"espacio '{codigoEspacio}'"));
        var idsTexto = reservaIds.Select(id => id.ToString()).ToArray();
        var auditorias = await db.Auditorias.CountAsync(x => x.Entidad == "Reserva" && idsTexto.Contains(x.EntidadId!));
        var mensajes = await db.Notificaciones.Select(x => x.Mensaje).ToListAsync();
        var notificaciones = mensajes.Count(mensaje => reservaIds.Any(id => mensaje.Contains($"#{id} ")));
        var estados = await db.Reservas.Where(x => reservaIds.Contains(x.Id))
            .OrderBy(x => x.Id).Select(x => new { x.Id, x.Estado }).ToListAsync();
        var estadosSnapshot = string.Join(",", estados.Select(x => $"{x.Id}:{x.Estado}"));
        return (bloqueos, auditorias, auditoriasBloqueo, notificaciones, estadosSnapshot);
    }

    private async Task<EstadoReserva> ObtenerEstadoAsync(int reservaId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Reservas.Where(x => x.Id == reservaId).Select(x => x.Estado).SingleAsync();
    }

    private static CrearBloqueoRequestDto CrearRequestBloqueo(int espacioId, DateTime desde, DateTime hasta) => new()
    {
        EspacioId = espacioId,
        Desde = desde,
        Hasta = hasta,
        Motivo = "Bloqueo de prueba",
        CancelarReservasAfectadas = true
    };

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

        var efectosAntes = await ObtenerEfectosAsync(espacioId, [reservaCreada!.Id]);
        var conflictResp = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", bloqueoReq);
        Assert.Equal(HttpStatusCode.Conflict, conflictResp.StatusCode);
        Assert.Equal(efectosAntes, await ObtenerEfectosAsync(espacioId, [reservaCreada.Id]));

        // Verificar que la respuesta JSON incluya la lista de reservas afectadas
        var content = await conflictResp.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        Assert.True(jsonDoc.RootElement.TryGetProperty("reservasAfectadas", out var afectadasElem));
        Assert.Equal(JsonValueKind.Array, afectadasElem.ValueKind);
        Assert.True(afectadasElem.GetArrayLength() > 0);
        Assert.Contains(afectadasElem.EnumerateArray(), elem => elem.GetProperty("id").GetInt32() == reservaCreada.Id);
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
        var noAfectada = await CrearReservaAsync(docente, espacioId, d.AddHours(19), d.AddHours(20));

        // 2. Crear bloqueo con CancelarReservasAfectadas = true
        var bloqueoReq = new CrearBloqueoRequestDto
        {
            EspacioId = espacioId,
            Desde = d.AddHours(13),
            Hasta = d.AddHours(18),
            Motivo = "Reparación eléctrica de urgencia",
            CancelarReservasAfectadas = true
        };

        var reservasMostradas = await ObtenerReservasAfectadasAsync(admin, bloqueoReq);
        ConfirmarReservas(bloqueoReq, reservasMostradas);

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
        var detalleNoAfectada = await (await SendAsync(docente, HttpMethod.Get, $"/api/reservas/{noAfectada.Id}")).Content.ReadFromJsonAsync<ReservaDetalleDto>();
        Assert.Equal(EstadoReserva.PendienteAprobacion, detalleNoAfectada!.Estado);

        // 4. Verificar que el docente recibió notificación
        var notifsPage = await (await SendAsync(docente, HttpMethod.Get, "/api/notificaciones?leida=false&pageSize=100")).Content.ReadFromJsonAsync<PagedResultDto<NotificacionDto>>();
        Assert.Contains(notifsPage!.Items, n => n.Mensaje.Contains($"#{reserva.Id}") && n.Mensaje.Contains("CANCELADA"));
    }

    [Fact]
    public async Task Crear_ConNuevaReservaDesdeLaConfirmacion_Devuelve409SinEfectosYActualizaLista()
    {
        var espacioId = await CrearEspacioAsync();
        var d = Dia();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();
        var primera = await CrearReservaAsync(docente, espacioId, d.AddHours(10), d.AddHours(12));
        var request = CrearRequestBloqueo(espacioId, d.AddHours(9), d.AddHours(13));
        var mostradas = await ObtenerReservasAfectadasAsync(admin, request);
        var nueva = await CrearReservaAsync(docente, espacioId, d.AddHours(11), d.AddHours(13));
        ConfirmarReservas(request, mostradas);

        var idsReserva = new[] { primera.Id, nueva.Id };
        var efectosAntes = await ObtenerEfectosAsync(espacioId, idsReserva);
        var response = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var actualizadas = await LeerAfectadasAsync(response);
        Assert.Equal(2, actualizadas.Count);
        Assert.Contains(actualizadas, x => x.Id == primera.Id);
        Assert.Contains(actualizadas, x => x.Id != primera.Id);
        Assert.Contains("revis", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reservasConfirmadas", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(efectosAntes, await ObtenerEfectosAsync(espacioId, idsReserva));
        Assert.Equal(EstadoReserva.PendienteAprobacion, await ObtenerEstadoAsync(primera.Id));
        Assert.Equal(EstadoReserva.PendienteAprobacion, await ObtenerEstadoAsync(nueva.Id));
    }

    [Fact]
    public async Task Crear_ReservaConfirmadaYaNoAfectada_Devuelve409ConListaVaciaYSinEfectos()
    {
        var espacioId = await CrearEspacioAsync();
        var d = Dia();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();
        var reserva = await CrearReservaAsync(docente, espacioId, d.AddHours(10), d.AddHours(12));
        var request = CrearRequestBloqueo(espacioId, d.AddHours(9), d.AddHours(13));
        var mostradas = await ObtenerReservasAfectadasAsync(admin, request);
        var mover = await SendAsync(docente, HttpMethod.Put, $"/api/reservas/{reserva.Id}", new ActualizarReservaRequestDto
        {
            FechaHoraInicio = d.AddHours(15), FechaHoraFin = d.AddHours(17), TipoActividad = TipoActividad.Clase,
            AsistentesEstimados = 20, Motivo = "Reserva movida fuera del rango",
            EquipamientoRequerido = new() { new() { EquipamientoId = 1, Cantidad = 1 } }
        });
        Assert.Equal(HttpStatusCode.OK, mover.StatusCode);
        ConfirmarReservas(request, mostradas);
        var efectosAntes = await ObtenerEfectosAsync(espacioId, mostradas.Select(x => x.Id).ToArray());

        var response = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Empty(await LeerAfectadasAsync(response));
        Assert.Equal(efectosAntes, await ObtenerEfectosAsync(espacioId, mostradas.Select(x => x.Id).ToArray()));
    }

    [Fact]
    public async Task Crear_ReservaConfirmadaCambioEstadoAunqueSigaAfectada_Devuelve409PorVersionObsoleta()
    {
        var espacioId = await CrearEspacioAsync();
        var d = Dia();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();
        var reserva = await CrearReservaAsync(docente, espacioId, d.AddHours(10), d.AddHours(12));
        var request = CrearRequestBloqueo(espacioId, d.AddHours(9), d.AddHours(13));
        var mostradas = await ObtenerReservasAfectadasAsync(admin, request);
        var aprobar = await SendAsync(admin, new HttpMethod("PATCH"), $"/api/reservas/{reserva.Id}", new { accion = "aprobar" });
        Assert.Equal(HttpStatusCode.OK, aprobar.StatusCode);
        ConfirmarReservas(request, mostradas);
        var efectosAntes = await ObtenerEfectosAsync(espacioId, mostradas.Select(x => x.Id).ToArray());

        var response = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var actualizadas = await LeerAfectadasAsync(response);
        Assert.Single(actualizadas);
        Assert.Equal(EstadoReserva.Aprobada, actualizadas[0].Estado);
        Assert.NotEqual(mostradas[0].Version, actualizadas[0].Version);
        Assert.Equal(efectosAntes, await ObtenerEfectosAsync(espacioId, mostradas.Select(x => x.Id).ToArray()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Crear_IdentificadorConfirmadoNoPositivo_Devuelve400(int id)
    {
        var espacioId = await CrearEspacioAsync();
        var d = Dia();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();
        var reserva = await CrearReservaAsync(docente, espacioId, d.AddHours(10), d.AddHours(12));
        var request = CrearRequestBloqueo(espacioId, d.AddHours(9), d.AddHours(13));
        request.ReservasConfirmadas = [id];
        var efectosAntes = await ObtenerEfectosAsync(espacioId, [reserva.Id]);
        var response = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(efectosAntes, await ObtenerEfectosAsync(espacioId, [reserva.Id]));
    }

    [Theory]
    [InlineData("inexistente")]
    [InlineData("otro-espacio")]
    public async Task Crear_IdentificadorNoAfectado_Devuelve409ListaActualSinEfectos(string tipoId)
    {
        var espacioId = await CrearEspacioAsync();
        var otroEspacioId = await CrearEspacioAsync();
        var d = Dia();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();
        var afectada = await CrearReservaAsync(docente, espacioId, d.AddHours(10), d.AddHours(12));
        var otra = await CrearReservaAsync(docente, otroEspacioId, d.AddHours(10), d.AddHours(12));
        var request = CrearRequestBloqueo(espacioId, d.AddHours(9), d.AddHours(13));
        var mostradas = await ObtenerReservasAfectadasAsync(admin, request);
        request.CancelarReservasAfectadas = true;
        request.ReservasConfirmadas = [afectada.Id, tipoId == "inexistente" ? int.MaxValue : otra.Id];
        request.VersionesReservasConfirmadas = mostradas.Select(x => new ReservaConfirmadaVersionDto { Id = x.Id, Version = x.Version }).ToList();
        var efectosAntes = await ObtenerEfectosAsync(espacioId, [afectada.Id]);

        var response = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Single(await LeerAfectadasAsync(response));
        Assert.Equal(efectosAntes, await ObtenerEfectosAsync(espacioId, [afectada.Id]));
    }

    [Fact]
    public async Task Crear_TrueSinConfirmaciones_ConAfectadas409_SinAfectadas201()
    {
        var espacioId = await CrearEspacioAsync();
        var d = Dia();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();
        var reserva = await CrearReservaAsync(docente, espacioId, d.AddHours(10), d.AddHours(12));
        var conAfectada = CrearRequestBloqueo(espacioId, d.AddHours(9), d.AddHours(13));
        var efectosAntes = await ObtenerEfectosAsync(espacioId, [reserva.Id]);

        var rechazado = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", conAfectada);
        Assert.Equal(HttpStatusCode.Conflict, rechazado.StatusCode);
        Assert.Single(await LeerAfectadasAsync(rechazado));
        Assert.Equal(efectosAntes, await ObtenerEfectosAsync(espacioId, [reserva.Id]));

        var sinAfectadas = CrearRequestBloqueo(espacioId, d.AddHours(14), d.AddHours(16));
        sinAfectadas.ReservasConfirmadas = [];
        var creado = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", sinAfectadas);
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
    }

    [Fact]
    public async Task Crear_ListaConfirmadaDesordenadaYDuplicada_SeAcepta()
    {
        var espacioId = await CrearEspacioAsync();
        var d = Dia();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();
        var una = await CrearReservaAsync(docente, espacioId, d.AddHours(10), d.AddHours(12));
        var dos = await CrearReservaAsync(docente, espacioId, d.AddHours(12), d.AddHours(14));
        var request = CrearRequestBloqueo(espacioId, d.AddHours(9), d.AddHours(15));
        var mostradas = await ObtenerReservasAfectadasAsync(admin, request);
        var porId = mostradas.ToDictionary(x => x.Id);
        request.CancelarReservasAfectadas = true;
        request.ReservasConfirmadas = [dos.Id, una.Id, dos.Id];
        request.VersionesReservasConfirmadas = [
            new() { Id = dos.Id, Version = porId[dos.Id].Version },
            new() { Id = una.Id, Version = porId[una.Id].Version },
            new() { Id = dos.Id, Version = porId[dos.Id].Version }
        ];

        var response = await SendAsync(admin, HttpMethod.Post, "/api/bloqueos", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creado = await response.Content.ReadFromJsonAsync<CrearBloqueoResponseDto>();
        Assert.Equal(new[] { una.Id, dos.Id }.Order(), creado!.ReservasCanceladas.Select(x => x.Id).Order());
    }

    private static async Task<List<ReservaAfectadaDto>> LeerAfectadasAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return JsonSerializer.Deserialize<List<ReservaAfectadaDto>>(
            json.RootElement.GetProperty("reservasAfectadas").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    [Theory]
    [InlineData("crear")]
    [InlineData("modificar")]
    [InlineData("aprobar")]
    [InlineData("cancelar")]
    [InlineData("rechazar")]
    public async Task Concurrencia_BloqueoYOperacionReserva_SeSerializanSin500(string operacion)
    {
        var espacioId = await CrearEspacioAsync();
        var d = Dia();
        var docente = await DocenteAsync();
        var admin = await AdminAsync();
        var reserva = await CrearReservaAsync(docente, espacioId, d.AddHours(10), d.AddHours(12));
        var requestBloqueo = CrearRequestBloqueo(espacioId, d.AddHours(9), d.AddHours(13));
        var mostradas = await ObtenerReservasAfectadasAsync(admin, requestBloqueo);
        ConfirmarReservas(requestBloqueo, mostradas);

        var inicio = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpResponseMessage> AlIniciar(Func<Task<HttpResponseMessage>> accion)
        {
            await inicio.Task;
            return await accion();
        }

        Task<HttpResponseMessage> operacionReserva = operacion switch
        {
            "crear" => AlIniciar(() => SendAsync(docente, HttpMethod.Post, "/api/reservas", new CrearReservaRequestDto
            {
                EspacioId = espacioId, FechaHoraInicio = d.AddHours(10), FechaHoraFin = d.AddHours(11),
                TipoActividad = TipoActividad.Clase, AsistentesEstimados = 10, Motivo = "Alta simultánea",
                EquipamientoRequerido = new() { new() { EquipamientoId = 1, Cantidad = 1 } }
            })),
            "modificar" => AlIniciar(() => SendAsync(docente, HttpMethod.Put, $"/api/reservas/{reserva.Id}", new ActualizarReservaRequestDto
            {
                FechaHoraInicio = d.AddHours(15), FechaHoraFin = d.AddHours(17), TipoActividad = TipoActividad.Clase,
                AsistentesEstimados = 20, Motivo = "Modificación simultánea",
                EquipamientoRequerido = new() { new() { EquipamientoId = 1, Cantidad = 1 } }
            })),
            "aprobar" => AlIniciar(() => SendAsync(admin, new HttpMethod("PATCH"), $"/api/reservas/{reserva.Id}", new { accion = "aprobar" })),
            "cancelar" => AlIniciar(() => SendAsync(admin, new HttpMethod("PATCH"), $"/api/reservas/{reserva.Id}", new { accion = "cancelar" })),
            "rechazar" => AlIniciar(() => SendAsync(admin, new HttpMethod("PATCH"), $"/api/reservas/{reserva.Id}", new { accion = "rechazar", motivo = "Rechazo simultáneo" })),
            _ => throw new ArgumentOutOfRangeException(nameof(operacion))
        };
        var crearBloqueo = AlIniciar(() => SendAsync(admin, HttpMethod.Post, "/api/bloqueos", requestBloqueo));

        inicio.SetResult();
        var responses = await Task.WhenAll(operacionReserva, crearBloqueo);

        Assert.DoesNotContain(responses, r => r.StatusCode == HttpStatusCode.InternalServerError);
        Assert.Contains(responses[1].StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.Conflict });
        Assert.Contains(responses[0].StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.Conflict });
        Assert.False(responses[0].IsSuccessStatusCode && responses[1].StatusCode == HttpStatusCode.Created);

        if (responses[1].StatusCode == HttpStatusCode.Conflict)
        {
            var efectos = await ObtenerEfectosAsync(espacioId, [reserva.Id]);
            Assert.Equal(0, efectos.Bloqueos);
            Assert.Equal(0, efectos.AuditoriasBloqueo);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(await db.Auditorias.AnyAsync(x => x.Entidad == "Reserva" && x.EntidadId == reserva.Id.ToString() &&
                                                           x.Motivo != null && x.Motivo.StartsWith("Cancelada por bloqueo del espacio:")));
            Assert.False(await db.Notificaciones.AnyAsync(x => x.Mensaje.Contains($"#{reserva.Id} ") &&
                                                               x.Mensaje.Contains("CANCELADA debido a un bloqueo")));
        }
        else
        {
            Assert.Equal(EstadoReserva.Cancelada, await ObtenerEstadoAsync(reserva.Id));
        }
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
