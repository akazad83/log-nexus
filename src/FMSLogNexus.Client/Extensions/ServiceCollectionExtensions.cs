using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FMSLogNexus.Client;

/// <summary>
/// Extension methods for registering FMS Log Nexus services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds FMS Log Nexus client services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddFMSLogNexus(
        this IServiceCollection services,
        Action<FMSLogNexusOptions> configure)
    {
        services.Configure(configure);
        services.TryAddSingleton<FMSLogNexusClient>();
        
        return services;
    }

    /// <summary>
    /// Adds FMS Log Nexus client services with options instance.
    /// </summary>
    public static IServiceCollection AddFMSLogNexus(
        this IServiceCollection services,
        FMSLogNexusOptions options)
    {
        services.Configure<FMSLogNexusOptions>(o =>
        {
            o.BaseUrl = options.BaseUrl;
            o.ServerName = options.ServerName;
            o.ApiKey = options.ApiKey;
            o.AccessToken = options.AccessToken;
            o.AgentVersion = options.AgentVersion;
            o.HeartbeatIntervalSeconds = options.HeartbeatIntervalSeconds;
            o.LogFlushIntervalSeconds = options.LogFlushIntervalSeconds;
            o.MaxBatchSize = options.MaxBatchSize;
            o.HttpTimeoutSeconds = options.HttpTimeoutSeconds;
            o.RetryAttempts = options.RetryAttempts;
            o.RetryDelayMs = options.RetryDelayMs;
            o.EnableRealTimeUpdates = options.EnableRealTimeUpdates;
            o.EnableAutoHeartbeat = options.EnableAutoHeartbeat;
            o.EnableLogBuffering = options.EnableLogBuffering;
        });
        
        services.TryAddSingleton<FMSLogNexusClient>();
        
        return services;
    }

    /// <summary>
    /// Adds the FMS Agent background service for automatic heartbeats and log flushing.
    /// </summary>
    public static IServiceCollection AddFMSAgent(this IServiceCollection services)
    {
        services.AddHostedService<FMSAgentService>();
        return services;
    }

    /// <summary>
    /// Adds the heartbeat background service.
    /// </summary>
    public static IServiceCollection AddFMSHeartbeat(this IServiceCollection services)
    {
        services.AddHostedService<HeartbeatService>();
        return services;
    }

    /// <summary>
    /// Adds the log flush background service.
    /// </summary>
    public static IServiceCollection AddFMSLogFlush(this IServiceCollection services)
    {
        services.AddHostedService<LogFlushService>();
        return services;
    }

    /// <summary>
    /// Adds all FMS Log Nexus services including agent background services.
    /// </summary>
    public static IServiceCollection AddFMSLogNexusAgent(
        this IServiceCollection services,
        Action<FMSLogNexusOptions> configure)
    {
        return services
            .AddFMSLogNexus(configure)
            .AddFMSAgent();
    }
}
