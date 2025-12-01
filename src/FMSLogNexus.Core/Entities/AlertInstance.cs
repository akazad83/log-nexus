using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Entities;

/// <summary>
/// Represents a triggered alert instance.
/// Maps to [alert].[AlertInstances] table.
/// </summary>
public class AlertInstance : EntityBase
{
    /// <summary>
    /// Reference to the alert definition.
    /// </summary>
    public Guid AlertId { get; set; }

    /// <summary>
    /// UTC timestamp when the alert was triggered.
    /// </summary>
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Alert message describing the trigger reason.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Severity level of this instance.
    /// </summary>
    public AlertSeverity Severity { get; set; } = AlertSeverity.Medium;

    /// <summary>
    /// Current status of the alert instance.
    /// </summary>
    public AlertInstanceStatus Status { get; set; } = AlertInstanceStatus.New;

    /// <summary>
    /// Additional context as JSON.
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Job ID if this alert is related to a specific job.
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// Server name if this alert is related to a specific server.
    /// </summary>
    public string? ServerName { get; set; }

    // -------------------------------------------------------------------------
    // Acknowledgement
    // -------------------------------------------------------------------------

    /// <summary>
    /// UTC timestamp when the alert was acknowledged.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Username who acknowledged the alert.
    /// </summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// Note added when acknowledging.
    /// </summary>
    public string? AcknowledgeNote { get; set; }

    // -------------------------------------------------------------------------
    // Resolution
    // -------------------------------------------------------------------------

    /// <summary>
    /// UTC timestamp when the alert was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Username who resolved the alert.
    /// </summary>
    public string? ResolvedBy { get; set; }

    /// <summary>
    /// Note added when resolving.
    /// </summary>
    public string? ResolutionNote { get; set; }

    // -------------------------------------------------------------------------
    // Notifications
    // -------------------------------------------------------------------------

    /// <summary>
    /// Record of notifications sent as JSON.
    /// </summary>
    public string? NotificationsSent { get; set; }

    // -------------------------------------------------------------------------
    // Navigation Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// The alert definition that triggered this instance.
    /// </summary>
    public virtual Alert? Alert { get; set; }

    /// <summary>
    /// The job associated with this alert (if any).
    /// </summary>
    public virtual Job? Job { get; set; }

    /// <summary>
    /// The server associated with this alert (if any).
    /// </summary>
    public virtual Server? Server { get; set; }

    // -------------------------------------------------------------------------
    // Computed Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// Minutes since the alert was triggered.
    /// </summary>
    public int MinutesSinceTriggered => (int)(DateTime.UtcNow - TriggeredAt).TotalMinutes;

    /// <summary>
    /// Indicates if the alert is new/unacknowledged.
    /// </summary>
    public bool IsNew => Status == AlertInstanceStatus.New;

    /// <summary>
    /// Indicates if the alert has been acknowledged.
    /// </summary>
    public bool IsAcknowledged => Status == AlertInstanceStatus.Acknowledged;

    /// <summary>
    /// Indicates if the alert has been resolved.
    /// </summary>
    public bool IsResolved => Status == AlertInstanceStatus.Resolved;

    /// <summary>
    /// Indicates if the alert is active (not resolved or suppressed).
    /// </summary>
    public bool IsActive => Status == AlertInstanceStatus.New || Status == AlertInstanceStatus.Acknowledged;

    /// <summary>
    /// Time to acknowledge in minutes (null if not acknowledged).
    /// </summary>
    public int? TimeToAcknowledgeMinutes => AcknowledgedAt.HasValue
        ? (int)(AcknowledgedAt.Value - TriggeredAt).TotalMinutes
        : null;

    /// <summary>
    /// Time to resolve in minutes (null if not resolved).
    /// </summary>
    public int? TimeToResolveMinutes => ResolvedAt.HasValue
        ? (int)(ResolvedAt.Value - TriggeredAt).TotalMinutes
        : null;

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Acknowledges the alert.
    /// </summary>
    public bool Acknowledge(string username, string? note = null)
    {
        if (Status != AlertInstanceStatus.New)
            return false;

        Status = AlertInstanceStatus.Acknowledged;
        AcknowledgedAt = DateTime.UtcNow;
        AcknowledgedBy = username;
        AcknowledgeNote = note;

        return true;
    }

    /// <summary>
    /// Resolves the alert.
    /// </summary>
    public bool Resolve(string username, string? note = null)
    {
        if (Status == AlertInstanceStatus.Resolved || Status == AlertInstanceStatus.Suppressed)
            return false;

        Status = AlertInstanceStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = username;
        ResolutionNote = note;

        // Auto-acknowledge if not already
        if (!AcknowledgedAt.HasValue)
        {
            AcknowledgedAt = ResolvedAt;
            AcknowledgedBy = username;
        }

        return true;
    }

    /// <summary>
    /// Suppresses the alert.
    /// </summary>
    public bool Suppress(string username, string? note = null)
    {
        if (Status == AlertInstanceStatus.Resolved || Status == AlertInstanceStatus.Suppressed)
            return false;

        Status = AlertInstanceStatus.Suppressed;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = username;
        ResolutionNote = note ?? "Suppressed";

        return true;
    }

    /// <summary>
    /// Gets the context as a dictionary.
    /// </summary>
    public Dictionary<string, object>? GetContextAsDictionary()
    {
        if (string.IsNullOrEmpty(Context))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Context);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Records that a notification was sent.
    /// </summary>
    public void RecordNotification(string channel, string recipient, bool success, string? errorMessage = null)
    {
        var notifications = GetNotificationsList();

        notifications.Add(new NotificationRecord
        {
            Channel = channel,
            Recipient = recipient,
            SentAt = DateTime.UtcNow,
            Success = success,
            ErrorMessage = errorMessage
        });

        NotificationsSent = System.Text.Json.JsonSerializer.Serialize(notifications);
    }

    /// <summary>
    /// Gets the list of sent notifications.
    /// </summary>
    public List<NotificationRecord> GetNotificationsList()
    {
        if (string.IsNullOrEmpty(NotificationsSent))
            return new List<NotificationRecord>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<NotificationRecord>>(NotificationsSent)
                ?? new List<NotificationRecord>();
        }
        catch
        {
            return new List<NotificationRecord>();
        }
    }
}

/// <summary>
/// Record of a notification sent for an alert.
/// </summary>
public class NotificationRecord
{
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
