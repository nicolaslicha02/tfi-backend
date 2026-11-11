using System.Net;
using System.Text.Json;
using GestionReservas.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace GestionReservas.Api.Middlewares;

/// <summary>
/// Middleware global para captura y estandarización de excepciones según RFC 7807 (ProblemDetails).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path,
            Type = "https://httpstatuses.com/"
        };

        switch (exception)
        {
            case ValidationException valEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Title = "Error de validación";
                problemDetails.Detail = valEx.Message;
                problemDetails.Type += "400";
                problemDetails.Extensions["errors"] = valEx.Errors;
                _logger.LogWarning(valEx, "Error de validación en {Path}", context.Request.Path);
                break;

            case UnauthorizedException unauthEx:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                problemDetails.Status = (int)HttpStatusCode.Unauthorized;
                problemDetails.Title = "No autorizado";
                problemDetails.Detail = unauthEx.Message;
                problemDetails.Type += "401";
                _logger.LogWarning(unauthEx, "Acceso no autorizado en {Path}", context.Request.Path);
                break;

            case ForbiddenException forbiddenEx:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                problemDetails.Status = (int)HttpStatusCode.Forbidden;
                problemDetails.Title = "Acceso denegado";
                problemDetails.Detail = forbiddenEx.Message;
                problemDetails.Type += "403";
                _logger.LogWarning(forbiddenEx, "Acceso prohibido en {Path}", context.Request.Path);
                break;

            case NotFoundException notFoundEx:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                problemDetails.Status = (int)HttpStatusCode.NotFound;
                problemDetails.Title = "Recurso no encontrado";
                problemDetails.Detail = notFoundEx.Message;
                problemDetails.Type += "404";
                _logger.LogInformation(notFoundEx, "Recurso no encontrado en {Path}", context.Request.Path);
                break;

            case ConflictException conflictEx:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                problemDetails.Status = (int)HttpStatusCode.Conflict;
                problemDetails.Title = "Conflicto con el estado actual del recurso";
                problemDetails.Detail = conflictEx.Message;
                problemDetails.Type += "409";
                _logger.LogWarning(conflictEx, "Conflicto en {Path}", context.Request.Path);
                break;

            default:
                _logger.LogError(exception, "Excepción no controlada procesando la solicitud {Path}", context.Request.Path);
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                problemDetails.Title = "Error interno del servidor";
                problemDetails.Detail = (_env.IsDevelopment() || _env.EnvironmentName == "Testing")
                    ? exception.ToString()
                    : "Ocurrió un error inesperado al procesar su solicitud. Comuníquese con el administrador del sistema.";
                problemDetails.Type += "500";
                break;
        }

        context.Response.ContentType = "application/problem+json";
        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(json);
    }
}
