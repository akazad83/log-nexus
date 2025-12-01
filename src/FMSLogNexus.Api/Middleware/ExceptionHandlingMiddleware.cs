using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace FMSLogNexus.Api.Middleware;

/// <summary>
/// Middleware for handling exceptions globally.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
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
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var statusCode = GetStatusCode(exception);
        var problemDetails = CreateProblemDetails(context, exception, statusCode);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(problemDetails, options);
        await context.Response.WriteAsync(json);
    }

    private static int GetStatusCode(Exception exception) => exception switch
    {
        ArgumentException => (int)HttpStatusCode.BadRequest,
        KeyNotFoundException => (int)HttpStatusCode.NotFound,
        UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
        InvalidOperationException => (int)HttpStatusCode.Conflict,
        NotImplementedException => (int)HttpStatusCode.NotImplemented,
        OperationCanceledException => 499, // Client Closed Request
        _ => (int)HttpStatusCode.InternalServerError
    };

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception, int statusCode)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = _environment.IsDevelopment() ? exception.Message : GetGenericMessage(statusCode),
            Instance = context.Request.Path
        };

        // Add trace ID for correlation
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        // In development, include stack trace
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exception"] = exception.GetType().Name;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;

            if (exception.InnerException != null)
            {
                problemDetails.Extensions["innerException"] = exception.InnerException.Message;
            }
        }

        return problemDetails;
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        423 => "Locked",
        499 => "Client Closed Request",
        500 => "Internal Server Error",
        501 => "Not Implemented",
        503 => "Service Unavailable",
        _ => "Error"
    };

    private static string GetGenericMessage(int statusCode) => statusCode switch
    {
        400 => "The request was invalid.",
        401 => "Authentication is required.",
        403 => "You do not have permission to perform this action.",
        404 => "The requested resource was not found.",
        409 => "A conflict occurred with the current state of the resource.",
        423 => "The resource is locked.",
        500 => "An unexpected error occurred. Please try again later.",
        501 => "This feature is not implemented.",
        503 => "The service is temporarily unavailable.",
        _ => "An error occurred."
    };
}

/// <summary>
/// Extension methods for exception handling middleware.
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
