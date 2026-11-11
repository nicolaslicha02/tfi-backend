using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Common.Models;
using GestionReservas.Application.Features.Auth.DTOs;
using GestionReservas.Application.Features.Catalogos.DTOs;
using GestionReservas.Application.Features.Usuarios.DTOs;
using GestionReservas.Domain.Entities;
using GestionReservas.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionReservas.Tests;

public class UsuariosAndCatalogosEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public UsuariosAndCatalogosEndpointsTests(CustomWebApplicationFactory factory)
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
    public async Task Administrador_CreaUsuario_Devuelve201YDetalle()
    {
        // Arrange
        var tokenAdmin = await ObtenerTokenAsync("admin@facultad.edu.ar", "Admin123!");
        var uniqueEmail = $"nuevo.docente.{Guid.NewGuid().ToString("N")[..8]}@facultad.edu.ar";

        var request = new CrearUsuarioRequestDto
        {
            Nombre = "Juan",
            Apellido = "Pérez",
            Email = uniqueEmail,
            Telefono = "+54 11 5555-1234",
            Password = "DocenteSecure2026!#",
            Roles = new List<string> { "Solicitante" }
        };

        var message = new HttpRequestMessage(HttpMethod.Post, "/api/usuarios")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);

        // Act
        var response = await _client.SendAsync(message);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var usuarioCreado = await response.Content.ReadFromJsonAsync<UsuarioDetalleDto>();
        Assert.NotNull(usuarioCreado);
        Assert.True(usuarioCreado.Id > 0);
        Assert.Equal("Juan", usuarioCreado.Nombre);
        Assert.Equal("Pérez", usuarioCreado.Apellido);
        Assert.Equal(uniqueEmail, usuarioCreado.Email);
        Assert.True(usuarioCreado.Activo);
        Assert.Contains("Solicitante", usuarioCreado.Roles);
    }

    [Fact]
    public async Task CrearUsuario_ConEmailDuplicado_Devuelve409Conflict()
    {
        // Arrange
        var tokenAdmin = await ObtenerTokenAsync("admin@facultad.edu.ar", "Admin123!");
        var request = new CrearUsuarioRequestDto
        {
            Nombre = "Admin",
            Apellido = "Duplicado",
            Email = "admin@facultad.edu.ar", // Ya existe en el seed
            Password = "PasswordValido123!",
            Roles = new List<string> { "Administrador" }
        };

        var message = new HttpRequestMessage(HttpMethod.Post, "/api/usuarios")
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
    public async Task CrearUsuario_ConPasswordDebil_Devuelve400BadRequest()
    {
        // Arrange
        var tokenAdmin = await ObtenerTokenAsync("admin@facultad.edu.ar", "Admin123!");
        var request = new CrearUsuarioRequestDto
        {
            Nombre = "Usuario",
            Apellido = "Test",
            Email = $"test.debil.{Guid.NewGuid().ToString("N")[..8]}@facultad.edu.ar",
            Password = "debil", // Menos de 8 caracteres, sin mayúscula ni símbolo
            Roles = new List<string> { "Solicitante" }
        };

        var message = new HttpRequestMessage(HttpMethod.Post, "/api/usuarios")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);

        // Act
        var response = await _client.SendAsync(message);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UsuarioSinPermiso_IntentaCrearUsuario_Devuelve403Forbidden()
    {
        // Arrange: Solicitante (docente.perez@facultad.edu.ar) no tiene permiso usuarios.gestionar
        var tokenSolicitante = await ObtenerTokenAsync("docente.perez@facultad.edu.ar", "Docente123!");
        var request = new CrearUsuarioRequestDto
        {
            Nombre = "Hacker",
            Apellido = "User",
            Email = "hacker@facultad.edu.ar",
            Password = "PasswordSeguro123!",
            Roles = new List<string> { "Administrador" }
        };

        var message = new HttpRequestMessage(HttpMethod.Post, "/api/usuarios")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenSolicitante);

        // Act
        var response = await _client.SendAsync(message);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UsuarioSinPermiso_IntentaModificarUsuario_Devuelve403Forbidden()
    {
        // Arrange
        var tokenSolicitante = await ObtenerTokenAsync("docente.perez@facultad.edu.ar", "Docente123!");
        var updateRequest = new ActualizarUsuarioRequestDto
        {
            Nombre = "NombreModificado"
        };

        var message = new HttpRequestMessage(HttpMethod.Patch, "/api/usuarios/1")
        {
            Content = JsonContent.Create(updateRequest)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenSolicitante);

        // Act
        var response = await _client.SendAsync(message);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UsuariosEndpoints_SinToken_Devuelve401Unauthorized()
    {
        // Act
        var responseGet = await _client.GetAsync("/api/usuarios");
        var responsePost = await _client.PostAsJsonAsync("/api/usuarios", new CrearUsuarioRequestDto());

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, responseGet.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, responsePost.StatusCode);
    }

    [Fact]
    public async Task GetUsuarios_ConFiltrosYPaginacion_DevuelveResultadosPaginados()
    {
        // Arrange
        var tokenAdmin = await ObtenerTokenAsync("admin@facultad.edu.ar", "Admin123!");
        var message = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios?page=1&pageSize=2");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);

        // Act
        var response = await _client.SendAsync(message);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResultDto<UsuarioListadoDto>>();
        Assert.NotNull(pagedResult);
        Assert.True(pagedResult.Total >= 4); // Al menos los 4 usuarios sembrados
        Assert.Equal(2, pagedResult.Items.Count);
        Assert.Equal(1, pagedResult.Page);
        Assert.Equal(2, pagedResult.PageSize);

        // Probar filtro por texto
        var filterMessage = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios?search=Carlos");
        filterMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var filterResponse = await _client.SendAsync(filterMessage);

        Assert.Equal(HttpStatusCode.OK, filterResponse.StatusCode);
        var filterResult = await filterResponse.Content.ReadFromJsonAsync<PagedResultDto<UsuarioListadoDto>>();
        Assert.NotNull(filterResult);
        Assert.Contains(filterResult.Items, u => u.Email.Contains("responsable.sistemas"));
    }

    [Fact]
    public async Task Admin_NoPuedeDesactivarseASiMismo_Devuelve409Conflict()
    {
        // Arrange: Loguearse como admin y obtener su propio id
        var tokenAdmin = await ObtenerTokenAsync("admin@facultad.edu.ar", "Admin123!");
        var meMessage = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var meResponse = await _client.SendAsync(meMessage);
        var profile = await meResponse.Content.ReadFromJsonAsync<CurrentUserProfileDto>();
        Assert.NotNull(profile);

        // Act: Intentar auto-desactivarse
        var patchMessage = new HttpRequestMessage(HttpMethod.Patch, $"/api/usuarios/{profile.Id}")
        {
            Content = JsonContent.Create(new ActualizarUsuarioRequestDto
            {
                Activo = false
            })
        };
        patchMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);

        var response = await _client.SendAsync(patchMessage);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UsuarioDesactivado_NoPuedeLoguearse_Devuelve401Unauthorized()
    {
        // Arrange: Administrador crea usuario temporal y luego lo desactiva
        var tokenAdmin = await ObtenerTokenAsync("admin@facultad.edu.ar", "Admin123!");
        var email = $"temporal.desactivar.{Guid.NewGuid().ToString("N")[..8]}@facultad.edu.ar";
        var password = "PasswordValido123!";

        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/usuarios")
        {
            Content = JsonContent.Create(new CrearUsuarioRequestDto
            {
                Nombre = "Temporal",
                Apellido = "Desactivar",
                Email = email,
                Password = password,
                Roles = new List<string> { "Solicitante" }
            })
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var createRes = await _client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var usuario = await createRes.Content.ReadFromJsonAsync<UsuarioDetalleDto>();
        Assert.NotNull(usuario);

        // Desactivar usuario
        var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/usuarios/{usuario.Id}")
        {
            Content = JsonContent.Create(new ActualizarUsuarioRequestDto
            {
                Activo = false
            })
        };
        patchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var patchRes = await _client.SendAsync(patchReq);
        Assert.Equal(HttpStatusCode.OK, patchRes.StatusCode);

        // Act: Intentar login con usuario inactivo
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = email,
            Password = password
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task OperacionesDeUsuarios_RegistranAuditoriaEnBaseDeDatos()
    {
        // Arrange
        var tokenAdmin = await ObtenerTokenAsync("admin@facultad.edu.ar", "Admin123!");
        var email = $"audit.test.{Guid.NewGuid().ToString("N")[..8]}@facultad.edu.ar";

        // Crear usuario
        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/usuarios")
        {
            Content = JsonContent.Create(new CrearUsuarioRequestDto
            {
                Nombre = "Audit",
                Apellido = "Tester",
                Email = email,
                Password = "PasswordSeguro123!",
                Roles = new List<string> { "Solicitante" }
            })
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var createRes = await _client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var usuarioCreado = await createRes.Content.ReadFromJsonAsync<UsuarioDetalleDto>();
        Assert.NotNull(usuarioCreado);

        // Modificar usuario
        var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/usuarios/{usuarioCreado.Id}")
        {
            Content = JsonContent.Create(new ActualizarUsuarioRequestDto
            {
                Nombre = "AuditRenombrado"
            })
        };
        patchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var patchRes = await _client.SendAsync(patchReq);
        Assert.Equal(HttpStatusCode.OK, patchRes.StatusCode);

        // Assert: Consultar auditoría directamente en base de datos
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var auditorias = await db.Auditorias
            .Where(a => a.Entidad == "Usuario" && a.EntidadId == usuarioCreado.Id.ToString())
            .ToListAsync();

        Assert.NotEmpty(auditorias);
        Assert.Contains(auditorias, a => a.Accion == "CrearUsuario");
        Assert.Contains(auditorias, a => a.Accion == "ModificarUsuario");

        // Ningún registro de auditoría debe contener texto con la contraseña o password
        Assert.All(auditorias, a =>
        {
            Assert.DoesNotContain("PasswordSeguro123!", a.Motivo ?? string.Empty);
            Assert.DoesNotContain("PasswordSeguro123!", a.Detalle ?? string.Empty);
        });
    }

    [Fact]
    public async Task Catalogos_DevuelvenDatosDelSeed()
    {
        // Arrange: Requiere token autenticado
        var token = await ObtenerTokenAsync("docente.perez@facultad.edu.ar", "Docente123!");

        // 1. Departamentos
        var deptosReq = new HttpRequestMessage(HttpMethod.Get, "/api/departamentos");
        deptosReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deptosRes = await _client.SendAsync(deptosReq);
        Assert.Equal(HttpStatusCode.OK, deptosRes.StatusCode);
        var deptos = await deptosRes.Content.ReadFromJsonAsync<List<DepartamentoDto>>();
        Assert.NotNull(deptos);
        Assert.Equal(3, deptos.Count);
        Assert.Contains(deptos, d => d.Nombre.Contains("Sistemas"));

        // 2. Equipamientos
        var equipReq = new HttpRequestMessage(HttpMethod.Get, "/api/equipamientos");
        equipReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var equipRes = await _client.SendAsync(equipReq);
        Assert.Equal(HttpStatusCode.OK, equipRes.StatusCode);
        var equipamientos = await equipRes.Content.ReadFromJsonAsync<List<EquipamientoDto>>();
        Assert.NotNull(equipamientos);
        Assert.Equal(8, equipamientos.Count);
        Assert.Contains(equipamientos, e => e.Nombre.Contains("Proyector"));

        // 3. Tipos de Espacio
        var tiposReq = new HttpRequestMessage(HttpMethod.Get, "/api/tipos-espacio");
        tiposReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var tiposRes = await _client.SendAsync(tiposReq);
        Assert.Equal(HttpStatusCode.OK, tiposRes.StatusCode);
        var tipos = await tiposRes.Content.ReadFromJsonAsync<List<TipoEspacioDto>>();
        Assert.NotNull(tipos);
        Assert.Equal(4, tipos.Count);
        Assert.Contains(tipos, t => t.Nombre.Contains("Laboratorio"));
    }
}
