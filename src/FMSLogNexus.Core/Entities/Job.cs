using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Entities;

/// <summary>
/// Represents a job definition in the system.
/// Maps to [job].[Jobs] table.
/// </summary>
public class Job : IEntity<string>, IModificationAuditable, IActivatable
{
    /// <summary>
    /// Unique job identifier (e.g., "IMPORT-CUSTOMER-001").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Alias for Id to maintain naming consistency with database.
    /// </summary>
    public string JobId
    {
        get => Id;
        set => Id = value;
    }

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of what the job does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category for grouping jobs (e.g., "Data Import", "Reports").
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Comma-separated tags for additional categorization.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Type of job (Executable, PowerShell, VBScript, etc.).
    /// </summary>
    public JobType JobType { get; set; } = JobType.Unknown;

    /// <summary>
    /// The server this job is associated with.
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// Path to the executable or script.
    /// </summary>
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// Cron expression or schedule description.
    /// </summary>
    public string? Schedule { get; set; }

    /// <summary>
    /// Human-readable schedule description.
    /// </summary>
    public string? ScheduleDescription { get; set; }

    /// <summary>
    /// Whether the job is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a critical/business-critical job.
    /// </summary>
    public bool IsCritical { get; set; }

    /// <summary>
    /// JSON configuration for the job.
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// Expected duration in milliseconds.
    /// </summary>
    public long? ExpectedDurationMs { get; set; }

    /// <summary>
    /// Maximum allowed duration in milliseconds before timeout alert.
    /// </summary>
    public long? MaxDurationMs { get; set; }

    /// <summary>
    /// Minimum expected executions per day for SLA monitoring.
    /// </summary>
    public int? MinExecutionsPerDay { get; set; }

    // -------------------------------------------------------------------------
    // Statistics
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reference to the last execution ID.
    /// </summary>
    public Guid? LastExecutionId { get; set; }

    /// <summary>
    /// Timestamp of the last execution.
    /// </summary>
    public DateTime? LastExecutionAt { get; set; }

    /// <summary>
    /// Status of the last execution.
    /// </summary>
    public JobStatus? LastStatus { get; set; }

    /// <summary>
    /// Duration of the last execution in milliseconds.
    /// </summary>
    public long? LastDurationMs { get; set; }

    /// <summary>
    /// Total number of executions.
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Number of successful executions.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed executions.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Average duration in milliseconds.
    /// </summary>
    public long? AvgDurationMs { get; set; }

    // -------------------------------------------------------------------------
    // Audit Fields
    // -------------------------------------------------------------------------

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }

    // -------------------------------------------------------------------------
    // Navigation Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// The server this job runs on.
    /// </summary>
    public virtual Server? Server { get; set; }

    /// <summary>
    /// Collection of job executions.
    /// </summary>
    public virtual ICollection<JobExecution> Executions { get; set; } = new List<JobExecution>();

    /// <summary>
    /// Collection of log entries for this job.
    /// </summary>
    public virtual ICollection<LogEntry> LogEntries { get; set; } = new List<LogEntry>();

    /// <summary>
    /// Alerts configured for this job.
    /// </summary>
    public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();

    // -------------------------------------------------------------------------
    // Computed Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// Success rate as a percentage (0-100).
    /// </summary>
    public decimal? SuccessRate =>
        TotalExecutions > 0 ? Math.Round((decimal)SuccessCount / TotalExecutions * 100, 2) : null;

    /// <summary>
    /// Gets the tags as a list.
    /// </summary>
    public List<string> GetTagsList()
    {
        if (string.IsNullOrEmpty(Tags))
            return new List<string>();

        return Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .ToList();
    }

    /// <summary>
    /// Gets the configuration as a dictionary.
    /// </summary>
    public Dictionary<string, object>? GetConfigurationAsDictionary()
    {
        if (string.IsNullOrEmpty(Configuration))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Configuration);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Updates statistics after an execution completes.
    /// </summary>
    public void UpdateStatistics(JobExecution execution)
    {
        LastExecutionId = execution.Id;
        LastExecutionAt = execution.StartedAt;
        LastStatus = execution.Status;
        LastDurationMs = execution.DurationMs;

        TotalExecutions++;

        if (execution.Status == JobStatus.Completed)
            SuccessCount++;
        else if (execution.Status == JobStatus.Failed || execution.Status == JobStatus.Timeout)
            FailureCount++;

        // Update average duration
        if (execution.DurationMs.HasValue)
        {
            if (AvgDurationMs.HasValue)
            {
                AvgDurationMs = (AvgDurationMs * (TotalExecutions - 1) + execution.DurationMs) / TotalExecutions;
            }
            else
            {
                AvgDurationMs = execution.DurationMs;
            }
        }

        UpdatedAt = DateTime.UtcNow;
    }
}
