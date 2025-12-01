using System.Diagnostics;

namespace FMSLogNexus.Api.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;

        // Log request
        LogRequest(context, requestId);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            LogResponse(context, requestId, stopwatch.ElapsedMilliseconds);
        }
    }

    private void LogRequest(HttpContext context, string requestId)
    {
        var request = context.Request;

        _logger.LogInformation(
            "HTTP Request {RequestId}: {Method} {Path}{QueryString} from {RemoteIp} User-Agent: {UserAgent}",
            requestId,
            request.Method,
            request.Path,
            request.QueryString,
            GetClientIpAddress(context),
            request.Headers.UserAgent.ToString());
    }

    private void LogResponse(HttpContext context, string requestId, long elapsedMs)
    {
        var statusCode = context.Response.StatusCode;
        var logLevel = statusCode >= 500 ? LogLevel.Error :
                       statusCode >= 400 ? LogLevel.Warning :
                       LogLevel.Information;

        _logger.Log(
            logLevel,
            "HTTP Response {RequestId}: {StatusCode} in {ElapsedMs}ms",
            requestId,
            statusCode,
            elapsedMs);
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}

/// <summary>
/// Extension methods for request logging middleware.
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
