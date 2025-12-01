using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.DTOs.Responses;

/// <summary>
/// Dashboard summary response.
/// </summary>
public class DashboardSummaryResponse
{
    /// <summary>
    /// Log statistics.
    /// </summary>
    public DashboardLogStats Logs { get; set; } = new();

    /// <summary>
    /// Job statistics.
    /// </summary>
    public DashboardJobStats Jobs { get; set; } = new();

    /// <summary>
    /// Server statistics.
    /// </summary>
    public DashboardServerStats Servers { get; set; } = new();

    /// <summary>
    /// Alert statistics.
    /// </summary>
    public DashboardAlertStats Alerts { get; set; } = new();

    /// <summary>
    /// System health.
    /// </summary>
    public SystemHealthResponse Health { get; set; } = new();

    /// <summary>
    /// Data freshness (last updated).
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Dashboard log statistics.
/// </summary>
public class DashboardLogStats
{
    /// <summary>
    /// Total logs in period.
    /// </summary>
    public long Total { get; set; }

    /// <summary>
    /// Error count.
    /// </summary>
    public long Errors { get; set; }

    /// <summary>
    /// Warning count.
    /// </summary>
    public long Warnings { get; set; }

    /// <summary>
    /// Critical count.
    /// </summary>
    public long Critical { get; set; }

    /// <summary>
    /// Logs per minute (average).
    /// </summary>
    public decimal LogsPerMinute { get; set; }

    /// <summary>
    /// Change from previous period (percentage).
    /// </summary>
    public decimal? ChangePercentage { get; set; }
}

/// <summary>
/// Dashboard job statistics.
/// </summary>
public class DashboardJobStats
{
    /// <summary>
    /// Total jobs.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Active jobs.
    /// </summary>
    public int Active { get; set; }

    /// <summary>
    /// Running jobs.
    /// </summary>
    public int Running { get; set; }

    /// <summary>
    /// Failed in period.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Success rate.
    /// </summary>
    public decimal? SuccessRate { get; set; }

    /// <summary>
    /// Healthy jobs (score >= 80).
    /// </summary>
    public int Healthy { get; set; }

    /// <summary>
    /// Warning jobs (50 <= score < 80).
    /// </summary>
    public int Warning { get; set; }

    /// <summary>
    /// Critical jobs (score < 50).
    /// </summary>
    public int CriticalHealth { get; set; }
}

/// <summary>
/// Dashboard server statistics.
/// </summary>
public class DashboardServerStats
{
    /// <summary>
    /// Total servers.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Online servers.
    /// </summary>
    public int Online { get; set; }

    /// <summary>
    /// Offline servers.
    /// </summary>
    public int Offline { get; set; }

    /// <summary>
    /// Degraded servers.
    /// </summary>
    public int Degraded { get; set; }

    /// <summary>
    /// Unknown status servers.
    /// </summary>
    public int Unknown { get; set; }
}

/// <summary>
/// Dashboard alert statistics.
/// </summary>
public class DashboardAlertStats
{
    /// <summary>
    /// Total active alerts.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// New (unacknowledged) alerts.
    /// </summary>
    public int New { get; set; }

    /// <summary>
    /// Critical alerts.
    /// </summary>
    public int Critical { get; set; }

    /// <summary>
    /// High priority alerts.
    /// </summary>
    public int High { get; set; }
}

/// <summary>
/// System health response.
/// </summary>
public class SystemHealthResponse
{
    /// <summary>
    /// Overall status.
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// Status name.
    /// </summary>
    public string StatusName => Status.ToString();

    /// <summary>
    /// Component health checks.
    /// </summary>
    public List<HealthCheckResult> Components { get; set; } = new();

    /// <summary>
    /// Check timestamp.
    /// </summary>
    public DateTime CheckedAt { get; set; }

