namespace FMSLogNexus.Core.Enums;

/// <summary>
/// Log severity levels matching Serilog/Microsoft.Extensions.Logging conventions.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Most detailed logging, typically only enabled in development.
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Debugging information useful during development.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// General operational information.
    /// </summary>
    Information = 2,

    /// <summary>
    /// Indicates a potential problem that should be investigated.
    /// </summary>
    Warning = 3,

    /// <summary>
    /// An error occurred but the application can continue.
    /// </summary>
    Error = 4,

    /// <summary>
    /// A critical error that requires immediate attention.
    /// </summary>
    Critical = 5
}

/// <summary>
/// Job execution status values.
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// Job execution is queued but not yet started.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Job execution is currently running.
    /// </summary>
    Running = 1,

    /// <summary>
    /// Job execution completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Job execution failed with an error.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Job execution was cancelled.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Job execution exceeded the maximum allowed duration.
    /// </summary>
    Timeout = 5,

    /// <summary>
    /// Job completed but with warnings.
    /// </summary>
    Warning = 6
}

/// <summary>
/// Types of jobs in the system.
/// </summary>
public enum JobType
{
    /// <summary>
    /// Job type is unknown or not specified.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Windows executable (.exe).
    /// </summary>
    Executable = 1,

    /// <summary>
    /// PowerShell script (.ps1).
    /// </summary>
    PowerShell = 2,

    /// <summary>
    /// VBScript (.vbs).
    /// </summary>
    VBScript = 3,

    /// <summary>
    /// Batch file (.bat, .cmd).
    /// </summary>
    BatchFile = 4,

    /// <summary>
    /// SQL Server Agent job.
    /// </summary>
    SqlAgent = 5,

    /// <summary>
    /// Windows Task Scheduler task.
    /// </summary>
    ScheduledTask = 6,

    /// <summary>
    /// Windows Service.
    /// </summary>
    WindowsService = 7,

    /// <summary>
    /// .NET application.
    /// </summary>
    DotNet = 8,

    /// <summary>
    /// SSIS package.
    /// </summary>
    SSIS = 9,

    /// <summary>
    /// Custom or other job type.
    /// </summary>
    Other = 99
}

/// <summary>
/// Server/machine status values.
/// </summary>
public enum ServerStatus
{
    /// <summary>
    /// Server status is unknown (no heartbeat received).
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Server is online and healthy.
    /// </summary>
    Online = 1,

    /// <summary>
    /// Server is offline (heartbeat timeout exceeded).
    /// </summary>
    Offline = 2,

    /// <summary>
    /// Server is degraded (heartbeat delayed but not timed out).
    /// </summary>
    Degraded = 3,

    /// <summary>
    /// Server is in maintenance mode.
    /// </summary>
    Maintenance = 4
}

/// <summary>
/// Alert severity levels.
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// Low severity - informational alert.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium severity - should be reviewed.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High severity - requires attention.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical severity - requires immediate action.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Types of alerts that can be configured.
/// </summary>
public enum AlertType
{
    /// <summary>
    /// Alert when error count exceeds threshold.
    /// </summary>
    ErrorThreshold = 0,

    /// <summary>
    /// Alert when a job fails.
    /// </summary>
    JobFailure = 1,

    /// <summary>
    /// Alert when a server goes offline.
    /// </summary>
    ServerOffline = 2,

    /// <summary>
    /// Alert for performance-related issues.
    /// </summary>
    PerformanceWarning = 3,

    /// <summary>
    /// Alert based on custom SQL query.
    /// </summary>
    CustomQuery = 4,

    /// <summary>
    /// Alert based on log message pattern matching.
    /// </summary>
    PatternMatch = 5,

    /// <summary>
    /// Alert when job duration exceeds expected time.
    /// </summary>
    DurationExceeded = 6,

    /// <summary>
    /// Alert when job doesn't run within expected schedule.
    /// </summary>
    MissedSchedule = 7
}

/// <summary>
/// Status of a triggered alert instance.
/// </summary>
public enum AlertInstanceStatus
{
    /// <summary>
    /// Alert is new and unacknowledged.
    /// </summary>
    New = 0,

    /// <summary>
    /// Alert has been acknowledged but not resolved.
    /// </summary>
    Acknowledged = 1,

    /// <summary>
    /// Alert has been resolved.
    /// </summary>
    Resolved = 2,

    /// <summary>
    /// Alert has been suppressed/ignored.
    /// </summary>
    Suppressed = 3
}

/// <summary>
/// User roles for authorization.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Read-only access to logs and dashboards.
    /// </summary>
    Viewer = 0,

    /// <summary>
    /// Can acknowledge alerts and manage basic operations.
    /// </summary>
    Operator = 1,

    /// <summary>
    /// Full access to all features including configuration.
    /// </summary>
    Administrator = 2,

    /// <summary>
    /// API access with limited permissions for external integrations.
    /// </summary>
    ApiUser = 3
}

/// <summary>
/// Sort order for queries.
/// </summary>
public enum SortOrder
{
    /// <summary>
    /// Ascending order (A-Z, oldest first).
    /// </summary>
    Ascending = 0,

    /// <summary>
    /// Descending order (Z-A, newest first).
    /// </summary>
    Descending = 1
}

/// <summary>
/// Time period for trend/statistics queries.
/// </summary>
public enum TimePeriod
{
    /// <summary>
    /// Last hour.
    /// </summary>
    LastHour = 0,

    /// <summary>
    /// Last 1 hour (alias for LastHour).
    /// </summary>
    Last1Hour = 0,

    /// <summary>
    /// Last 6 hours.
    /// </summary>
    Last6Hours = 1,

    /// <summary>
    /// Last 24 hours.
    /// </summary>
    Last24Hours = 2,

    /// <summary>
    /// Last 7 days.
    /// </summary>
    Last7Days = 3,

    /// <summary>
    /// Last 30 days.
    /// </summary>
    Last30Days = 4,

    /// <summary>
    /// Custom date range.
    /// </summary>
    Custom = 99
}

/// <summary>
/// Interval for trend data aggregation.
/// </summary>
public enum TrendInterval
{
    /// <summary>
    /// Per minute aggregation.
    /// </summary>
    Minute = 0,

    /// <summary>
    /// Per hour aggregation.
    /// </summary>
    Hour = 1,

    /// <summary>
    /// Per day aggregation.
    /// </summary>
    Day = 2,

    /// <summary>
    /// Per week aggregation.
    /// </summary>
    Week = 3,

    /// <summary>
    /// Per month aggregation.
    /// </summary>
    Month = 4
}

/// <summary>
/// Export format options.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// JSON format.
    /// </summary>
    Json = 0,

    /// <summary>
    /// CSV format.
    /// </summary>
    Csv = 1,

    /// <summary>
    /// Excel format.
    /// </summary>
    Excel = 2
}

/// <summary>
/// Health status for dashboard display.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// Status is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Everything is healthy.
    /// </summary>
    Healthy = 1,

    /// <summary>
    /// Some warnings exist.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Critical issues detected.
    /// </summary>
    Critical = 3
}
