using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Entities;

/// <summary>
/// Represents a single execution instance of a job.
/// Maps to [job].[JobExecutions] table.
/// </summary>
public class JobExecution : EntityBase
{
    /// <summary>
    /// The job ID this execution belongs to.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the execution started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when the execution completed (null if still running).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Current status of the execution.
    /// </summary>
    public JobStatus Status { get; set; } = JobStatus.Pending;

    /// <summary>
    /// Server where the execution is running.
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// How the execution was triggered.
    /// </summary>
    public string TriggerType { get; set; } = "Manual";

    /// <summary>
    /// Who/what triggered the execution.
    /// </summary>
    public string? TriggeredBy { get; set; }

    /// <summary>
    /// Correlation ID for tracking related operations.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Input parameters as JSON.
    /// </summary>
    public string? Parameters { get; set; }

    /// <summary>
    /// Execution result summary as JSON.
    /// </summary>
    public string? ResultSummary { get; set; }

    /// <summary>
    /// Numeric result/exit code.
    /// </summary>
    public int? ResultCode { get; set; }

    /// <summary>
    /// Error message if the execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Category of error if failed.
    /// </summary>
    public string? ErrorCategory { get; set; }

    // -------------------------------------------------------------------------
    // Log Counts
    // -------------------------------------------------------------------------

    /// <summary>
    /// Total number of log entries for this execution.
    /// </summary>
    public int LogCount { get; set; }

    /// <summary>
    /// Number of Trace-level logs.
    /// </summary>
    public int TraceCount { get; set; }

    /// <summary>
    /// Number of Debug-level logs.
    /// </summary>
    public int DebugCount { get; set; }

    /// <summary>
    /// Number of Information-level logs.
    /// </summary>
    public int InfoCount { get; set; }

    /// <summary>
    /// Number of Warning-level logs.
    /// </summary>
    public int WarningCount { get; set; }

    /// <summary>
    /// Number of Error-level logs.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Number of Critical-level logs.
    /// </summary>
    public int CriticalCount { get; set; }

    // -------------------------------------------------------------------------
    // Navigation Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// The job this execution belongs to.
    /// </summary>
    public virtual Job? Job { get; set; }

    /// <summary>
    /// The server where this execution ran.
    /// </summary>
    public virtual Server? Server { get; set; }

    /// <summary>
    /// Log entries for this execution.
    /// </summary>
    public virtual ICollection<LogEntry> LogEntries { get; set; } = new List<LogEntry>();

    // -------------------------------------------------------------------------
    // Computed Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// Duration in milliseconds (null if not completed).
    /// </summary>
    public long? DurationMs => CompletedAt.HasValue
        ? (long)(CompletedAt.Value - StartedAt).TotalMilliseconds
        : null;

    /// <summary>
    /// Duration formatted as human-readable string.
    /// </summary>
    public string? DurationFormatted => DurationMs.HasValue
        ? FormatDuration(DurationMs.Value)
        : null;

    /// <summary>
    /// Running duration in milliseconds (for in-progress executions).
    /// </summary>
    public long RunningDurationMs => (long)(DateTime.UtcNow - StartedAt).TotalMilliseconds;

    /// <summary>
    /// Indicates if the execution is still running.
    /// </summary>
    public bool IsRunning => Status == JobStatus.Pending || Status == JobStatus.Running;

    /// <summary>
    /// Indicates if the execution completed successfully.
    /// </summary>
    public bool IsSuccess => Status == JobStatus.Completed;

    /// <summary>
    /// Indicates if the execution failed.
    /// </summary>
    public bool IsFailed => Status == JobStatus.Failed || Status == JobStatus.Timeout;

    /// <summary>
    /// Indicates if there were any errors logged.
    /// </summary>
    public bool HasErrors => ErrorCount > 0 || CriticalCount > 0;

    /// <summary>
    /// Indicates if there were any warnings logged.
    /// </summary>
    public bool HasWarnings => WarningCount > 0;

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Starts the execution (sets status to Running).
    /// </summary>
    public void Start()
    {
        Status = JobStatus.Running;
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Completes the execution successfully.
    /// </summary>
    public void Complete(object? resultSummary = null)
    {
        Status = JobStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ResultCode = 0;

        if (resultSummary != null)
        {
            ResultSummary = System.Text.Json.JsonSerializer.Serialize(resultSummary);
        }
    }

    /// <summary>
    /// Marks the execution as failed.
    /// </summary>
    public void Fail(string errorMessage, string? errorCategory = null, int? resultCode = null)
    {
        Status = JobStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        ErrorCategory = errorCategory;
        ResultCode = resultCode ?? 1;
    }

    /// <summary>
    /// Marks the execution as timed out.
    /// </summary>
    public void Timeout()
    {
        Status = JobStatus.Timeout;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = "Execution exceeded maximum allowed duration";
        ErrorCategory = "Timeout";
        ResultCode = -1;
    }

    /// <summary>
    /// Marks the execution as cancelled.
    /// </summary>
    public void Cancel(string? reason = null)
    {
        Status = JobStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = reason ?? "Execution was cancelled";
        ErrorCategory = "Cancelled";
        ResultCode = -2;
    }

    /// <summary>
    /// Increments log count for the specified level.
    /// </summary>
    public void IncrementLogCount(LogLevel level)
    {
        LogCount++;
        switch (level)
        {
            case LogLevel.Trace:
                TraceCount++;
                break;
            case LogLevel.Debug:
                DebugCount++;
                break;
            case LogLevel.Information:
                InfoCount++;
                break;
            case LogLevel.Warning:
                WarningCount++;
                break;
            case LogLevel.Error:
                ErrorCount++;
                break;
            case LogLevel.Critical:
                CriticalCount++;
                break;
        }
    }

    /// <summary>
    /// Gets the parameters as a dictionary.
    /// </summary>
    public Dictionary<string, object>? GetParametersAsDictionary()
    {
        if (string.IsNullOrEmpty(Parameters))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Parameters);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the result summary as a dictionary.
    /// </summary>
    public Dictionary<string, object>? GetResultSummaryAsDictionary()
    {
        if (string.IsNullOrEmpty(ResultSummary))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(ResultSummary);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Formats duration in milliseconds to human-readable string.
    /// </summary>
    private static string FormatDuration(long milliseconds)
    {
        if (milliseconds < 1000)
            return $"{milliseconds}ms";

        if (milliseconds < 60000)
            return $"{milliseconds / 1000.0:F1}s";

        if (milliseconds < 3600000)
            return $"{milliseconds / 60000}m {(milliseconds % 60000) / 1000}s";

        return $"{milliseconds / 3600000}h {(milliseconds % 3600000) / 60000}m";
    }
}