    /// <summary>
    /// Total check duration in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }
}

/// <summary>
/// Individual health check result.
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// Component name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Status.
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// Status description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Check duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Additional data.
    /// </summary>
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// Dashboard trends response.
/// </summary>
public class DashboardTrendsResponse
{
    /// <summary>
    /// Time period.
    /// </summary>
    public TimePeriod Period { get; set; }

    /// <summary>
    /// Interval.
    /// </summary>
    public TrendInterval Interval { get; set; }

    /// <summary>
    /// Trend data points.
    /// </summary>
    public List<LogTrendPoint> Data { get; set; } = new();
}

/// <summary>
/// Job health overview response.
/// </summary>
public class JobHealthOverviewResponse
{
    /// <summary>
    /// Summary counts.
    /// </summary>
    public JobHealthSummary Summary { get; set; } = new();

    /// <summary>
    /// Jobs sorted by health (worst first).
    /// </summary>
    public List<JobListItem> Jobs { get; set; } = new();

    /// <summary>
    /// Recent failures.
    /// </summary>
    public List<RecentFailure> RecentFailures { get; set; } = new();
}

/// <summary>
/// Job health summary.
/// </summary>
public class JobHealthSummary
{
    public int Total { get; set; }
    public int Healthy { get; set; }
    public int Warning { get; set; }
    public int Critical { get; set; }
    public int Unknown { get; set; }
}

/// <summary>
/// Recent job failure.
/// </summary>
public class RecentFailure
{
    public Guid ExecutionId { get; set; }
    public string JobId { get; set; } = string.Empty;
    public string JobDisplayName { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCategory { get; set; }
}

/// <summary>
/// Server status response.
/// </summary>
public class ServerStatusResponse
{
    /// <summary>
    /// Server name.
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// IP address.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Status.
    /// </summary>
    public ServerStatus Status { get; set; }

    /// <summary>
    /// Status name.
    /// </summary>
    public string StatusName => Status.ToString();

    /// <summary>
    /// Last heartbeat.
    /// </summary>
    public DateTime? LastHeartbeat { get; set; }

    /// <summary>
    /// Seconds since heartbeat.
    /// </summary>
    public int? SecondsSinceHeartbeat { get; set; }

    /// <summary>
    /// Agent version.
    /// </summary>
    public string? AgentVersion { get; set; }

    /// <summary>
    /// Active job count.
    /// </summary>
    public int ActiveJobCount { get; set; }

    /// <summary>
    /// Running job count.
    /// </summary>
    public int RunningJobCount { get; set; }

    /// <summary>
    /// Server metrics.
    /// </summary>
    public ServerMetrics? Metrics { get; set; }
}

/// <summary>
/// Server metrics.
/// </summary>
public class ServerMetrics
{
    /// <summary>
    /// CPU usage percentage.
    /// </summary>
    public decimal? CpuUsage { get; set; }

    /// <summary>
    /// Memory usage percentage.
    /// </summary>
    public decimal? MemoryUsage { get; set; }

    /// <summary>
    /// Disk usage percentage.
    /// </summary>
    public decimal? DiskUsage { get; set; }
}

/// <summary>
/// Recent error log for dashboard.
/// </summary>
public class RecentErrorResponse
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string LevelName => Level.ToString();
    public string Message { get; set; } = string.Empty;
    public string? JobId { get; set; }
    public string? JobDisplayName { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string? ExceptionType { get; set; }
}

/// <summary>
/// System configuration response.
/// </summary>
public class ConfigurationResponse
{
    /// <summary>
    /// Configuration values by category.
    /// </summary>
    public Dictionary<string, Dictionary<string, ConfigurationItem>> Categories { get; set; } = new();
}

/// <summary>
/// Individual configuration item.
/// </summary>
public class ConfigurationItem
{
    /// <summary>
    /// Configuration key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Current value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Data type.
    /// </summary>
    public string DataType { get; set; } = "string";

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether encrypted.
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// Last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
