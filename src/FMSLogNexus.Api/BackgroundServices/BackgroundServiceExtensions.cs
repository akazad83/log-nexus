using Microsoft.Extensions.Options;

namespace FMSLogNexus.Api.BackgroundServices;

/// <summary>
/// Extension methods for registering background services.
/// </summary>
public static class BackgroundServiceExtensions
{
    /// <summary>
    /// Adds all background services to the service collection.
    /// </summary>
    public static IServiceCollection AddBackgroundServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register options
        services.Configure<BackgroundServiceOptions>(
            configuration.GetSection(BackgroundServiceOptions.SectionName));

        // Validate options on startup
        services.AddSingleton<IValidateOptions<BackgroundServiceOptions>, BackgroundServiceOptionsValidator>();

        // Register background services
        services.AddHostedService<DashboardUpdateService>();
        services.AddHostedService<ServerHealthCheckService>();
        services.AddHostedService<ExecutionTimeoutService>();
        services.AddHostedService<AlertEvaluationService>();
        services.AddHostedService<LogStatisticsService>();
        services.AddHostedService<MaintenanceService>();
        services.AddHostedService<JobHealthMonitorService>();

        return services;
    }

    /// <summary>
    /// Adds background services with custom configuration.
    /// </summary>
    public static IServiceCollection AddBackgroundServices(
        this IServiceCollection services,
        Action<BackgroundServiceOptions> configure)
    {
        services.Configure(configure);

        // Register background services
        services.AddHostedService<DashboardUpdateService>();
        services.AddHostedService<ServerHealthCheckService>();
        services.AddHostedService<ExecutionTimeoutService>();
        services.AddHostedService<AlertEvaluationService>();
        services.AddHostedService<LogStatisticsService>();
        services.AddHostedService<MaintenanceService>();
        services.AddHostedService<JobHealthMonitorService>();

        return services;
    }
}

/// <summary>
/// Validator for background service options.
/// </summary>
public class BackgroundServiceOptionsValidator : IValidateOptions<BackgroundServiceOptions>
{
    public ValidateOptionsResult Validate(string? name, BackgroundServiceOptions options)
    {
        var errors = new List<string>();

        if (options.DashboardUpdateIntervalSeconds < 5)
        {
            errors.Add("DashboardUpdateIntervalSeconds must be at least 5 seconds");
        }

        if (options.ServerHealthCheckIntervalSeconds < 10)
        {
            errors.Add("ServerHealthCheckIntervalSeconds must be at least 10 seconds");
        }

        if (options.ExecutionTimeoutCheckIntervalSeconds < 10)
        {
            errors.Add("ExecutionTimeoutCheckIntervalSeconds must be at least 10 seconds");
        }

        if (options.AlertEvaluationIntervalSeconds < 5)
        {
            errors.Add("AlertEvaluationIntervalSeconds must be at least 5 seconds");
        }

        if (options.LogStatisticsIntervalSeconds < 30)
        {
            errors.Add("LogStatisticsIntervalSeconds must be at least 30 seconds");
        }

        if (options.MaintenanceIntervalHours < 1)
        {
            errors.Add("MaintenanceIntervalHours must be at least 1 hour");
        }

        if (options.ServerTimeoutThresholdSeconds < 60)
        {
            errors.Add("ServerTimeoutThresholdSeconds must be at least 60 seconds");
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Health check for background services.
/// </summary>
public class BackgroundServicesHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundServicesHealthCheck> _logger;

    public BackgroundServicesHealthCheck(
        IServiceProvider serviceProvider,
        ILogger<BackgroundServicesHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if options are valid
            var options = _serviceProvider.GetRequiredService<IOptions<BackgroundServiceOptions>>();

            if (!options.Value.Enabled)
            {
                return Task.FromResult(
                    Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                        "Background services are disabled"));
            }

            // In a production system, you might check:
            // - Last execution time of each service
            // - Error counts
            // - Queue depths
            // - etc.

            return Task.FromResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                    "Background services are running"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking background services health");
            return Task.FromResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    "Error checking background services", ex));
        }
    }
}
