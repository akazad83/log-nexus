using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.DTOs.Responses;

/// <summary>
/// Basic job response.
/// </summary>
public class JobResponse : JobListItem
{
}

/// <summary>
/// Job health response.
/// </summary>
public class JobHealthResponse
{
    /// <summary>
    /// Job ID.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Health score (0-100).
    /// </summary>
    public int HealthScore { get; set; }

    /// <summary>
    /// Health status.
    /// </summary>
    public HealthStatus HealthStatus { get; set; }

    /// <summary>
    /// Status name.
    /// </summary>
    public string HealthStatusName => HealthStatus.ToString();

    /// <summary>
    /// Consecutive failure count.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Last execution status.
    /// </summary>
    public JobStatus? LastStatus { get; set; }

    /// <summary>
    /// Last execution timestamp.
    /// </summary>
    public DateTime? LastExecutionAt { get; set; }

    /// <summary>
    /// Success rate percentage.
    /// </summary>
    public decimal? SuccessRate { get; set; }

    /// <summary>
    /// Average duration in milliseconds.
    /// </summary>
    public long? AvgDurationMs { get; set; }

    /// <summary>
    /// Total executions.
    /// </summary>
    public int TotalExecutions { get; set; }
}

/// <summary>
/// Job list item response.
/// </summary>
public class JobListItem
{
    /// <summary>
    /// Job ID.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Category.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Job type.
    /// </summary>
    public JobType JobType { get; set; }

    /// <summary>
    /// Job type name.
    /// </summary>
    public string JobTypeName => JobType.ToString();

    /// <summary>
    /// Server name.
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// Whether active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether critical.
    /// </summary>
    public bool IsCritical { get; set; }

    /// <summary>
    /// Last execution status.
    /// </summary>
    public JobStatus? LastStatus { get; set; }

    /// <summary>
    /// Last status name.
    /// </summary>
    public string? LastStatusName => LastStatus?.ToString();

    /// <summary>
    /// Last execution timestamp.
    /// </summary>
    public DateTime? LastExecutionAt { get; set; }

    /// <summary>
    /// Health score (0-100).
    /// </summary>
    public int? HealthScore { get; set; }

    /// <summary>
    /// Health status based on score.
    /// </summary>
    public HealthStatus HealthStatus => HealthScore switch
    {
        null => HealthStatus.Unknown,
        >= 80 => HealthStatus.Healthy,
        >= 50 => HealthStatus.Warning,
        _ => HealthStatus.Critical
    };
}

/// <summary>
/// Detailed job response.
/// </summary>
public class JobDetailResponse
{
    /// <summary>
    /// Job ID.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Tags.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Job type.
    /// </summary>
    public JobType JobType { get; set; }

    /// <summary>
    /// Server name.
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// Executable path.
    /// </summary>
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// Schedule.
    /// </summary>
    public string? Schedule { get; set; }

    /// <summary>
    /// Schedule description.
    /// </summary>
    public string? ScheduleDescription { get; set; }

    /// <summary>
    /// Whether active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether critical.
    /// </summary>
    public bool IsCritical { get; set; }

    /// <summary>
    /// Expected duration in milliseconds.
    /// </summary>
    public long? ExpectedDurationMs { get; set; }

    /// <summary>
    /// Max duration in milliseconds.
    /// </summary>
    public long? MaxDurationMs { get; set; }

    /// <summary>
    /// Configuration.
    /// </summary>
    public Dictionary<string, object>? Configuration { get; set; }

    /// <summary>
    /// Statistics.
    /// </summary>
    public JobStatistics Statistics { get; set; } = new();

    /// <summary>
    /// Recent executions.
    /// </summary>
    public List<ExecutionListItem> RecentExecutions { get; set; } = new();

    /// <summary>
    /// Created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Job statistics.
/// </summary>
public class JobStatistics
{
    /// <summary>
    /// Total executions.
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Success count.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Failure count.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Success rate percentage.
    /// </summary>
    public decimal? SuccessRate { get; set; }

    /// <summary>
    /// Average duration in milliseconds.
    /// </summary>
    public long? AvgDurationMs { get; set; }

