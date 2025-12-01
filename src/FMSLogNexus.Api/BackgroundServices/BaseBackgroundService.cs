namespace FMSLogNexus.Api.BackgroundServices;

/// <summary>
/// Base class for background services with common functionality.
/// </summary>
public abstract class BaseBackgroundService : BackgroundService
{
    protected readonly IServiceProvider _serviceProvider;
    protected readonly ILogger _logger;
    protected readonly string _serviceName;

    protected BaseBackgroundService(
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _serviceName = GetType().Name;
    }

    /// <summary>
    /// Gets the interval between executions.
    /// </summary>
    protected abstract TimeSpan Interval { get; }

    /// <summary>
    /// Gets whether the service should run immediately on startup.
    /// </summary>
    protected virtual bool RunOnStartup => false;

    /// <summary>
    /// Gets the initial delay before first execution.
    /// </summary>
    protected virtual TimeSpan InitialDelay => TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Service} starting with interval {Interval}", _serviceName, Interval);

        if (!RunOnStartup)
        {
            _logger.LogInformation("{Service} waiting {Delay} before first execution", _serviceName, InitialDelay);
            await Task.Delay(InitialDelay, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("{Service} executing", _serviceName);
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                await ExecuteTaskAsync(stoppingToken);

                stopwatch.Stop();
                _logger.LogDebug("{Service} completed in {ElapsedMs}ms", _serviceName, stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("{Service} cancelled", _serviceName);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Service} encountered an error", _serviceName);
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("{Service} stopped", _serviceName);
    }

    /// <summary>
    /// Executes the background task.
    /// </summary>
    protected abstract Task ExecuteTaskAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new service scope for dependency resolution.
    /// </summary>
    protected IServiceScope CreateScope() => _serviceProvider.CreateScope();

    /// <summary>
    /// Gets a service from a scope.
    /// </summary>
    protected T GetService<T>(IServiceScope scope) where T : notnull
        => scope.ServiceProvider.GetRequiredService<T>();
}

/// <summary>
/// Configuration options for background services.
/// </summary>
public class BackgroundServiceOptions
{
    public const string SectionName = "BackgroundServices";

    /// <summary>
    /// Dashboard update interval in seconds.
    /// </summary>
    public int DashboardUpdateIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Server health check interval in seconds.
    /// </summary>
    public int ServerHealthCheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Execution timeout check interval in seconds.
    /// </summary>
    public int ExecutionTimeoutCheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Alert evaluation interval in seconds.
    /// </summary>
    public int AlertEvaluationIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Log statistics aggregation interval in seconds.
    /// </summary>
    public int LogStatisticsIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Maintenance task interval in hours.
    /// </summary>
    public int MaintenanceIntervalHours { get; set; } = 24;

    /// <summary>
    /// Server timeout threshold in seconds.
    /// </summary>
    public int ServerTimeoutThresholdSeconds { get; set; } = 300;

    /// <summary>
    /// Whether to enable all background services.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
