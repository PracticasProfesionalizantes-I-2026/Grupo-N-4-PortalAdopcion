namespace PortalAdopcion.API.Middleware;

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using PortalAdopcion.Shared.Exceptions;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (ValidationException ex)
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            var response = ValidationErrorTranslator.FromValidationException(ex);
            await ctx.Response.WriteAsJsonAsync(
                response,
                options: (JsonSerializerOptions?)null,
                contentType: "application/json",
                cancellationToken: ctx.RequestAborted);
        }
        catch (Exception ex) when (ex is JsonException or BadHttpRequestException)
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            var response = ValidationErrorTranslator.FromJsonException(ex);
            await ctx.Response.WriteAsJsonAsync(
                response,
                options: (JsonSerializerOptions?)null,
                contentType: "application/json",
                cancellationToken: ctx.RequestAborted);
        }
        catch (Exception ex) when (ex is ConflictException or NotFoundException or AccessException or AuthenticationException)
        {
            ctx.Response.StatusCode = ex switch
            {
                ConflictException => StatusCodes.Status409Conflict,
                NotFoundException => StatusCodes.Status404NotFound,
                AccessException => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status401Unauthorized
            };
            await ctx.Response.WriteAsJsonAsync(
                new { type = "about:blank", title = ex.Message, status = ctx.Response.StatusCode, traceId = ctx.TraceIdentifier },
                options: (JsonSerializerOptions?)null,
                contentType: "application/problem+json",
                cancellationToken: ctx.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en {Path}", ctx.Request.Path);
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await ctx.Response.WriteAsJsonAsync(
                new { type = "about:blank", title = "Error interno. Consultá el registro del servidor.", status = 500, traceId = ctx.TraceIdentifier },
                options: (JsonSerializerOptions?)null,
                contentType: "application/problem+json",
                cancellationToken: ctx.RequestAborted);
        }
    }
}
