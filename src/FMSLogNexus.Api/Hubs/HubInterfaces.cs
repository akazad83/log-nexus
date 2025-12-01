using FMSLogNexus.Core.Enums;
using FmsLogLevel = FMSLogNexus.Core.Enums.LogLevel;

namespace FMSLogNexus.Api.Hubs;

#region Client Interfaces

/// <summary>
/// Interface for log hub client methods.
/// </summary>
public interface ILogHubClient
{
    /// <summary>
    /// Receives a new log entry.
    /// </summary>
    Task ReceiveLog(LogNotification log);

    /// <summary>
    /// Receives a batch of log entries.
    /// </summary>
    Task ReceiveLogBatch(IEnumerable<LogNotification> logs);

    /// <summary>
    /// Receives log statistics update.
    /// </summary>
    Task ReceiveLogStats(LogStatsNotification stats);
}

/// <summary>
/// Interface for job hub client methods.
/// </summary>
public interface IJobHubClient
{
    /// <summary>
    /// Receives job execution started notification.
    /// </summary>
    Task ExecutionStarted(ExecutionNotification execution);

    /// <summary>
    /// Receives job execution completed notification.
    /// </summary>
    Task ExecutionCompleted(ExecutionNotification execution);

    /// <summary>
    /// Receives job execution progress update.
    /// </summary>
    Task ExecutionProgress(ExecutionProgressNotification progress);

    /// <summary>
    /// Receives job status change notification.
    /// </summary>
    Task JobStatusChanged(JobStatusNotification status);

    /// <summary>
    /// Receives job health update.
    /// </summary>
    Task JobHealthUpdated(JobHealthNotification health);
}

/// <summary>
/// Interface for server hub client methods.
/// </summary>
public interface IServerHubClient
{
    /// <summary>
    /// Receives server status change notification.
    /// </summary>
    Task ServerStatusChanged(ServerStatusNotification status);

    /// <summary>
    /// Receives server heartbeat notification.
    /// </summary>
    Task ServerHeartbeat(ServerHeartbeatNotification heartbeat);

    /// <summary>
    /// Receives server metrics update.
    /// </summary>
    Task ServerMetrics(ServerMetricsNotification metrics);
}

/// <summary>
/// Interface for alert hub client methods.
/// </summary>
public interface IAlertHubClient
{
    /// <summary>
    /// Receives new alert triggered notification.
    /// </summary>
    Task AlertTriggered(AlertNotification alert);

    /// <summary>
    /// Receives alert acknowledged notification.
    /// </summary>
    Task AlertAcknowledged(AlertActionNotification action);

    /// <summary>
    /// Receives alert resolved notification.
    /// </summary>
    Task AlertResolved(AlertActionNotification action);

    /// <summary>
    /// Receives alert summary update.
    /// </summary>
    Task AlertSummaryUpdated(AlertSummaryNotification summary);
}

/// <summary>
/// Interface for dashboard hub client methods.
/// </summary>
public interface IDashboardHubClient
{
    /// <summary>
    /// Receives dashboard summary update.
    /// </summary>
    Task DashboardUpdated(DashboardNotification dashboard);

    /// <summary>
    /// Receives system notification.
    /// </summary>
    Task SystemNotification(SystemNotificationMessage notification);
}

#endregion

#region Notification DTOs

/// <summary>
/// Log entry notification.
/// </summary>
public class LogNotification
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public FmsLogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ServerName { get; set; }
    public string? JobId { get; set; }
    public Guid? ExecutionId { get; set; }
    public string? Category { get; set; }
    public string? ExceptionType { get; set; }
}

/// <summary>
/// Log statistics notification.
/// </summary>
public class LogStatsNotification
{
    public DateTime Timestamp { get; set; }
    public long TotalLogs { get; set; }
    public long ErrorCount { get; set; }
    public long WarningCount { get; set; }
    public double ErrorRate { get; set; }
    public string? ServerName { get; set; }
    public string? JobId { get; set; }
}

