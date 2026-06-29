using FluentValidation;
using System.Text.Json;

namespace Accounting.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(ctx, ex);
        }
    }

    private static Task WriteErrorAsync(HttpContext ctx, Exception ex)
    {
        ctx.Response.ContentType = "application/problem+json";

        if (ex is ValidationException valEx)
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errors = valEx.Errors
                .GroupBy(e => char.ToLower(e.PropertyName[0]) + e.PropertyName[1..])
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return ctx.Response.WriteAsJsonAsync(new
            {
                type = "https://httpstatuses.com/400",
                title = "Error de validación.",
                status = 400,
                errors
            });
        }

        var (status, title) = ex switch
        {
            InvalidOperationException => (StatusCodes.Status400BadRequest, ex.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor.")
        };

        ctx.Response.StatusCode = status;
        return ctx.Response.WriteAsJsonAsync(new
        {
            type = $"https://httpstatuses.com/{status}",
            title,
            status
        });
    }
}
