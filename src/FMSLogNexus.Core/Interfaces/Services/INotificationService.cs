using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Interfaces.Services;

/// <summary>
/// Service interface for notification operations.
/// Handles sending notifications via email, webhooks, etc.
/// </summary>
public interface INotificationService
{
    #region Alert Notifications

    /// <summary>
    /// Sends notifications for an alert instance.
    /// </summary>
    /// <param name="alertInstance">Alert instance to notify about.</param>
    /// <param name="alert">Alert definition (for notification config).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Notification result.</returns>
    Task<NotificationResult> SendAlertNotificationAsync(
        AlertInstance alertInstance,
        Alert alert,
        CancellationToken cancellationToken = default);

    #endregion

    #region Email

    /// <summary>
    /// Sends an email notification.
    /// </summary>
    /// <param name="recipients">Email recipients.</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="body">Email body (HTML).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Send result.</returns>
    Task<bool> SendEmailAsync(
        IEnumerable<string> recipients,
        string subject,
        string body,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email using a template.
    /// </summary>
    /// <param name="templateName">Template name.</param>
    /// <param name="recipients">Email recipients.</param>
    /// <param name="model">Template model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Send result.</returns>
    Task<bool> SendTemplatedEmailAsync(
        string templateName,
        IEnumerable<string> recipients,
        object model,
        CancellationToken cancellationToken = default);

    #endregion

    #region Webhook

    /// <summary>
    /// Sends a webhook notification.
    /// </summary>
    /// <param name="webhookUrl">Webhook URL.</param>
    /// <param name="payload">JSON payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Send result.</returns>
    Task<WebhookResult> SendWebhookAsync(
        string webhookUrl,
        object payload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends webhook notifications to multiple URLs.
    /// </summary>
    /// <param name="webhookUrls">Webhook URLs.</param>
    /// <param name="payload">JSON payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results by URL.</returns>
    Task<Dictionary<string, WebhookResult>> SendWebhooksAsync(
        IEnumerable<string> webhookUrls,
        object payload,
        CancellationToken cancellationToken = default);

    #endregion

    #region Dashboard

    /// <summary>
    /// Pushes a notification to the dashboard (via SignalR).
    /// </summary>
    /// <param name="notification">Dashboard notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PushDashboardNotificationAsync(
        DashboardNotification notification,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Notification send result.
/// </summary>
public class NotificationResult
{
    public bool Success { get; set; }
    public int EmailsSent { get; set; }
    public int EmailsFailed { get; set; }
    public int WebhooksSent { get; set; }
    public int WebhooksFailed { get; set; }
    public bool DashboardNotified { get; set; }
    public List<NotificationError> Errors { get; set; } = new();
}

/// <summary>
/// Notification error detail.
/// </summary>
public class NotificationError
{
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Webhook send result.
/// </summary>
public class WebhookResult
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string? Error { get; set; }
    public long DurationMs { get; set; }
}

/// <summary>
/// Dashboard notification for real-time UI updates.
/// </summary>
public class DashboardNotification
{
    public string Type { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
