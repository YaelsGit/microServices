using System.Text.Json;
using SharedModels.Dtos;

namespace CatalogService.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var correlationId = context.Items.TryGetValue("CorrelationId", out var value)
                ? value?.ToString()
                : "unknown";

            _logger.LogError(
                exception,
                "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
                correlationId,
                context.Request.Path,
                context.Request.Method
            );

            await HandleExceptionAsync(context, exception, correlationId ?? "unknown");
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = "An internal server error occurred. Please contact support.",
            Data = null,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response);
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        return context.Response.WriteAsync(json);
    }
}
