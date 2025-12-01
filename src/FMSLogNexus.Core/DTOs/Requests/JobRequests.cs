using System.ComponentModel.DataAnnotations;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.DTOs.Requests;

/// <summary>
/// Request to register a job (alias for CreateJobRequest for client SDK).
/// </summary>
public class RegisterJobRequest : CreateJobRequest
{
}

/// <summary>
/// Request to create or register a job.
/// </summary>
public class CreateJobRequest
{
    /// <summary>
    /// Unique job identifier.
    /// </summary>
    [Required(ErrorMessage = "Job ID is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Job ID must be between 1 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Job ID can only contain letters, numbers, underscores, and hyphens")]
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    [Required(ErrorMessage = "Display name is required")]
    [StringLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the job.
    /// </summary>
    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Category for grouping.
    /// </summary>
    [StringLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Type of job.
    /// </summary>
    public JobType JobType { get; set; } = JobType.Unknown;

    /// <summary>
    /// Server the job runs on.
    /// </summary>
    [StringLength(100)]
    public string? ServerName { get; set; }

    /// <summary>
    /// Path to executable or script.
    /// </summary>
    [StringLength(1000)]
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// Schedule (cron expression).
    /// </summary>
    [StringLength(100)]
    public string? Schedule { get; set; }

    /// <summary>
    /// Human-readable schedule description.
    /// </summary>
    [StringLength(200)]
    public string? ScheduleDescription { get; set; }

    /// <summary>
    /// Whether this is a critical job.
    /// </summary>
    public bool IsCritical { get; set; }

    /// <summary>
    /// Expected duration in milliseconds.
    /// </summary>
    [Range(0, long.MaxValue)]
    public long? ExpectedDurationMs { get; set; }

    /// <summary>
    /// Maximum allowed duration in milliseconds.
    /// </summary>
    [Range(0, long.MaxValue)]
    public long? MaxDurationMs { get; set; }

    /// <summary>
    /// Minimum expected executions per day.
    /// </summary>
    [Range(0, 1440)]
    public int? MinExecutionsPerDay { get; set; }

    /// <summary>
    /// Additional configuration.
    /// </summary>
    public Dictionary<string, object>? Configuration { get; set; }
}

/// <summary>
/// Request to update a job.
/// </summary>
public class UpdateJobRequest
{
    /// <summary>
    /// Display name.
    /// </summary>
    [StringLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Description of the job.
    /// </summary>
    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Category for grouping.
    /// </summary>
    [StringLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Type of job.
    /// </summary>
    public JobType? JobType { get; set; }

    /// <summary>
    /// Server the job runs on.
    /// </summary>
    [StringLength(100)]
    public string? ServerName { get; set; }

    /// <summary>
    /// Path to executable or script.
    /// </summary>
    [StringLength(1000)]
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// Schedule (cron expression).
    /// </summary>
    [StringLength(100)]
    public string? Schedule { get; set; }

    /// <summary>
    /// Human-readable schedule description.
    /// </summary>
    [StringLength(200)]
    public string? ScheduleDescription { get; set; }

    /// <summary>
    /// Whether the job is active.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Whether this is a critical job.
    /// </summary>
    public bool? IsCritical { get; set; }

    /// <summary>
    /// Expected duration in milliseconds.
    /// </summary>
    [Range(0, long.MaxValue)]
    public long? ExpectedDurationMs { get; set; }

    /// <summary>
    /// Maximum allowed duration in milliseconds.
    /// </summary>
    [Range(0, long.MaxValue)]
    public long? MaxDurationMs { get; set; }

    /// <summary>
    /// Minimum expected executions per day.
    /// </summary>
    [Range(0, 1440)]
    public int? MinExecutionsPerDay { get; set; }

    /// <summary>
    /// Additional configuration.
    /// </summary>
    public Dictionary<string, object>? Configuration { get; set; }
}

/// <summary>
/// Request to start a job execution.
/// </summary>
public class StartExecutionRequest
{
    /// <summary>
    /// Job ID to execute.
    /// </summary>
    [Required(ErrorMessage = "Job ID is required")]
    [StringLength(100)]
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Server where the job is running.
    /// </summary>
    [StringLength(100)]
    public string? ServerName { get; set; }

    /// <summary>
    /// How the execution was triggered.
    /// </summary>
    [StringLength(50)]
    public string? TriggerType { get; set; }

    /// <summary>
    /// Who/what triggered the execution.
    /// </summary>
    [StringLength(200)]
    public string? TriggeredBy { get; set; }

    /// <summary>
    /// Correlation ID (generated if not provided).
    /// </summary>
    [StringLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Input parameters.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
}

/// <summary>
/// Request to complete a job execution.
/// </summary>
public class CompleteExecutionRequest
{
    /// <summary>
    /// Final status.
    /// </summary>
    [Required]
    public JobStatus Status { get; set; } = JobStatus.Completed;

    /// <summary>
    /// Result summary.
    /// </summary>
    public Dictionary<string, object>? ResultSummary { get; set; }

    /// <summary>
    /// Numeric result/exit code.
    /// </summary>
    public int? ResultCode { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    [StringLength(4000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error category if failed.
    /// </summary>
    [StringLength(100)]
    public string? ErrorCategory { get; set; }
}

/// <summary>
/// Request to search jobs.
/// </summary>
public class JobSearchRequest : PaginationRequest
{
    /// <summary>
    /// Filter by server name.
    /// </summary>
    [StringLength(100)]
    public string? ServerName { get; set; }

    /// <summary>
    /// Filter by category.
    /// </summary>
    [StringLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Filter by job type.
    /// </summary>
    public JobType? JobType { get; set; }

    /// <summary>
    /// Filter by active status.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Filter by critical status.
    /// </summary>
    public bool? IsCritical { get; set; }

    /// <summary>
    /// Filter by last status.
    /// </summary>
    public JobStatus? LastStatus { get; set; }

    /// <summary>
    /// Text search in job ID, name, description.
    /// </summary>
    [StringLength(200)]
    public string? Search { get; set; }

    /// <summary>
    /// Filter by tags (any match).
    /// </summary>
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Request to search job executions.
/// </summary>
public class ExecutionSearchRequest : PaginationRequest
{
    /// <summary>
    /// Filter by job ID.
    /// </summary>
    [StringLength(100)]
    public string? JobId { get; set; }

    /// <summary>
    /// Filter by server name.
    /// </summary>
    [StringLength(100)]
    public string? ServerName { get; set; }

    /// <summary>
    /// Filter by status.
    /// </summary>
    public JobStatus? Status { get; set; }

    /// <summary>
    /// Filter by start date.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Filter by end date.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by trigger type.
    /// </summary>
    [StringLength(50)]
    public string? TriggerType { get; set; }

    /// <summary>
    /// Filter to only running executions.
    /// </summary>
    public bool? IsRunning { get; set; }

    /// <summary>
    /// Filter to only failed executions.
    /// </summary>
    public bool? IsFailed { get; set; }
}
