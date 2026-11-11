using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestionReservas.Application.Common.Helpers;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Auth.DTOs;
using GestionReservas.Application.Features.Espacios.DTOs;
using GestionReservas.Domain.Entities;
using GestionReservas.Domain.Enums;
using GestionReservas.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionReservas.Tests;

public class EspaciosEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public EspaciosEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> ObtenerTokenAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = email,
            Password = password
        });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(result);
        return result.Token;
    }

    [Fact]
    public async Task Administrador_CrudEspacios_FuncionaCorrectamente()
    {
        // Arrange
        var tokenAdmin = await ObtenerTokenAsync("admin@facultad.edu.ar", "Admin123!");
        var uniqueCode = $"AULA-TEST-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        // 1. CREATE (POST)
        var createRequest = new CrearEspacioRequestDto
        {
            Codigo = uniqueCode,
            Nombre = "Aula de Pruebas Automatizadas",
            TipoEspacioId = 1, // Aula Teórica
            Edificio = "Edificio Central",
            Piso = 2,
            Sector = "Ala Norte",
            CapacidadMaxima = 45,
            DepartamentoId = 1, // Sistemas
            Estado = EstadoEspacio.Disponible,
            Equipamientos = new List<EspacioEquipamientoInputDto>
            {
                new() { EquipamientoId = 1, Cantidad = 1, Especificacion = "Proyector Láser" }
            }
        };

        var postMsg = new HttpRequestMessage(HttpMethod.Post, "/api/espacios")
        {
            Content = JsonContent.Create(createRequest)
        };
        postMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);

        var postResponse = await _client.SendAsync(postMsg);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        Assert.NotNull(postResponse.Headers.Location);

        var created = await postResponse.Content.ReadFromJsonAsync<EspacioDetalleDto>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal(uniqueCode, created.Codigo);
        Assert.Single(created.Equipamientos);

        // 2. READ (GET by Id)
        var getMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/espacios/{created.Id}");
        getMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var getResponse = await _client.SendAsync(getMsg);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<EspacioDetalleDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("Aula de Pruebas Automatizadas", fetched.Nombre);

        // 3. UPDATE (PUT)
        var updateRequest = new ActualizarEspacioRequestDto
        {
            Codigo = uniqueCode,
            Nombre = "Aula Modificada",
            TipoEspacioId = 1,
            Edificio = "Edificio Central",
            Piso = 2,
            Sector = "Ala Norte",
            CapacidadMaxima = 55,
            DepartamentoId = 1,
            Estado = EstadoEspacio.Disponible,
            Equipamientos = new List<EspacioEquipamientoInputDto>
            {
                new() { EquipamientoId = 1, Cantidad = 1, Especificacion = "Proyector 4K" },
                new() { EquipamientoId = 7, Cantidad = 2, Especificacion = "Aire Acondicionado" }
            }
        };

        var putMsg = new HttpRequestMessage(HttpMethod.Put, $"/api/espacios/{created.Id}")
        {
            Content = JsonContent.Create(updateRequest)
        };
        putMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var putResponse = await _client.SendAsync(putMsg);

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var updated = await putResponse.Content.ReadFromJsonAsync<EspacioDetalleDto>();
        Assert.NotNull(updated);
        Assert.Equal("Aula Modificada", updated.Nombre);
        Assert.Equal(55, updated.CapacidadMaxima);
        Assert.Equal(2, updated.Equipamientos.Count);

        // 4. DELETE (Soft delete)
        var delMsg = new HttpRequestMessage(HttpMethod.Delete, $"/api/espacios/{created.Id}");
        delMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var delResponse = await _client.SendAsync(delMsg);

        Assert.Equal(HttpStatusCode.NoContent, delResponse.StatusCode);

        // 5. Verificar que el detalle devuelva 404 una vez dado de baja
        var getAfterDelMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/espacios/{created.Id}");
        getAfterDelMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var getAfterDelResponse = await _client.SendAsync(getAfterDelMsg);

        Assert.Equal(HttpStatusCode.NotFound, getAfterDelResponse.StatusCode);
    }

    [Fact]
    public async Task CrearEspacio_ConCodigoDuplicado_Devuelve409Conflict()
    {
        // Arrange
        var tokenAdmin = await ObtenerTokenAsync("admin@facultad.edu.ar", "Admin123!");

        var request = new CrearEspacioRequestDto
        {
            Codigo = "AULA-101", // Ya existe en el seed
            Nombre = "Aula Duplicada",
            TipoEspacioId = 1,
            Edificio = "Central",
            Piso = 1,
            Sector = "Norte",
            CapacidadMaxima = 30,
            DepartamentoId = 1
        };

        var message = new HttpRequestMessage(HttpMethod.Post, "/api/espacios")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);

        // Act
        var response = await _client.SendAsync(message);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Responsable_CreaYEditaEspacioDeSuDepartamento_OK_YDeOtroDepartamento_Devuelve403()
    {
        // Arrange: Carlos Gutiérrez (responsable.sistemas) pertenece al DepartamentoId = 1 (Sistemas)
        var tokenResp = await ObtenerTokenAsync("responsable.sistemas@facultad.edu.ar", "Responsable123!");
        var uniqueCode = $"LAB-SIS-AUT-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        // 1. Intentar crear espacio para Departamento 2 (Ciencias Básicas) -> 403 Forbidden
        var reqOtroDepto = new CrearEspacioRequestDto
        {
            Codigo = uniqueCode + "-X",
            Nombre = "Laboratorio No Permitido",
            TipoEspacioId = 2,
            Edificio = "Tecnologías",
            Piso = 1,
            Sector = "Este",
            CapacidadMaxima = 20,
            DepartamentoId = 2 // No es de su depto!
        };

        var msgOtroDepto = new HttpRequestMessage(HttpMethod.Post, "/api/espacios")
        {
            Content = JsonContent.Create(reqOtroDepto)
        };
        msgOtroDepto.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResp);
        var resOtroDepto = await _client.SendAsync(msgOtroDepto);

        Assert.Equal(HttpStatusCode.Forbidden, resOtroDepto.StatusCode);

        // 2. Crear espacio para SU propio departamento (DepartamentoId = 1) -> 201 Created
        var reqSuDepto = new CrearEspacioRequestDto
        {
            Codigo = uniqueCode,
            Nombre = "Laboratorio de Redes Avanzadas",
            TipoEspacioId = 2,
            Edificio = "Tecnologías",
            Piso = 1,
            Sector = "Ala Este",
            CapacidadMaxima = 28,
            DepartamentoId = 1 // Su departamento
        };

        var msgSuDepto = new HttpRequestMessage(HttpMethod.Post, "/api/espacios")
        {
            Content = JsonContent.Create(reqSuDepto)
        };
        msgSuDepto.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResp);
        var resSuDepto = await _client.SendAsync(msgSuDepto);

        Assert.Equal(HttpStatusCode.Created, resSuDepto.StatusCode);
        var creado = await resSuDepto.Content.ReadFromJsonAsync<EspacioDetalleDto>();
        Assert.NotNull(creado);

        // 3. Modificar su propio espacio -> 200 OK
        var reqUpdate = new ActualizarEspacioRequestDto
        {
            Codigo = uniqueCode,
            Nombre = "Laboratorio Redes Renombrado",
            TipoEspacioId = 2,
            Edificio = "Tecnologías",
            Piso = 1,
            Sector = "Ala Este",
            CapacidadMaxima = 30,
            DepartamentoId = 1
        };

        var msgUpdate = new HttpRequestMessage(HttpMethod.Put, $"/api/espacios/{creado.Id}")
        {
            Content = JsonContent.Create(reqUpdate)
        };
        msgUpdate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResp);
        var resUpdate = await _client.SendAsync(msgUpdate);

        Assert.Equal(HttpStatusCode.OK, resUpdate.StatusCode);

        // 4. Intentar modificar espacio de otro departamento (AULA-101 pertenece a Departamento 2) -> 403 Forbidden
        // En el seed AULA-101 tiene ID = 1
        var msgEditOtro = new HttpRequestMessage(HttpMethod.Put, "/api/espacios/1")
        {
            Content = JsonContent.Create(new ActualizarEspacioRequestDto
            {
                Codigo = "AULA-101",
                Nombre = "Intento Hack AULA-101",
                TipoEspacioId = 1,
                Edificio = "Central",
                Piso = 1,
                Sector = "Norte",
                CapacidadMaxima = 120,
                DepartamentoId = 2
            })
        };
        msgEditOtro.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResp);
        var resEditOtro = await _client.SendAsync(msgEditOtro);

        Assert.Equal(HttpStatusCode.Forbidden, resEditOtro.StatusCode);
    }

    [Fact]
    public async Task Solicitante_IntentaOperacionesDeEscritura_Devuelve403()
    {
        // Arrange: Martín Pérez (docente.perez@facultad.edu.ar) solo tiene espacios.ver
        var tokenSolicitante = await ObtenerTokenAsync("docente.perez@facultad.edu.ar", "Docente123!");

        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/espacios")
        {
            Content = JsonContent.Create(new CrearEspacioRequestDto
            {
                Codigo = "AULA-HACK",
                Nombre = "No autorizada",
                TipoEspacioId = 1,
                Edificio = "Central",
                Piso = 1,
                Sector = "Norte",
                CapacidadMaxima = 30,
                DepartamentoId = 1
            })
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenSolicitante);
        var createRes = await _client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Forbidden, createRes.StatusCode);

        var putReq = new HttpRequestMessage(HttpMethod.Put, "/api/espacios/1")
        {
            Content = JsonContent.Create(new ActualizarEspacioRequestDto
            {
                Codigo = "AULA-101",
                Nombre = "Cambio no permitido",
                TipoEspacioId = 1,
                Edificio = "Central",
                Piso = 1,
                Sector = "Norte",
                CapacidadMaxima = 120,
                DepartamentoId = 2
            })
        };
        putReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenSolicitante);
        var putRes = await _client.SendAsync(putReq);
        Assert.Equal(HttpStatusCode.Forbidden, putRes.StatusCode);

        var delReq = new HttpRequestMessage(HttpMethod.Delete, "/api/espacios/1");
        delReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenSolicitante);
        var delRes = await _client.SendAsync(delReq);
        Assert.Equal(HttpStatusCode.Forbidden, delRes.StatusCode);
    }

    [Fact]
    public async Task EspaciosEndpoints_SinToken_Devuelve401Unauthorized()
    {
        var getRes = await _client.GetAsync("/api/espacios");
        var postRes = await _client.PostAsJsonAsync("/api/espacios", new CrearEspacioRequestDto());
        var dispRes = await _client.GetAsync("/api/espacios/disponibles?fecha=2026-11-20&horaInicio=10:00&horaFin=12:00");

        Assert.Equal(HttpStatusCode.Unauthorized, getRes.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, postRes.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, dispRes.StatusCode);
    }

    [Fact]
    public async Task BajaLogica_ConReservasFuturasAprobadasOPendientes_Devuelve409Conflict()
    {
        // Arrange
        var tokenAdmin = await ObtenerTokenAsync("admin@facultad.edu.ar", "Admin123!");
        int espacioId;

        // Crear espacio y reserva futura aprobada
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var espacio = new Espacio
            {
                Codigo = $"CONF-DEL-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                Nombre = "Espacio con Reserva Futura",
                Edificio = "Edificio A",
                Piso = 1,
                Sector = "Norte",
                CapacidadMaxima = 40,
                DepartamentoId = 1,
                TipoEspacioId = 1,
                Estado = EstadoEspacio.Disponible
            };
            db.Espacios.Add(espacio);
            await db.SaveChangesAsync();
            espacioId = espacio.Id;

            var reservaFutura = new Reserva
            {
                EspacioId = espacio.Id,
                SolicitanteId = 1,
                FechaHoraInicio = DateTime.UtcNow.AddDays(5),
                FechaHoraFin = DateTime.UtcNow.AddDays(5).AddHours(2),
                TipoActividad = TipoActividad.Clase,
                AsistentesEstimados = 25,
                Motivo = "Clase de prueba con reserva futura",
                Estado = EstadoReserva.Aprobada,
                FechaSolicitud = DateTime.UtcNow
            };
            db.Reservas.Add(reservaFutura);
            await db.SaveChangesAsync();
        }

        // Act: Intentar dar de baja el espacio
        var delMsg = new HttpRequestMessage(HttpMethod.Delete, $"/api/espacios/{espacioId}");
        delMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var delResponse = await _client.SendAsync(delMsg);

        // Assert: 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, delResponse.StatusCode);
    }

    [Fact]
    public async Task Disponibles_ConCapacidadYEquipamiento_FiltraAdecuadamente()
    {
        // Arrange: Pedimos para 60 personas + Proyector (eq 1) el próximo martes de 18:00 a 21:00
        var token = await ObtenerTokenAsync("docente.perez@facultad.edu.ar", "Docente123!");

        // Próximo martes a las 18:00
        var fecha = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var horaInicio = "18:00";
        var horaFin = "21:00";

        var url = $"/api/espacios/disponibles?fecha={fecha:yyyy-MM-dd}&horaInicio={horaInicio}&horaFin={horaFin}&capacidadMinima=60&equipamientoIds=1";

        var message = new HttpRequestMessage(HttpMethod.Get, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(message);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var disponibles = await response.Content.ReadFromJsonAsync<List<EspacioListadoDto>>();
        Assert.NotNull(disponibles);

        // AULA-101 (capacidad 120, proyector) y AULA-201 (capacidad 60, proyector) deben calificar
        Assert.Contains(disponibles, e => e.Codigo == "AULA-101");
        Assert.Contains(disponibles, e => e.Codigo == "AULA-201");

        // Espacios con capacidad < 60 o sin proyector NO deben aparecer
        Assert.DoesNotContain(disponibles, e => e.CapacidadMaxima < 60);
        Assert.All(disponibles, e =>
        {
            Assert.Contains(e.Equipamientos, eq => eq.EquipamientoId == 1);
        });
    }

    [Fact]
    public async Task Disponibles_ConReservaAprobada10A12_NoApareceEn11A13_PeroSiApareceEn12A14()
    {
        // Arrange
        var token = await ObtenerTokenAsync("docente.perez@facultad.edu.ar", "Docente123!");
        var fecha = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));
        int espacioId;

        // Crear espacio y reserva aprobada de 10:00 a 12:00 local facultad
        var inicio10Local = new TimeOnly(10, 0);
        var fin12Local = new TimeOnly(12, 0);
        var inicioUtc = DateTimeZoneHelper.ToUtc(fecha, inicio10Local);
        var finUtc = DateTimeZoneHelper.ToUtc(fecha, fin12Local);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var espacio = new Espacio
            {
                Codigo = $"DISP-RES-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                Nombre = "Espacio para Prueba de Superposición",
                Edificio = "Central",
                Piso = 1,
                Sector = "Norte",
                CapacidadMaxima = 50,
                DepartamentoId = 1,
                TipoEspacioId = 1,
                Estado = EstadoEspacio.Disponible
            };
            db.Espacios.Add(espacio);
            await db.SaveChangesAsync();
            espacioId = espacio.Id;

            var reserva = new Reserva
            {
                EspacioId = espacio.Id,
                SolicitanteId = 1,
                FechaHoraInicio = inicioUtc,
                FechaHoraFin = finUtc,
                TipoActividad = TipoActividad.Clase,
                AsistentesEstimados = 30,
                Motivo = "Clase 10-12",
                Estado = EstadoReserva.Aprobada,
                FechaSolicitud = DateTime.UtcNow
            };
            db.Reservas.Add(reserva);
            await db.SaveChangesAsync();
        }

        // Act 1: Consultar de 11:00 a 13:00 (se superpone con 10-12) -> NO debe aparecer
        var urlSuperpuesta = $"/api/espacios/disponibles?fecha={fecha:yyyy-MM-dd}&horaInicio=11:00&horaFin=13:00";
        var msgSuperpuesto = new HttpRequestMessage(HttpMethod.Get, urlSuperpuesta);
        msgSuperpuesto.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resSuperpuesta = await _client.SendAsync(msgSuperpuesto);

        Assert.Equal(HttpStatusCode.OK, resSuperpuesta.StatusCode);
        var itemsSuperpuesta = await resSuperpuesta.Content.ReadFromJsonAsync<List<EspacioListadoDto>>();
        Assert.NotNull(itemsSuperpuesta);
        Assert.DoesNotContain(itemsSuperpuesta, e => e.Id == espacioId);

        // Act 2: Consultar de 12:00 a 14:00 (rango contiguo pegado, finA == inicioB) -> SÍ debe aparecer
        var urlContigua = $"/api/espacios/disponibles?fecha={fecha:yyyy-MM-dd}&horaInicio=12:00&horaFin=14:00";
        var msgContiguo = new HttpRequestMessage(HttpMethod.Get, urlContigua);
        msgContiguo.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resContigua = await _client.SendAsync(msgContiguo);

        Assert.Equal(HttpStatusCode.OK, resContigua.StatusCode);
        var itemsContigua = await resContigua.Content.ReadFromJsonAsync<List<EspacioListadoDto>>();
        Assert.NotNull(itemsContigua);
        Assert.Contains(itemsContigua, e => e.Id == espacioId);
    }

    [Fact]
    public async Task Disponibles_ConBloqueoOFueraDeServicio_NoAparecenEnResultado()
    {
        // Arrange
        var token = await ObtenerTokenAsync("docente.perez@facultad.edu.ar", "Docente123!");
        var fecha = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15));

        int espacioBloqueadoId;
        int espacioFueraServicioId;

        var inicioUtc = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(14, 0));
        var finUtc = DateTimeZoneHelper.ToUtc(fecha, new TimeOnly(18, 0));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            // Espacio 1: Con bloqueo
            var espacio1 = new Espacio
            {
                Codigo = $"BLOQ-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                Nombre = "Espacio Bloqueado",
                Edificio = "Central",
                Piso = 1,
                Sector = "Norte",
                CapacidadMaxima = 30,
                DepartamentoId = 1,
                TipoEspacioId = 1,
                Estado = EstadoEspacio.Disponible
            };
            db.Espacios.Add(espacio1);
            await db.SaveChangesAsync();
            espacioBloqueadoId = espacio1.Id;

            db.Bloqueos.Add(new Bloqueo
            {
                EspacioId = espacio1.Id,
                CreadoPorUsuarioId = 1,
                FechaHoraDesde = inicioUtc,
                FechaHoraHasta = finUtc,
                Motivo = "Pintura y mantenimiento"
            });

            // Espacio 2: Fuera de servicio
            var espacio2 = new Espacio
            {
                Codigo = $"FDS-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                Nombre = "Espacio Fuera De Servicio",
                Edificio = "Central",
                Piso = 1,
                Sector = "Norte",
                CapacidadMaxima = 30,
                DepartamentoId = 1,
                TipoEspacioId = 1,
                Estado = EstadoEspacio.FueraDeServicio
            };
            db.Espacios.Add(espacio2);
            await db.SaveChangesAsync();
            espacioFueraServicioId = espacio2.Id;
        }

        // Act: Consultar de 15:00 a 17:00
        var url = $"/api/espacios/disponibles?fecha={fecha:yyyy-MM-dd}&horaInicio=15:00&horaFin=17:00";
        var message = new HttpRequestMessage(HttpMethod.Get, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(message);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var disponibles = await response.Content.ReadFromJsonAsync<List<EspacioListadoDto>>();
        Assert.NotNull(disponibles);

        Assert.DoesNotContain(disponibles, e => e.Id == espacioBloqueadoId);
        Assert.DoesNotContain(disponibles, e => e.Id == espacioFueraServicioId);
    }

    [Fact]
    public async Task GetEspacios_ConFiltrosYPaginacion_FuncionaCorrectamente()
    {
        // Arrange
        var token = await ObtenerTokenAsync("docente.perez@facultad.edu.ar", "Docente123!");

        // Act: Consultar página 1, tamaño 3
        var url = "/api/espacios?page=1&pageSize=3";
        var message = new HttpRequestMessage(HttpMethod.Get, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(message);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedResultDto<EspacioListadoDto>>();
        Assert.NotNull(paged);
        Assert.True(paged.Total >= 6); // Al menos los 6 espacios sembrados
        Assert.Equal(3, paged.Items.Count);
        Assert.Equal(1, paged.Page);
        Assert.Equal(3, paged.PageSize);

        // Filtro por texto "Magna"
        var urlSearch = "/api/espacios?search=Magna";
        var msgSearch = new HttpRequestMessage(HttpMethod.Get, urlSearch);
        msgSearch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resSearch = await _client.SendAsync(msgSearch);
        Assert.Equal(HttpStatusCode.OK, resSearch.StatusCode);
        var searchPaged = await resSearch.Content.ReadFromJsonAsync<PagedResultDto<EspacioListadoDto>>();
        Assert.NotNull(searchPaged);
        Assert.Contains(searchPaged.Items, e => e.Codigo == "AULA-101");
    }
}