/// <summary>
/// Execution notification.
/// </summary>
public class ExecutionNotification
{
    public Guid Id { get; set; }
    public string JobId { get; set; } = string.Empty;
    public string JobDisplayName { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? DurationMs { get; set; }
    public string? TriggerType { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Execution progress notification.
/// </summary>
public class ExecutionProgressNotification
{
    public Guid ExecutionId { get; set; }
    public string JobId { get; set; } = string.Empty;
    public int PercentComplete { get; set; }
    public string? CurrentStep { get; set; }
    public long ElapsedMs { get; set; }
    public long? EstimatedRemainingMs { get; set; }
}

/// <summary>
/// Job status notification.
/// </summary>
public class JobStatusNotification
{
    public string JobId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public JobStatus? LastStatus { get; set; }
    public DateTime? LastExecutionAt { get; set; }
}

/// <summary>
/// Job health notification.
/// </summary>
public class JobHealthNotification
{
    public string JobId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int HealthScore { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public int ConsecutiveFailures { get; set; }
    public double? SuccessRate { get; set; }
}

/// <summary>
/// Server status notification.
/// </summary>
public class ServerStatusNotification
{
    public string ServerName { get; set; } = string.Empty;
    public ServerStatus Status { get; set; }
    public ServerStatus? PreviousStatus { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsActive { get; set; }
    public bool InMaintenance { get; set; }
}

/// <summary>
/// Server heartbeat notification.
/// </summary>
public class ServerHeartbeatNotification
{
    public string ServerName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public ServerStatus Status { get; set; }
    public string? AgentVersion { get; set; }
}

/// <summary>
/// Server metrics notification.
/// </summary>
public class ServerMetricsNotification
{
    public string ServerName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double? CpuUsage { get; set; }
    public double? MemoryUsage { get; set; }
    public double? DiskUsage { get; set; }
    public int ActiveConnections { get; set; }
    public int RunningJobs { get; set; }
}

/// <summary>
/// Alert notification.
/// </summary>
public class AlertNotification
{
    public Guid InstanceId { get; set; }
    public Guid AlertId { get; set; }
    public string AlertName { get; set; } = string.Empty;
    public AlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; }
    public string? JobId { get; set; }
    public string? ServerName { get; set; }
}

/// <summary>
/// Alert action notification.
/// </summary>
public class AlertActionNotification
{
    public Guid InstanceId { get; set; }
    public Guid AlertId { get; set; }
    public string AlertName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Alert summary notification.
/// </summary>
public class AlertSummaryNotification
{
    public int TotalActive { get; set; }
    public int NewAlerts { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
}

/// <summary>
/// Dashboard notification.
/// </summary>
public class DashboardNotification
{
    public DateTime Timestamp { get; set; }
    public DashboardLogStats LogStats { get; set; } = new();
    public DashboardJobStats JobStats { get; set; } = new();
    public DashboardServerStats ServerStats { get; set; } = new();
    public DashboardAlertStats AlertStats { get; set; } = new();
}

public class DashboardLogStats
{
    public long TotalLogs24h { get; set; }
    public long ErrorCount24h { get; set; }
    public long WarningCount24h { get; set; }
    public double ErrorRate { get; set; }
}

public class DashboardJobStats
{
    public int TotalJobs { get; set; }
    public int ActiveJobs { get; set; }
    public int RunningNow { get; set; }
    public int FailedLastRun { get; set; }
    public int HealthyJobs { get; set; }
}

public class DashboardServerStats
{
    public int TotalServers { get; set; }
    public int OnlineServers { get; set; }
    public int OfflineServers { get; set; }
    public int MaintenanceServers { get; set; }
}

public class DashboardAlertStats
{
    public int ActiveAlerts { get; set; }
    public int NewAlerts { get; set; }
    public int CriticalAlerts { get; set; }
}

/// <summary>
/// System notification message.
/// </summary>
public class SystemNotificationMessage
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "info";
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}

#endregion

#region Subscription Models

/// <summary>
/// Log subscription options.
/// </summary>
public class LogSubscriptionOptions
{
    public string? ServerName { get; set; }
    public string? JobId { get; set; }
    public Guid? ExecutionId { get; set; }
    public FmsLogLevel? MinLevel { get; set; }
    public List<string>? Categories { get; set; }
    public bool IncludeStatistics { get; set; }
}

/// <summary>
/// Job subscription options.
/// </summary>
public class JobSubscriptionOptions
{
    public string? JobId { get; set; }
    public string? ServerName { get; set; }
    public string? Category { get; set; }
    public bool IncludeHealthUpdates { get; set; } = true;
}

/// <summary>
/// Server subscription options.
/// </summary>
public class ServerSubscriptionOptions
{
    public string? ServerName { get; set; }
    public bool IncludeMetrics { get; set; }
    public bool IncludeHeartbeats { get; set; }
}

/// <summary>
/// Alert subscription options.
/// </summary>
public class AlertSubscriptionOptions
{
    public string? JobId { get; set; }
    public string? ServerName { get; set; }
    public AlertSeverity? MinSeverity { get; set; }
    public List<AlertType>? AlertTypes { get; set; }
}

#endregion
