using System.ComponentModel.DataAnnotations;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.DTOs.Requests;

/// <summary>
/// Request to create an alert.
/// </summary>
public class CreateAlertRequest
{
    /// <summary>
    /// Alert name.
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Alert description.
    /// </summary>
    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Type of alert.
    /// </summary>
    [Required]
    public AlertType AlertType { get; set; }

    /// <summary>
    /// Alert severity.
    /// </summary>
    [Required]
    public AlertSeverity Severity { get; set; } = AlertSeverity.Medium;

    /// <summary>
    /// Alert condition configuration.
    /// </summary>
    [Required(ErrorMessage = "Condition is required")]
    public AlertConditionRequest Condition { get; set; } = new();

    /// <summary>
    /// Minimum minutes between alert triggers.
    /// </summary>
    [Range(1, 1440, ErrorMessage = "Throttle must be between 1 and 1440 minutes")]
    public int ThrottleMinutes { get; set; } = 15;

    /// <summary>
    /// Notification channel configuration.
    /// </summary>
    public NotificationChannelRequest? NotificationChannels { get; set; }

    /// <summary>
    /// Job ID if scoped to a specific job.
    /// </summary>
    [StringLength(100)]
    public string? JobId { get; set; }

    /// <summary>
    /// Server name if scoped to a specific server.
    /// </summary>
    [StringLength(100)]
    public string? ServerName { get; set; }
}

/// <summary>
/// Request to update an alert.
/// </summary>
public class UpdateAlertRequest
{
    /// <summary>
    /// Alert name.
    /// </summary>
    [StringLength(200, MinimumLength = 3)]
    public string? Name { get; set; }

    /// <summary>
    /// Alert description.
    /// </summary>
    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Alert severity.
    /// </summary>
    public AlertSeverity? Severity { get; set; }

    /// <summary>
    /// Alert condition configuration.
    /// </summary>
    public AlertConditionRequest? Condition { get; set; }

    /// <summary>
    /// Whether the alert is active.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Minimum minutes between alert triggers.
    /// </summary>
    [Range(1, 1440)]
    public int? ThrottleMinutes { get; set; }

    /// <summary>
    /// Notification channel configuration.
    /// </summary>
    public NotificationChannelRequest? NotificationChannels { get; set; }
}

/// <summary>
/// Alert condition configuration.
/// </summary>
public class AlertConditionRequest
{
    /// <summary>
    /// Error threshold condition.
    /// </summary>
    public ErrorThresholdConditionRequest? ErrorThreshold { get; set; }

    /// <summary>
    /// Job failure condition.
    /// </summary>
    public JobFailureConditionRequest? JobFailure { get; set; }

    /// <summary>
    /// Server offline condition.
    /// </summary>
    public ServerOfflineConditionRequest? ServerOffline { get; set; }

    /// <summary>
    /// Duration exceeded condition.
    /// </summary>
    public DurationExceededConditionRequest? DurationExceeded { get; set; }

    /// <summary>
    /// Pattern match condition.
    /// </summary>
    public PatternMatchConditionRequest? PatternMatch { get; set; }

    /// <summary>
    /// Custom query condition.
    /// </summary>
    public CustomQueryConditionRequest? CustomQuery { get; set; }
}

/// <summary>
/// Error threshold condition parameters.
/// </summary>
public class ErrorThresholdConditionRequest
{
    /// <summary>
    /// Error count threshold.
    /// </summary>
    [Required]
    [Range(1, 10000)]
    public int Threshold { get; set; } = 10;

    /// <summary>
    /// Time window in minutes.
    /// </summary>
    [Required]
    [Range(1, 1440)]
    public int WindowMinutes { get; set; } = 60;

    /// <summary>
    /// Minimum log level to count.
    /// </summary>
    public LogLevel Level { get; set; } = LogLevel.Error;
}

/// <summary>
/// Job failure condition parameters.
/// </summary>
public class JobFailureConditionRequest
{
    /// <summary>
    /// Number of consecutive failures before alerting.
    /// </summary>
    [Required]
    [Range(1, 100)]
    public int ConsecutiveFailures { get; set; } = 1;

    /// <summary>
    /// Include timeout as failure.
    /// </summary>
    public bool IncludeTimeout { get; set; } = true;
}

/// <summary>
/// Server offline condition parameters.
/// </summary>
public class ServerOfflineConditionRequest
{
    /// <summary>
    /// Minutes without heartbeat before alerting.
    /// </summary>
    [Required]
    [Range(1, 60)]
    public int TimeoutMinutes { get; set; } = 5;
}

/// <summary>
/// Duration exceeded condition parameters.
/// </summary>
public class DurationExceededConditionRequest
{
    /// <summary>
    /// Maximum duration in milliseconds.
    /// </summary>
    [Required]
    [Range(1000, long.MaxValue)]
    public long MaxDurationMs { get; set; }

    /// <summary>
    /// Or percentage over expected duration.
    /// </summary>
    [Range(100, 1000)]
    public int? PercentageOverExpected { get; set; }
}

/// <summary>
/// Pattern match condition parameters.
/// </summary>
public class PatternMatchConditionRequest
{
    /// <summary>
    /// Pattern to match (regex or simple text).
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Whether pattern is a regex.
    /// </summary>
    public bool IsRegex { get; set; }

    /// <summary>
    /// Number of matches before alerting.
    /// </summary>
    [Range(1, 1000)]
    public int MatchCount { get; set; } = 1;

    /// <summary>
    /// Time window in minutes.
    /// </summary>
    [Range(1, 1440)]
    public int WindowMinutes { get; set; } = 60;
}

/// <summary>
/// Custom query condition parameters.
/// </summary>
public class CustomQueryConditionRequest
{
    /// <summary>
    /// SQL query that returns a count.
    /// </summary>
    [Required]
    [StringLength(4000)]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Threshold for alert.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int Threshold { get; set; } = 1;
}

/// <summary>
/// Notification channel configuration.
/// </summary>
public class NotificationChannelRequest
{
    /// <summary>
    /// Email addresses to notify.
    /// </summary>
    public List<string>? Email { get; set; }

    /// <summary>
    /// Whether to show on dashboard.
    /// </summary>
    public bool Dashboard { get; set; } = true;

    /// <summary>
    /// Webhook URLs to call.
    /// </summary>
    public List<string>? Webhooks { get; set; }
}

/// <summary>
/// Request to acknowledge an alert instance.
/// </summary>
public class AcknowledgeAlertRequest
{
    /// <summary>
    /// Optional note when acknowledging.
    /// </summary>
    [StringLength(1000)]
    public string? Note { get; set; }
}

/// <summary>
/// Request to resolve an alert instance.
/// </summary>
public class ResolveAlertRequest
{
    /// <summary>
    /// Optional resolution note.
    /// </summary>
    [StringLength(1000)]
    public string? Note { get; set; }
}

/// <summary>
/// Request to search alert instances.
/// </summary>
public class AlertInstanceSearchRequest : PaginationRequest
{
    /// <summary>
    /// Filter by alert definition ID.
    /// </summary>
    public Guid? AlertId { get; set; }

    /// <summary>
    /// Filter by status.
    /// </summary>
    public AlertInstanceStatus? Status { get; set; }

    /// <summary>
    /// Filter by severity.
    /// </summary>
    public AlertSeverity? Severity { get; set; }

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
    /// Filter by triggered date start.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Filter by triggered date end.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter to unresolved only.
    /// </summary>
    public bool? UnresolvedOnly { get; set; }
}
