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
        var (status, title) = ex switch
        {
            InvalidOperationException => (StatusCodes.Status400BadRequest, ex.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor.")
        };

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";

        var body = JsonSerializer.Serialize(new
        {
            type = $"https://httpstatuses.com/{status}",
            title,
            status
        });

        return ctx.Response.WriteAsync(body);
    }
}
