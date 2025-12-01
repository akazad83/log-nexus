using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace FMSLogNexus.Api.Middleware;

/// <summary>
/// Simple in-memory rate limiting middleware.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IMemoryCache _cache;
    private readonly RateLimitingOptions _options;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IMemoryCache cache,
        RateLimitingOptions options)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var clientKey = GetClientKey(context);
        var endpoint = GetEndpointKey(context);
        var cacheKey = $"rate_limit:{clientKey}:{endpoint}";

        var requestCount = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _options.WindowDuration;
            return new RequestCounter { Count = 0, WindowStart = DateTime.UtcNow };
        });

        if (requestCount == null)
        {
            await _next(context);
            return;
        }

        requestCount.Count++;

        // Get limit for this endpoint
        var limit = GetLimitForEndpoint(context);

        if (requestCount.Count > limit)
        {
            _logger.LogWarning(
                "Rate limit exceeded for {ClientKey} on {Endpoint}. Count: {Count}, Limit: {Limit}",
                clientKey, endpoint, requestCount.Count, limit);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.Append("Retry-After", GetRetryAfterSeconds(requestCount).ToString());
            context.Response.Headers.Append("X-RateLimit-Limit", limit.ToString());
            context.Response.Headers.Append("X-RateLimit-Remaining", "0");
            context.Response.Headers.Append("X-RateLimit-Reset", GetResetTimestamp(requestCount).ToString());

            await context.Response.WriteAsJsonAsync(new
            {
                Status = 429,
                Title = "Too Many Requests",
                Detail = $"Rate limit exceeded. Try again in {GetRetryAfterSeconds(requestCount)} seconds."
            });
            return;
        }

        // Add rate limit headers
        context.Response.Headers.Append("X-RateLimit-Limit", limit.ToString());
        context.Response.Headers.Append("X-RateLimit-Remaining", Math.Max(0, limit - requestCount.Count).ToString());
        context.Response.Headers.Append("X-RateLimit-Reset", GetResetTimestamp(requestCount).ToString());

        await _next(context);
    }

    private string GetClientKey(HttpContext context)
    {
        // Try to get user ID from claims
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // Fall back to IP address
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return $"ip:{forwardedFor.Split(',').First().Trim()}";
        }

        return $"ip:{context.Connection.RemoteIpAddress}";
    }

    private static string GetEndpointKey(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";

        // Normalize path for rate limiting (remove IDs)
        var normalizedPath = NormalizePath(path);
        return $"{method}:{normalizedPath}";
    }

    private static string NormalizePath(string path)
    {
        // Replace GUIDs with placeholder
        var guidPattern = new System.Text.RegularExpressions.Regex(
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        path = guidPattern.Replace(path, "{id}");

        // Replace numeric IDs with placeholder
        var numericPattern = new System.Text.RegularExpressions.Regex(@"/\d+(/|$)");
        path = numericPattern.Replace(path, "/{id}$1");

        return path.ToLowerInvariant();
    }

    private int GetLimitForEndpoint(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var method = context.Request.Method;

        // Higher limits for log ingestion
        if (path.Contains("/logs") && method == "POST")
        {
            return _options.LogIngestionLimit;
        }

        // Higher limits for batch operations
        if (path.Contains("/batch"))
        {
            return _options.BatchLimit;
        }

        // Default limit
        return _options.DefaultLimit;
    }

    private int GetRetryAfterSeconds(RequestCounter counter)
    {
        var windowEnd = counter.WindowStart.Add(_options.WindowDuration);
        var remaining = windowEnd - DateTime.UtcNow;
        return Math.Max(1, (int)remaining.TotalSeconds);
    }

    private long GetResetTimestamp(RequestCounter counter)
    {
        var windowEnd = counter.WindowStart.Add(_options.WindowDuration);
        return new DateTimeOffset(windowEnd).ToUnixTimeSeconds();
    }

    private class RequestCounter
    {
        public int Count { get; set; }
        public DateTime WindowStart { get; set; }
    }
}

/// <summary>
/// Rate limiting configuration options.
/// </summary>
public class RateLimitingOptions
{
    public bool Enabled { get; set; } = true;
    public TimeSpan WindowDuration { get; set; } = TimeSpan.FromMinutes(1);
    public int DefaultLimit { get; set; } = 100;
    public int LogIngestionLimit { get; set; } = 1000;
    public int BatchLimit { get; set; } = 50;
}

/// <summary>
/// Extension methods for rate limiting middleware.
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app, RateLimitingOptions? options = null)
    {
        options ??= new RateLimitingOptions();
        return app.UseMiddleware<RateLimitingMiddleware>(options);
    }

    public static IServiceCollection AddRateLimiting(this IServiceCollection services, Action<RateLimitingOptions>? configure = null)
    {
        var options = new RateLimitingOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        return services;
    }
}
