using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Entities;

/// <summary>
/// Represents an alert rule definition.
/// Maps to [alert].[Alerts] table.
/// </summary>
public class Alert : AuditableEntity, IActivatable
{
    /// <summary>
    /// Alert name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Alert description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of alert.
    /// </summary>
    public AlertType AlertType { get; set; }

    /// <summary>
    /// Alert severity level.
    /// </summary>
    public AlertSeverity Severity { get; set; } = AlertSeverity.Medium;

    /// <summary>
    /// Alert condition as JSON.
    /// </summary>
    public string Condition { get; set; } = "{}";

    /// <summary>
    /// Whether the alert is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Minimum minutes between alert triggers (throttling).
    /// </summary>
    public int ThrottleMinutes { get; set; } = 15;

    /// <summary>
    /// Last time the alert was triggered.
    /// </summary>
    public DateTime? LastTriggeredAt { get; set; }

    /// <summary>
    /// Total number of times this alert has been triggered.
    /// </summary>
    public int TriggerCount { get; set; }

    /// <summary>
    /// Notification channels as JSON (email addresses, webhooks, etc.).
    /// </summary>
    public string? NotificationChannels { get; set; }

    /// <summary>
    /// Job ID if this alert is scoped to a specific job.
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// Server name if this alert is scoped to a specific server.
    /// </summary>
    public string? ServerName { get; set; }

    // -------------------------------------------------------------------------
    // Navigation Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// The job this alert is associated with (if any).
    /// </summary>
    public virtual Job? Job { get; set; }

    /// <summary>
    /// The server this alert is associated with (if any).
    /// </summary>
    public virtual Server? Server { get; set; }

    /// <summary>
    /// Alert instances triggered by this alert.
    /// </summary>
    public virtual ICollection<AlertInstance> Instances { get; set; } = new List<AlertInstance>();

    // -------------------------------------------------------------------------
    // Computed Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// Indicates if the alert can be triggered (not throttled).
    /// </summary>
    public bool CanTrigger
    {
        get
        {
            if (!IsActive)
                return false;

            if (!LastTriggeredAt.HasValue)
                return true;

            return DateTime.UtcNow >= LastTriggeredAt.Value.AddMinutes(ThrottleMinutes);
        }
    }

    /// <summary>
    /// Minutes until throttle expires (0 if not throttled).
    /// </summary>
    public int ThrottleRemainingMinutes
    {
        get
        {
            if (!LastTriggeredAt.HasValue)
                return 0;

            var expiresAt = LastTriggeredAt.Value.AddMinutes(ThrottleMinutes);
            var remaining = (expiresAt - DateTime.UtcNow).TotalMinutes;
            return remaining > 0 ? (int)remaining : 0;
        }
    }

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets the condition as a typed object.
    /// </summary>
    public T? GetCondition<T>() where T : class
    {
        if (string.IsNullOrEmpty(Condition))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(Condition);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets the condition from an object.
    /// </summary>
    public void SetCondition<T>(T condition) where T : class
    {
        Condition = System.Text.Json.JsonSerializer.Serialize(condition);
    }

    /// <summary>
    /// Gets notification channels as a typed object.
    /// </summary>
    public NotificationChannelConfig? GetNotificationChannels()
    {
        if (string.IsNullOrEmpty(NotificationChannels))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<NotificationChannelConfig>(NotificationChannels);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets notification channels.
    /// </summary>
    public void SetNotificationChannels(NotificationChannelConfig channels)
    {
        NotificationChannels = System.Text.Json.JsonSerializer.Serialize(channels);
    }

    /// <summary>
    /// Creates a new alert instance and updates trigger info.
    /// </summary>
    public AlertInstance Trigger(string message, Dictionary<string, object>? context = null)
    {
        var instance = new AlertInstance
        {
            AlertId = Id,
            TriggeredAt = DateTime.UtcNow,
            Message = message,
            Severity = Severity,
            JobId = JobId,
            ServerName = ServerName,
            Status = AlertInstanceStatus.New
        };

        if (context != null)
        {
            instance.Context = System.Text.Json.JsonSerializer.Serialize(context);
        }

        LastTriggeredAt = instance.TriggeredAt;
        TriggerCount++;
        UpdatedAt = DateTime.UtcNow;

        return instance;
    }
}

/// <summary>
/// Configuration for notification channels.
/// </summary>
public class NotificationChannelConfig
{
    /// <summary>
    /// Email addresses to notify.
    /// </summary>
    public List<string> Email { get; set; } = new();

    /// <summary>
    /// Whether to show on dashboard.
    /// </summary>
    public bool Dashboard { get; set; } = true;

    /// <summary>
    /// Webhook URLs to call.
    /// </summary>
    public List<string> Webhooks { get; set; } = new();
}

/// <summary>
/// Base class for alert conditions.
/// </summary>
public abstract class AlertConditionBase
{
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Error threshold alert condition.
/// </summary>
public class ErrorThresholdCondition : AlertConditionBase
{
    public int Threshold { get; set; } = 10;
    public int WindowMinutes { get; set; } = 60;
    public int Level { get; set; } = 4; // Error
}

/// <summary>
/// Job failure alert condition.
/// </summary>
public class JobFailureCondition : AlertConditionBase
{
    public string? JobId { get; set; }
    public int ConsecutiveFailures { get; set; } = 1;
}

/// <summary>
/// Server offline alert condition.
/// </summary>
public class ServerOfflineCondition : AlertConditionBase
{
    public int TimeoutMinutes { get; set; } = 5;
}
