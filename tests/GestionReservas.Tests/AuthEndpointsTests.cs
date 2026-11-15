using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using GestionReservas.Application.Common.Interfaces;
using GestionReservas.Application.Features.Auth.DTOs;
using GestionReservas.Domain.Entities;
using GestionReservas.Infrastructure.Persistence;
using GestionReservas.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace GestionReservas.Tests;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private const string JwtSecret = "ClaveSuperSecretaExtremadamenteLargaParaJwtGestionDeEspacios2026!#";

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ConCredencialesValidas_Devuelve200YTokenConDatos()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "admin@facultad.edu.ar",
            Password = "Admin123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Error {response.StatusCode}: {body}");

        var content = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(content);
        Assert.False(string.IsNullOrWhiteSpace(content.Token));
        Assert.True(content.ExpiresAt > DateTime.UtcNow);
        Assert.NotNull(content.Usuario);
        Assert.Equal("admin@facultad.edu.ar", content.Usuario.Email);
        Assert.Contains("Administrador", content.Usuario.Roles);
    }

    [Fact]
    public async Task Login_ConPasswordIncorrecta_Devuelve401YMensajeGenerico()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "admin@facultad.edu.ar",
            Password = "PasswordEquivocada999!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(401, problem.Status);
        Assert.Equal("Credenciales inválidas.", problem.Detail);
    }

    [Fact]
    public async Task Login_ConUsuarioInexistente_Devuelve401YMensajeGenerico()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "usuario.inexistente@facultad.edu.ar",
            Password = "CualquierPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(401, problem.Status);
        Assert.Equal("Credenciales inválidas.", problem.Detail);
    }

    [Fact]
    public async Task Login_ConEmailInvalido_Devuelve400BadRequest()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "formato-invalido-sin-arroba",
            Password = "Admin123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task GetCurrentUser_SinToken_Devuelve401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_ConTokenInvalido_Devuelve401Unauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token-totalmente-invalido-y-falso");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_ConTokenVencido_Devuelve401Unauthorized()
    {
        // Arrange: generar un token con fecha de expiración en el pasado
        var expiredToken = GenerarTokenExpirado();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_ConTokenValido_Devuelve200YPerfilCompleto()
    {
        // 1. Obtener token válido haciendo login
        var loginReq = new LoginRequestDto
        {
            Email = "admin@facultad.edu.ar",
            Password = "Admin123!"
        };
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginData);

        // 2. Consultar /api/auth/me
        var meReq = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginData.Token);

        var meRes = await _client.SendAsync(meReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, meRes.StatusCode);

        var profile = await meRes.Content.ReadFromJsonAsync<CurrentUserProfileDto>();
        Assert.NotNull(profile);
        Assert.Equal("admin@facultad.edu.ar", profile.Email);
        Assert.Contains("Administrador", profile.Roles);
        Assert.NotEmpty(profile.Permisos);
        Assert.True(profile.Permisos.Count >= 10);
    }

    [Fact]
    public async Task GetCurrentUser_DevuelveDepartamentoRealYNullParaSolicitanteSinDepartamento()
    {
        // 1. Verificar departamento real de responsable.sistemas@facultad.edu.ar
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "responsable.sistemas@facultad.edu.ar",
            Password = "Responsable123!"
        });
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        var loginRespData = await loginResp.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginRespData);

        var meRespReq = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRespReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginRespData.Token);
        var meRespRes = await _client.SendAsync(meRespReq);
        Assert.Equal(HttpStatusCode.OK, meRespRes.StatusCode);

        var respProfile = await meRespRes.Content.ReadFromJsonAsync<CurrentUserProfileDto>();
        Assert.NotNull(respProfile);
        Assert.NotNull(respProfile.Departamento);
        Assert.Contains("Sistemas", respProfile.Departamento.Nombre);

        // 2. Verificar que un Solicitante con DepartamentoId = null devuelva Departamento == null
        var emailSolicitante = $"solicitante.sindepto.{Guid.NewGuid():N}@facultad.edu.ar";
        const string passwordSolicitante = "Solicitante123!";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var rolSolicitante = await db.Roles.FirstAsync(r => r.Nombre == "Solicitante");

            var nuevoSolicitante = new Usuario
            {
                NombreCompleto = "Solicitante Sin Departamento",
                Email = emailSolicitante,
                PasswordHash = hasher.HashPassword(passwordSolicitante),
                Activo = true,
                DepartamentoId = null,
                FechaCreacion = DateTime.UtcNow
            };
            db.Usuarios.Add(nuevoSolicitante);
            await db.SaveChangesAsync();

            db.UsuarioRoles.Add(new UsuarioRol
            {
                UsuarioId = nuevoSolicitante.Id,
                RolId = rolSolicitante.Id
            });
            await db.SaveChangesAsync();
        }

        var loginSol = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = emailSolicitante,
            Password = passwordSolicitante
        });
        Assert.Equal(HttpStatusCode.OK, loginSol.StatusCode);
        var loginSolData = await loginSol.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginSolData);

        var meSolReq = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meSolReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginSolData.Token);
        var meSolRes = await _client.SendAsync(meSolReq);
        Assert.Equal(HttpStatusCode.OK, meSolRes.StatusCode);

        var solProfile = await meSolRes.Content.ReadFromJsonAsync<CurrentUserProfileDto>();
        Assert.NotNull(solProfile);
        Assert.Null(solProfile.Departamento);
    }

    private static string GenerarTokenExpirado()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "admin@facultad.edu.ar"),
            new Claim(ClaimTypes.Role, "Administrador")
        };

        var now = DateTime.UtcNow;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            NotBefore = now.AddMinutes(-20),
            IssuedAt = now.AddMinutes(-20),
            Expires = now.AddMinutes(-10), // Expiró hace 10 minutos
            Issuer = "GestionReservasApi",
            Audience = "GestionReservasClient",
            SigningCredentials = creds
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(tokenDescriptor));
    }
}
