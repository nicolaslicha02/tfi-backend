using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestionReservas.Application.Common.Helpers;
using GestionReservas.Application.Features.Auth.DTOs;
using GestionReservas.Application.Features.Calendario.DTOs;
using GestionReservas.Domain.Entities;
using GestionReservas.Domain.Enums;
using GestionReservas.Infrastructure.Persistence;
using GestionReservas.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionReservas.Tests;

public class CalendarioEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CalendarioEndpointsTests(CustomWebApplicationFactory factory)
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

    private Task<HttpResponseMessage> SendAsync(string token, HttpMethod method, string url)
    {
        var msg = new HttpRequestMessage(method, url);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(msg);
    }

    [Fact]
    public async Task GetCalendario_SinToken_Devuelve401Unauthorized()
    {
        var response = await _client.GetAsync("/api/calendario?desde=2026-05-01T00:00:00Z&hasta=2026-05-07T23:59:59Z");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCalendario_RangoMayorA31Dias_Devuelve400BadRequest()
    {
        var token = await DocenteAsync();
        var response = await SendAsync(
            token,
            HttpMethod.Get,
            "/api/calendario?desde=2026-05-01T00:00:00Z&hasta=2026-06-05T00:00:00Z");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCalendario_HastaMenorQueDesde_Devuelve400BadRequest()
    {
        var token = await DocenteAsync();
        var response = await SendAsync(
            token,
            HttpMethod.Get,
            "/api/calendario?desde=2026-05-15T00:00:00Z&hasta=2026-05-10T00:00:00Z");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCalendario_RangoValido_DevuelveReservasBloqueosYCursadasOrdenadosPorInicio()
    {
        int espacioId;
        var fechaLunes = new DateOnly(2026, 5, 4);
        var fechaMartes = new DateOnly(2026, 5, 5);
        var fechaMiercoles = new DateOnly(2026, 5, 6);
        var fechaJueves = new DateOnly(2026, 5, 7);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var comision = await db.Comisiones.FirstAsync(c => c.Codigo == "A1011");
            var solicitante = await db.Usuarios.FirstAsync(u => u.Email == "docente.perez@facultad.edu.ar");

            var espacio = new Espacio
            {
                Codigo = $"CAL-MIX-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                Nombre = "Aula Calendario Integral",
                Edificio = "Pabellón Calendario",
                Piso = 1,
                Sector = "Norte",
                CapacidadMaxima = 60,
                Estado = EstadoEspacio.Disponible,
                DepartamentoId = 1,
                TipoEspacioId = 1
            };
            db.Espacios.Add(espacio);
            await db.SaveChangesAsync();
            espacioId = espacio.Id;

            // Cursada los martes de 18:00 a 21:00 local
            db.AsignacionesEspacio.Add(new AsignacionEspacio
            {
                EspacioId = espacioId,
                ComisionId = comision.Id,
                DiaSemana = DayOfWeek.Tuesday,
                HoraInicio = new TimeSpan(18, 0, 0),
                HoraFin = new TimeSpan(21, 0, 0)
            });

            // Reserva Aprobada el lunes 04/05 de 09:00 a 11:00 local
            db.Reservas.Add(new Reserva
            {
                EspacioId = espacioId,
                SolicitanteId = solicitante.Id,
                FechaHoraInicio = DateTimeZoneHelper.ToUtc(fechaLunes, new TimeOnly(9, 0)),
                FechaHoraFin = DateTimeZoneHelper.ToUtc(fechaLunes, new TimeOnly(11, 0)),
                TipoActividad = TipoActividad.Clase,
                AsistentesEstimados = 25,
                Motivo = "Reserva aprobada lunes",
                Estado = EstadoReserva.Aprobada,
                FechaSolicitud = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc)
            });

            // Bloqueo el miércoles 06/05 de 14:00 a 16:00 local
            db.Bloqueos.Add(new Bloqueo
            {
                EspacioId = espacioId,
                CreadoPorUsuarioId = 1,
                FechaHoraDesde = DateTimeZoneHelper.ToUtc(fechaMiercoles, new TimeOnly(14, 0)),
                FechaHoraHasta = DateTimeZoneHelper.ToUtc(fechaMiercoles, new TimeOnly(16, 0)),
                Motivo = "Mantenimiento proyector",
                FechaCreacion = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc)
            });

            // Reserva Pendiente el jueves 07/05 de 10:00 a 12:00 local
            db.Reservas.Add(new Reserva
            {
                EspacioId = espacioId,
                SolicitanteId = solicitante.Id,
                FechaHoraInicio = DateTimeZoneHelper.ToUtc(fechaJueves, new TimeOnly(10, 0)),
                FechaHoraFin = DateTimeZoneHelper.ToUtc(fechaJueves, new TimeOnly(12, 0)),
                TipoActividad = TipoActividad.Examen,
                AsistentesEstimados = 30,
                Motivo = "Seminario pendiente jueves",
                Estado = EstadoReserva.PendienteAprobacion,
                FechaSolicitud = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc)
            });

            await db.SaveChangesAsync();
        }

        var admin = await AdminAsync();
        var desde = Uri.EscapeDataString(DateTimeZoneHelper.ToUtc(fechaLunes, new TimeOnly(0, 0)).ToString("O"));
        var hasta = Uri.EscapeDataString(DateTimeZoneHelper.ToUtc(new DateOnly(2026, 5, 10), new TimeOnly(23, 59)).ToString("O"));

        var response = await SendAsync(admin, HttpMethod.Get, $"/api/calendario?desde={desde}&hasta={hasta}&espacioId={espacioId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var eventos = await response.Content.ReadFromJsonAsync<List<EventoCalendarioDto>>();
        Assert.NotNull(eventos);
        Assert.Equal(4, eventos.Count);

        // Ordenados por Inicio ascendente: Lunes (Reserva Aprobada) -> Martes (Cursada) -> Miércoles (Bloqueo) -> Jueves (Reserva Pendiente)
        Assert.Equal("Reserva", eventos[0].Tipo);
        Assert.Equal(EstadoReserva.Aprobada, eventos[0].Estado);
        Assert.Equal("Cursada", eventos[1].Tipo);
        Assert.Null(eventos[1].Estado);
        Assert.Equal(DateTimeZoneHelper.ToUtc(fechaMartes, new TimeOnly(18, 0)), eventos[1].Inicio);
        Assert.Equal("Bloqueo", eventos[2].Tipo);
        Assert.Equal("Reserva", eventos[3].Tipo);
        Assert.Equal(EstadoReserva.PendienteAprobacion, eventos[3].Estado);

        for (int i = 1; i < eventos.Count; i++)
        {
            Assert.True(eventos[i].Inicio >= eventos[i - 1].Inicio);
        }
    }

    [Fact]
    public async Task GetCalendario_CursadaSemanal_ApareceEnCadaMartesDelRango()
    {
        int aula101Id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            aula101Id = (await db.Espacios.FirstAsync(e => e.Codigo == "AULA-101")).Id;
        }

        var token = await DocenteAsync();
        // Mayo 2026 tiene 4 martes: 05/05, 12/05, 19/05 y 26/05 (rango de 29 días <= 31)
        var desde = Uri.EscapeDataString(DateTimeZoneHelper.ToUtc(new DateOnly(2026, 5, 1), new TimeOnly(0, 0)).ToString("O"));
        var hasta = Uri.EscapeDataString(DateTimeZoneHelper.ToUtc(new DateOnly(2026, 5, 30), new TimeOnly(23, 59)).ToString("O"));

        var response = await SendAsync(token, HttpMethod.Get, $"/api/calendario?desde={desde}&hasta={hasta}&espacioId={aula101Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var eventos = await response.Content.ReadFromJsonAsync<List<EventoCalendarioDto>>();
        Assert.NotNull(eventos);

        var cursadas = eventos.Where(e => e.Tipo == "Cursada").ToList();
        Assert.Equal(4, cursadas.Count);

        var martesEsperados = new[]
        {
            new DateOnly(2026, 5, 5),
            new DateOnly(2026, 5, 12),
            new DateOnly(2026, 5, 19),
            new DateOnly(2026, 5, 26)
        };

        for (int i = 0; i < martesEsperados.Length; i++)
        {
            Assert.Equal(DateTimeZoneHelper.ToUtc(martesEsperados[i], new TimeOnly(18, 0)), cursadas[i].Inicio);
            Assert.Equal(DateTimeZoneHelper.ToUtc(martesEsperados[i], new TimeOnly(21, 0)), cursadas[i].Fin);
            Assert.Contains("A1011", cursadas[i].Titulo);
        }
    }

    [Fact]
    public async Task GetCalendario_FiltrosPorEspacioYPorEdificio_FiltranCorrectamente()
    {
        var sufijo = Guid.NewGuid().ToString("N")[..6].ToUpper();
        var edificioA = $"EdificioCalA-{sufijo}";
        var edificioB = $"EdificioCalB-{sufijo}";
        int espacioAId;
        int espacioBId;
        var fecha = new DateOnly(2026, 6, 10);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var solicitante = await db.Usuarios.FirstAsync(u => u.Email == "docente.perez@facultad.edu.ar");

            var espA = new Espacio
            {
                Codigo = $"CA-{sufijo}",
                Nombre = "Espacio Edificio A",
                Edificio = edificioA,
                Piso = 1,
                Sector = "A",
                CapacidadMaxima = 30,
                Estado = EstadoEspacio.Disponible,
                DepartamentoId = 1,
                TipoEspacioId = 1
            };
            var espB = new Espacio
            {
                Codigo = $"CB-{sufijo}",
                Nombre = "Espacio Edificio B",
                Edificio = edificioB,
                Piso = 2,
                Sector = "B",
                CapacidadMaxima = 40,
                Estado = EstadoEspacio.Disponible,
                DepartamentoId = 2,
                TipoEspacioId = 1
            };
            db.Espacios.AddRange(espA, espB);
            await db.SaveChangesAsync();
            espacioAId = espA.Id;
            espacioBId = espB.Id;

            db.Reservas.AddRange(
                new Reserva
                {
                    EspacioId = espacioAId,
                    SolicitanteId = solicitante.Id,
                    FechaHoraInicio = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(9, 0)),
                    FechaHoraFin = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(11, 0)),
                    TipoActividad = TipoActividad.Clase,
                    AsistentesEstimados = 15,
                    Motivo = "Reserva en A",
                    Estado = EstadoReserva.Aprobada,
                    FechaSolicitud = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc)
                },
                new Reserva
                {
                    EspacioId = espacioBId,
                    SolicitanteId = solicitante.Id,
                    FechaHoraInicio = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(14, 0)),
                    FechaHoraFin = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(16, 0)),
                    TipoActividad = TipoActividad.Clase,
                    AsistentesEstimados = 20,
                    Motivo = "Reserva en B",
                    Estado = EstadoReserva.Aprobada,
                    FechaSolicitud = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc)
                });
            await db.SaveChangesAsync();
        }

        var token = await DocenteAsync();
        var desde = Uri.EscapeDataString(DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(0, 0)).ToString("O"));
        var hasta = Uri.EscapeDataString(DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(23, 59)).ToString("O"));

        // 1. Filtro por espacioId = espacioAId
        var resEspacio = await SendAsync(token, HttpMethod.Get, $"/api/calendario?desde={desde}&hasta={hasta}&espacioId={espacioAId}");
        Assert.Equal(HttpStatusCode.OK, resEspacio.StatusCode);
        var evEspacio = await resEspacio.Content.ReadFromJsonAsync<List<EventoCalendarioDto>>();
        Assert.NotNull(evEspacio);
        Assert.Single(evEspacio);
        Assert.Equal(espacioAId, evEspacio[0].EspacioId);

        // 2. Filtro por edificio = edificioB
        var resEdificio = await SendAsync(token, HttpMethod.Get, $"/api/calendario?desde={desde}&hasta={hasta}&edificio={Uri.EscapeDataString(edificioB)}");
        Assert.Equal(HttpStatusCode.OK, resEdificio.StatusCode);
        var evEdificio = await resEdificio.Content.ReadFromJsonAsync<List<EventoCalendarioDto>>();
        Assert.NotNull(evEdificio);
        Assert.Single(evEdificio);
        Assert.Equal(espacioBId, evEdificio[0].EspacioId);
    }

    [Fact]
    public async Task GetCalendario_SolicitanteVeReservasAjenasComoOcupadoSinDatosSensiblesYPropiasConDetalle()
    {
        int espacioId;
        int reservaAjenaId;
        int reservaPropiaId;
        var fecha = new DateOnly(2026, 6, 15);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var adminUser = await db.Usuarios.FirstAsync(u => u.Email == "admin@facultad.edu.ar");
            var docenteUser = await db.Usuarios.FirstAsync(u => u.Email == "docente.perez@facultad.edu.ar");

            var espacio = new Espacio
            {
                Codigo = $"PRIV-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                Nombre = "Espacio Privacidad Calendario",
                Edificio = "Central",
                Piso = 1,
                Sector = "Sur",
                CapacidadMaxima = 40,
                Estado = EstadoEspacio.Disponible,
                DepartamentoId = 1,
                TipoEspacioId = 1
            };
            db.Espacios.Add(espacio);
            await db.SaveChangesAsync();
            espacioId = espacio.Id;

            var ajena = new Reserva
            {
                EspacioId = espacioId,
                SolicitanteId = adminUser.Id,
                FechaHoraInicio = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(8, 0)),
                FechaHoraFin = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(10, 0)),
                TipoActividad = TipoActividad.Reunion,
                AsistentesEstimados = 10,
                Motivo = "Reunión Reservada Rectorado",
                Observaciones = "Observación privada confidencial",
                Estado = EstadoReserva.Aprobada,
                FechaSolicitud = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)
            };

            var propia = new Reserva
            {
                EspacioId = espacioId,
                SolicitanteId = docenteUser.Id,
                FechaHoraInicio = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(11, 0)),
                FechaHoraFin = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(13, 0)),
                TipoActividad = TipoActividad.Clase,
                AsistentesEstimados = 20,
                Motivo = "Clase de Consulta Propia",
                Observaciones = "Traer apuntes de práctica",
                Estado = EstadoReserva.PendienteAprobacion,
                FechaSolicitud = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)
            };

            var bloqueo = new Bloqueo
            {
                EspacioId = espacioId,
                CreadoPorUsuarioId = adminUser.Id,
                FechaHoraDesde = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(15, 0)),
                FechaHoraHasta = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(17, 0)),
                Motivo = "Reparación interna tablero eléctrico",
                FechaCreacion = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)
            };

            db.Reservas.AddRange(ajena, propia);
            db.Bloqueos.Add(bloqueo);
            await db.SaveChangesAsync();
            reservaAjenaId = ajena.Id;
            reservaPropiaId = propia.Id;
        }

        var desde = Uri.EscapeDataString(DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(0, 0)).ToString("O"));
        var hasta = Uri.EscapeDataString(DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(23, 59)).ToString("O"));
        var url = $"/api/calendario?desde={desde}&hasta={hasta}&espacioId={espacioId}";

        // 1. Consulta como Solicitante (docente.perez)
        var docenteToken = await DocenteAsync();
        var resDocente = await SendAsync(docenteToken, HttpMethod.Get, url);
        Assert.Equal(HttpStatusCode.OK, resDocente.StatusCode);
        var eventosDocente = await resDocente.Content.ReadFromJsonAsync<List<EventoCalendarioDto>>();
        Assert.NotNull(eventosDocente);
        Assert.Equal(3, eventosDocente.Count);

        var evAjenaDocente = eventosDocente.Single(e => e.Tipo == "Reserva" && e.ReferenciaId == reservaAjenaId);
        Assert.Equal("Ocupado", evAjenaDocente.Titulo);
        Assert.Null(evAjenaDocente.Solicitante);
        Assert.Null(evAjenaDocente.SolicitanteId);
        Assert.Null(evAjenaDocente.Motivo);
        Assert.Null(evAjenaDocente.Observaciones);
        Assert.Null(evAjenaDocente.TipoActividad);

        var evPropiaDocente = eventosDocente.Single(e => e.Tipo == "Reserva" && e.ReferenciaId == reservaPropiaId);
        Assert.Contains("Clase de Consulta Propia", evPropiaDocente.Titulo);
        Assert.Equal("Clase de Consulta Propia", evPropiaDocente.Motivo);
        Assert.Equal("Traer apuntes de práctica", evPropiaDocente.Observaciones);
        Assert.NotNull(evPropiaDocente.Solicitante);

        var evBloqueoDocente = eventosDocente.Single(e => e.Tipo == "Bloqueo");
        Assert.Equal("Bloqueado", evBloqueoDocente.Titulo);
        Assert.Null(evBloqueoDocente.Motivo);

        // 2. Consulta como Administrador: ve todos los motivos y datos completos
        var adminToken = await AdminAsync();
        var resAdmin = await SendAsync(adminToken, HttpMethod.Get, url);
        Assert.Equal(HttpStatusCode.OK, resAdmin.StatusCode);
        var eventosAdmin = await resAdmin.Content.ReadFromJsonAsync<List<EventoCalendarioDto>>();
        Assert.NotNull(eventosAdmin);

        var evAjenaAdmin = eventosAdmin.Single(e => e.Tipo == "Reserva" && e.ReferenciaId == reservaAjenaId);
        Assert.Equal("Reunión Reservada Rectorado", evAjenaAdmin.Motivo);
        Assert.Equal("Observación privada confidencial", evAjenaAdmin.Observaciones);
        Assert.NotNull(evAjenaAdmin.Solicitante);

        var evBloqueoAdmin = eventosAdmin.Single(e => e.Tipo == "Bloqueo");
        Assert.Equal("Reparación interna tablero eléctrico", evBloqueoAdmin.Motivo);
    }
}
