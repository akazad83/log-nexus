using Microsoft.AspNetCore.SignalR;

namespace FMSLogNexus.Api.Hubs;

/// <summary>
/// Extension methods for SignalR services registration.
/// </summary>
public static class SignalRExtensions
{
    /// <summary>
    /// Adds SignalR hub services to the service collection.
    /// </summary>
    public static IServiceCollection AddSignalRHubs(this IServiceCollection services, Action<HubOptions>? configureHubs = null)
    {
        // Add SignalR
        var signalRBuilder = services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = 1024 * 1024; // 1 MB
            options.StreamBufferCapacity = 100;
            options.HandshakeTimeout = TimeSpan.FromSeconds(15);
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);

            configureHubs?.Invoke(options);
        });

        // Add JSON protocol with custom options
        signalRBuilder.AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

        // Register connection manager
        services.AddSingleton<IConnectionManager, ConnectionManager>();

        // Register broadcasters
        services.AddScoped<ILogBroadcaster, LogBroadcaster>();
        services.AddScoped<IJobBroadcaster, JobBroadcaster>();
        services.AddScoped<IServerBroadcaster, ServerBroadcaster>();
        services.AddScoped<IAlertBroadcaster, AlertBroadcaster>();
        services.AddScoped<IDashboardBroadcaster, DashboardBroadcaster>();

        // Register unified real-time service
        services.AddScoped<IRealTimeService, RealTimeService>();

        return services;
    }

    /// <summary>
    /// Maps SignalR hub endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapSignalRHubs(this IEndpointRouteBuilder endpoints)
    {
        // Map hubs with authentication
        endpoints.MapHub<LogHub>("/hubs/logs", options =>
        {
            options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
                                Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents;
        });

        endpoints.MapHub<JobHub>("/hubs/jobs", options =>
        {
            options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
                                Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents;
        });

        endpoints.MapHub<ServerHub>("/hubs/servers", options =>
        {
            options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
                                Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents;
        });

        endpoints.MapHub<AlertHub>("/hubs/alerts", options =>
        {
            options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
                                Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents;
        });

        endpoints.MapHub<DashboardHub>("/hubs/dashboard", options =>
        {
            options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
                                Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents;
        });

        return endpoints;
    }

    /// <summary>
    /// Configures SignalR with Redis backplane for scaling.
    /// </summary>
    public static ISignalRServerBuilder AddRedisBackplane(this ISignalRServerBuilder builder, string connectionString)
    {
        // Note: Requires Microsoft.AspNetCore.SignalR.StackExchangeRedis package
        // builder.AddStackExchangeRedis(connectionString, options =>
        // {
        //     options.Configuration.ChannelPrefix = "FMSLogNexus";
        // });

        return builder;
    }
}

/// <summary>
/// Hub filter for logging and error handling.
/// </summary>
public class HubLoggingFilter : IHubFilter
{
    private readonly ILogger<HubLoggingFilter> _logger;

    public HubLoggingFilter(ILogger<HubLoggingFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var hubName = invocationContext.Hub.GetType().Name;
        var methodName = invocationContext.HubMethodName;
        var connectionId = invocationContext.Context.ConnectionId;

        _logger.LogDebug(
            "Hub method invoked: {Hub}.{Method} by connection {ConnectionId}",
            hubName, methodName, connectionId);

        try
        {
            return await next(invocationContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error in hub method: {Hub}.{Method} by connection {ConnectionId}",
                hubName, methodName, connectionId);
            throw;
        }
    }

    public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        _logger.LogInformation(
            "Client connected to {Hub}: {ConnectionId}",
            context.Hub.GetType().Name, context.Context.ConnectionId);

        return next(context);
    }

    public Task OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception,
                "Client disconnected from {Hub} with error: {ConnectionId}",
                context.Hub.GetType().Name, context.Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation(
                "Client disconnected from {Hub}: {ConnectionId}",
                context.Hub.GetType().Name, context.Context.ConnectionId);
        }

        return next(context, exception);
    }
}

/// <summary>
/// Hub user ID provider based on claims.
/// </summary>
public class ClaimsUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? connection.User?.FindFirst("sub")?.Value;
    }
}