    /// <summary>
    /// Average duration formatted.
    /// </summary>
    public string? AvgDurationFormatted { get; set; }

    /// <summary>
    /// Last execution timestamp.
    /// </summary>
    public DateTime? LastExecutionAt { get; set; }

    /// <summary>
    /// Last status.
    /// </summary>
    public JobStatus? LastStatus { get; set; }

    /// <summary>
    /// Health score.
    /// </summary>
    public int? HealthScore { get; set; }
}

/// <summary>
/// Execution list item.
/// </summary>
public class ExecutionListItem
{
    /// <summary>
    /// Execution ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Job ID.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Job display name.
    /// </summary>
    public string? JobDisplayName { get; set; }

    /// <summary>
    /// Start timestamp.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// End timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Status.
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Status name.
    /// </summary>
    public string StatusName => Status.ToString();

    /// <summary>
    /// Server name.
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Duration formatted.
    /// </summary>
    public string? DurationFormatted { get; set; }

    /// <summary>
    /// Trigger type.
    /// </summary>
    public string TriggerType { get; set; } = string.Empty;

    /// <summary>
    /// Whether currently running.
    /// </summary>
    public bool IsRunning => Status == JobStatus.Pending || Status == JobStatus.Running;

    /// <summary>
    /// Error count.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Warning count.
    /// </summary>
    public int WarningCount { get; set; }
}

/// <summary>
/// Detailed execution response.
/// </summary>
public class ExecutionDetailResponse : ExecutionListItem
{
    /// <summary>
    /// Correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Triggered by.
    /// </summary>
    public string? TriggeredBy { get; set; }

    /// <summary>
    /// Parameters.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// Result summary.
    /// </summary>
    public Dictionary<string, object>? ResultSummary { get; set; }

    /// <summary>
    /// Result code.
    /// </summary>
    public int? ResultCode { get; set; }

    /// <summary>
    /// Error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error category.
    /// </summary>
    public string? ErrorCategory { get; set; }

    /// <summary>
    /// Log counts by level.
    /// </summary>
    public LogCountSummary LogCounts { get; set; } = new();
}

/// <summary>
/// Log count summary for execution.
/// </summary>
public class LogCountSummary
{
    public int Total { get; set; }
    public int Trace { get; set; }
    public int Debug { get; set; }
    public int Info { get; set; }
    public int Warning { get; set; }
    public int Error { get; set; }
    public int Critical { get; set; }
}

/// <summary>
/// Start execution result.
/// </summary>
public class StartExecutionResult
{
    /// <summary>
    /// Execution ID.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Correlation ID.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Start timestamp.
    /// </summary>
    public DateTime StartedAt { get; set; }
}

/// <summary>
/// Running job info for dashboard.
/// </summary>
public class RunningJobInfo
{
    public Guid ExecutionId { get; set; }
    public string JobId { get; set; } = string.Empty;
    public string JobDisplayName { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public long RunningDurationMs { get; set; }
    public string RunningDurationFormatted { get; set; } = string.Empty;
    public long? MaxDurationMs { get; set; }
    public bool IsOverdue { get; set; }
}

/// <summary>
/// Job execution response DTO.
/// </summary>
public class JobExecutionResponse
{
    /// <summary>
    /// Execution ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Job ID.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Job display name.
    /// </summary>
    public string? JobDisplayName { get; set; }

    /// <summary>
    /// Start timestamp.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Completion timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Execution status.
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Status name for display.
    /// </summary>
    public string StatusName => Status.ToString();

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Server name where execution ran.
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// How the execution was triggered.
    /// </summary>
    public string TriggerType { get; set; } = string.Empty;

    /// <summary>
    /// Who or what triggered the execution.
    /// </summary>
    public string? TriggeredBy { get; set; }

    /// <summary>
    /// Correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error category if failed.
    /// </summary>
    public string? ErrorCategory { get; set; }

    /// <summary>
    /// Result code.
    /// </summary>
    public int? ResultCode { get; set; }

    /// <summary>
    /// Total log count.
    /// </summary>
    public int LogCount { get; set; }

    /// <summary>
    /// Trace log count.
    /// </summary>
    public int TraceCount { get; set; }

