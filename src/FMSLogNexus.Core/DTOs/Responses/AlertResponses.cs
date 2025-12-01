using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.DTOs.Responses;

/// <summary>
/// Basic alert response.
/// </summary>
public class AlertResponse : AlertListItem
{
}

/// <summary>
/// Alert instance response.
/// </summary>
public class AlertInstanceResponse : AlertInstanceListItem
{
}

/// <summary>
/// Alert list item response.
/// </summary>
public class AlertListItem
{
    /// <summary>
    /// Alert ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Alert name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Alert type.
    /// </summary>
    public AlertType AlertType { get; set; }

    /// <summary>
    /// Alert type name.
    /// </summary>
    public string AlertTypeName => AlertType.ToString();

    /// <summary>
    /// Severity.
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Severity name.
    /// </summary>
    public string SeverityName => Severity.ToString();

    /// <summary>
    /// Severity color (for UI).
    /// </summary>
    public string SeverityColor => Severity switch
    {
        AlertSeverity.Low => "#6c757d",      // Gray
        AlertSeverity.Medium => "#ffc107",   // Yellow
        AlertSeverity.High => "#fd7e14",     // Orange
        AlertSeverity.Critical => "#dc3545", // Red
        _ => "#6c757d"
    };

    /// <summary>
    /// Whether active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Throttle minutes.
    /// </summary>
    public int ThrottleMinutes { get; set; }

    /// <summary>
    /// Last triggered timestamp.
    /// </summary>
    public DateTime? LastTriggeredAt { get; set; }

    /// <summary>
    /// Total trigger count.
    /// </summary>
    public int TriggerCount { get; set; }

    /// <summary>
    /// Job ID scope.
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// Server name scope.
    /// </summary>
    public string? ServerName { get; set; }
}

/// <summary>
/// Detailed alert response.
/// </summary>
public class AlertDetailResponse : AlertListItem
{
    /// <summary>
    /// Condition configuration.
    /// </summary>
    public object? Condition { get; set; }

    /// <summary>
    /// Notification channels.
    /// </summary>
    public NotificationChannelsResponse? NotificationChannels { get; set; }

    /// <summary>
    /// Recent instances.
    /// </summary>
    public List<AlertInstanceListItem> RecentInstances { get; set; } = new();

    /// <summary>
    /// Created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Notification channels configuration response.
/// </summary>
public class NotificationChannelsResponse
{
    /// <summary>
    /// Email addresses.
    /// </summary>
    public List<string> Email { get; set; } = new();

    /// <summary>
    /// Dashboard notifications enabled.
    /// </summary>
    public bool Dashboard { get; set; } = true;

    /// <summary>
    /// Webhook URLs.
    /// </summary>
    public List<string> Webhooks { get; set; } = new();
}

/// <summary>
/// Alert instance list item.
/// </summary>
public class AlertInstanceListItem
{
    /// <summary>
    /// Instance ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Alert ID.
    /// </summary>
    public Guid AlertId { get; set; }

    /// <summary>
    /// Alert name.
    /// </summary>
    public string? AlertName { get; set; }

    /// <summary>
    /// Triggered timestamp.
    /// </summary>
    public DateTime TriggeredAt { get; set; }

    /// <summary>
    /// Alert message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Severity.
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Severity name.
    /// </summary>
    public string SeverityName => Severity.ToString();

    /// <summary>
    /// Severity color.
    /// </summary>
    public string SeverityColor => Severity switch
    {
        AlertSeverity.Low => "#6c757d",
        AlertSeverity.Medium => "#ffc107",
        AlertSeverity.High => "#fd7e14",
        AlertSeverity.Critical => "#dc3545",
        _ => "#6c757d"
    };

    /// <summary>
    /// Status.
    /// </summary>
    public AlertInstanceStatus Status { get; set; }

    /// <summary>
    /// Status name.
    /// </summary>
    public string StatusName => Status.ToString();

    /// <summary>
    /// Job ID.
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// Server name.
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// Acknowledged timestamp.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Acknowledged by.
    /// </summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// Resolved timestamp.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Resolved by.
    /// </summary>
    public string? ResolvedBy { get; set; }

    /// <summary>
    /// Minutes since triggered.
    /// </summary>
    public int MinutesSinceTriggered => (int)(DateTime.UtcNow - TriggeredAt).TotalMinutes;
}

/// <summary>
/// Detailed alert instance response.
/// </summary>
public class AlertInstanceDetailResponse : AlertInstanceListItem
{
    /// <summary>
    /// Context data.
    /// </summary>
    public Dictionary<string, object>? Context { get; set; }

    /// <summary>
    /// Acknowledge note.
    /// </summary>
    public string? AcknowledgeNote { get; set; }

    /// <summary>
    /// Resolution note.
    /// </summary>
    public string? ResolutionNote { get; set; }

    /// <summary>
    /// Notifications sent.
    /// </summary>
    public List<NotificationSentResponse> NotificationsSent { get; set; } = new();
}

/// <summary>
/// Notification sent record.
/// </summary>
public class NotificationSentResponse
{
    /// <summary>
    /// Channel (Email, Webhook, etc.).
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Recipient.
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Sent timestamp.
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Whether successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Alert instance search result with summary.
/// </summary>
public class AlertInstanceSearchResult
{
    /// <summary>
    /// Instances.
    /// </summary>
    public List<AlertInstanceListItem> Items { get; set; } = new();

    /// <summary>
    /// Summary counts.
    /// </summary>
    public AlertSummary Summary { get; set; } = new();

    /// <summary>
    /// Pagination info.
    /// </summary>
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Alert summary counts.
/// </summary>
public class AlertSummary
{
    /// <summary>
    /// Total count.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// New (unacknowledged) count.
    /// </summary>
    public int NewCount { get; set; }

    /// <summary>
    /// Acknowledged count.
    /// </summary>
    public int AcknowledgedCount { get; set; }

    /// <summary>
    /// Resolved count.
    /// </summary>
    public int ResolvedCount { get; set; }

    /// <summary>
    /// Critical count.
    /// </summary>
    public int CriticalCount { get; set; }

    /// <summary>
    /// High count.
    /// </summary>
    public int HighCount { get; set; }
}