    /// <summary>
    /// Debug log count.
    /// </summary>
    public int DebugCount { get; set; }

    /// <summary>
    /// Info log count.
    /// </summary>
    public int InfoCount { get; set; }

    /// <summary>
    /// Warning log count.
    /// </summary>
    public int WarningCount { get; set; }

    /// <summary>
    /// Error log count.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Critical log count.
    /// </summary>
    public int CriticalCount { get; set; }

    /// <summary>
    /// Whether currently running.
    /// </summary>
    public bool IsRunning => Status == JobStatus.Pending || Status == JobStatus.Running;

    /// <summary>
    /// Whether execution was successful.
    /// </summary>
    public bool IsSuccess => Status == JobStatus.Completed;

    /// <summary>
    /// Whether execution failed.
    /// </summary>
    public bool IsFailed => Status == JobStatus.Failed || Status == JobStatus.Timeout;
}

/// <summary>
/// Detailed job execution response with additional information.
/// </summary>
public class JobExecutionDetailResponse
{
    /// <summary>
    /// The execution details.
    /// </summary>
    public JobExecutionResponse Execution { get; set; } = new();

    /// <summary>
    /// Job display name.
    /// </summary>
    public string? JobDisplayName { get; set; }

    /// <summary>
    /// Job category.
    /// </summary>
    public string? JobCategory { get; set; }

    /// <summary>
    /// Execution parameters.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// Result summary.
    /// </summary>
    public Dictionary<string, object>? ResultSummary { get; set; }

    /// <summary>
    /// Log counts by level.
    /// </summary>
    public Dictionary<string, int> LogCountByLevel { get; set; } = new();
}

/// <summary>
/// Execution statistics for a job.
/// </summary>
public class ExecutionStatistics
{
    /// <summary>
    /// Job ID.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Start of statistics period.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End of statistics period.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Total execution count.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Successful execution count.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Failed execution count.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Cancelled execution count.
    /// </summary>
    public int CancelledCount { get; set; }

    /// <summary>
    /// Timed out execution count.
    /// </summary>
    public int TimeoutCount { get; set; }

    /// <summary>
    /// Success rate as percentage.
    /// </summary>
    public decimal SuccessRate => TotalCount > 0 
        ? Math.Round((decimal)SuccessCount / TotalCount * 100, 2) 
        : 0;

    /// <summary>
    /// Failure rate as percentage.
    /// </summary>
    public decimal FailureRate => TotalCount > 0 
        ? Math.Round((decimal)FailedCount / TotalCount * 100, 2) 
        : 0;

    /// <summary>
    /// Average duration in milliseconds.
    /// </summary>
    public long? AvgDurationMs { get; set; }

    /// <summary>
    /// Minimum duration in milliseconds.
    /// </summary>
    public long? MinDurationMs { get; set; }

    /// <summary>
    /// Maximum duration in milliseconds.
    /// </summary>
    public long? MaxDurationMs { get; set; }

    /// <summary>
    /// P50 (median) duration in milliseconds.
    /// </summary>
    public long? P50DurationMs { get; set; }

    /// <summary>
    /// P95 duration in milliseconds.
    /// </summary>
    public long? P95DurationMs { get; set; }

    /// <summary>
    /// P99 duration in milliseconds.
    /// </summary>
    public long? P99DurationMs { get; set; }

    /// <summary>
    /// Total log count across all executions.
    /// </summary>
    public long TotalLogCount { get; set; }

    /// <summary>
    /// Total error log count.
    /// </summary>
    public long TotalErrorCount { get; set; }
}

/// <summary>
/// Single point in execution trend data.
/// </summary>
public class ExecutionTrendPoint
{
    /// <summary>
    /// Timestamp for this data point.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Time period label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Total executions in this period.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Successful executions.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Failed executions.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Average duration in milliseconds.
    /// </summary>
    public long? AvgDurationMs { get; set; }

    /// <summary>
    /// Success rate percentage.
    /// </summary>
    public decimal SuccessRate => TotalCount > 0 
        ? Math.Round((decimal)SuccessCount / TotalCount * 100, 2) 
        : 0;
}
