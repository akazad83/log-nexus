using Microsoft.AspNetCore.SignalR;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Api.Hubs;

/// <summary>
/// Hub for real-time alert notifications.
/// </summary>
public class AlertHub : BaseHub<IAlertHubClient>
{
    private const string HubName = "AlertHub";
    private const string AllAlertsGroup = "alerts:all";
    private const string CriticalAlertsGroup = "alerts:critical";
    private const string HighAlertsGroup = "alerts:high";

    public AlertHub(
        ILogger<AlertHub> logger,
        IConnectionManager connectionManager)
        : base(logger, connectionManager)
    {
    }

    protected override string GetHubName() => HubName;

    #region Subscription Methods

    /// <summary>
    /// Subscribes to all alert notifications.
    /// </summary>
    public async Task SubscribeToAllAlerts()
    {
        await JoinGroupAsync(AllAlertsGroup);
        _logger.LogInformation("User {UserId} subscribed to all alerts", CurrentUserId);
    }

    /// <summary>
    /// Unsubscribes from all alert notifications.
    /// </summary>
    public async Task UnsubscribeFromAllAlerts()
    {
        await LeaveGroupAsync(AllAlertsGroup);
        _logger.LogInformation("User {UserId} unsubscribed from all alerts", CurrentUserId);
    }

    /// <summary>
    /// Subscribes to critical alerts only.
    /// </summary>
    public async Task SubscribeToCriticalAlerts()
    {
        await JoinGroupAsync(CriticalAlertsGroup);
        _logger.LogInformation("User {UserId} subscribed to critical alerts", CurrentUserId);
    }

    /// <summary>
    /// Unsubscribes from critical alerts.
    /// </summary>
    public async Task UnsubscribeFromCriticalAlerts()
    {
        await LeaveGroupAsync(CriticalAlertsGroup);
        _logger.LogInformation("User {UserId} unsubscribed from critical alerts", CurrentUserId);
    }

    /// <summary>
    /// Subscribes to high priority alerts and above.
    /// </summary>
    public async Task SubscribeToHighPriorityAlerts()
    {
        await JoinGroupAsync(HighAlertsGroup);
        _logger.LogInformation("User {UserId} subscribed to high priority alerts", CurrentUserId);
    }

    /// <summary>
    /// Unsubscribes from high priority alerts.
    /// </summary>
    public async Task UnsubscribeFromHighPriorityAlerts()
    {
        await LeaveGroupAsync(HighAlertsGroup);
        _logger.LogInformation("User {UserId} unsubscribed from high priority alerts", CurrentUserId);
    }

    /// <summary>
    /// Subscribes to alerts for a specific job.
    /// </summary>
    public async Task SubscribeToJobAlerts(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new HubException("Job ID is required.");

        var groupName = GetJobGroup(jobId);
        await JoinGroupAsync(groupName);
        _logger.LogInformation("User {UserId} subscribed to job {Job} alerts", CurrentUserId, jobId);
    }

    /// <summary>
    /// Unsubscribes from alerts for a specific job.
    /// </summary>
    public async Task UnsubscribeFromJobAlerts(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new HubException("Job ID is required.");

        var groupName = GetJobGroup(jobId);
        await LeaveGroupAsync(groupName);
        _logger.LogInformation("User {UserId} unsubscribed from job {Job} alerts", CurrentUserId, jobId);
    }

    /// <summary>
    /// Subscribes to alerts for a specific server.
    /// </summary>
    public async Task SubscribeToServerAlerts(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            throw new HubException("Server name is required.");

        var groupName = GetServerGroup(serverName);
        await JoinGroupAsync(groupName);
        _logger.LogInformation("User {UserId} subscribed to server {Server} alerts", CurrentUserId, serverName);
    }

    /// <summary>
    /// Unsubscribes from alerts for a specific server.
    /// </summary>
    public async Task UnsubscribeFromServerAlerts(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            throw new HubException("Server name is required.");

        var groupName = GetServerGroup(serverName);
        await LeaveGroupAsync(groupName);
        _logger.LogInformation("User {UserId} unsubscribed from server {Server} alerts", CurrentUserId, serverName);
    }

    /// <summary>
    /// Subscribes to alerts of a specific type.
    /// </summary>
    public async Task SubscribeToAlertType(AlertType alertType)
    {
        var groupName = GetTypeGroup(alertType);
        await JoinGroupAsync(groupName);
        _logger.LogInformation("User {UserId} subscribed to {AlertType} alerts", CurrentUserId, alertType);
    }

    /// <summary>
    /// Unsubscribes from alerts of a specific type.
    /// </summary>
    public async Task UnsubscribeFromAlertType(AlertType alertType)
    {
        var groupName = GetTypeGroup(alertType);
        await LeaveGroupAsync(groupName);
        _logger.LogInformation("User {UserId} unsubscribed from {AlertType} alerts", CurrentUserId, alertType);
    }

    /// <summary>
    /// Subscribes to alert summary updates.
    /// </summary>
    public async Task SubscribeToSummary()
    {
        await JoinGroupAsync("alerts:summary");
        _logger.LogInformation("User {UserId} subscribed to alert summary", CurrentUserId);
    }

    /// <summary>
    /// Unsubscribes from alert summary updates.
    /// </summary>
    public async Task UnsubscribeFromSummary()
    {
        await LeaveGroupAsync("alerts:summary");
        _logger.LogInformation("User {UserId} unsubscribed from alert summary", CurrentUserId);
    }

    #endregion

    #region Group Name Helpers

    public static string GetJobGroup(string jobId) => $"alerts:job:{jobId.ToLowerInvariant()}";
    public static string GetServerGroup(string serverName) => $"alerts:server:{serverName.ToLowerInvariant()}";
    public static string GetTypeGroup(AlertType alertType) => $"alerts:type:{alertType}";
    public static string GetSeverityGroup(AlertSeverity severity) => $"alerts:severity:{severity}";

    #endregion
}

/// <summary>
/// Service for broadcasting alert events to connected clients.
/// </summary>
public interface IAlertBroadcaster
{
    Task BroadcastAlertTriggeredAsync(AlertNotification alert, CancellationToken cancellationToken = default);
    Task BroadcastAlertAcknowledgedAsync(AlertActionNotification action, CancellationToken cancellationToken = default);
    Task BroadcastAlertResolvedAsync(AlertActionNotification action, CancellationToken cancellationToken = default);
    Task BroadcastAlertSummaryAsync(AlertSummaryNotification summary, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of alert broadcaster using SignalR.
/// </summary>
public class AlertBroadcaster : IAlertBroadcaster
{
    private readonly IHubContext<AlertHub, IAlertHubClient> _hubContext;
    private readonly ILogger<AlertBroadcaster> _logger;

    public AlertBroadcaster(IHubContext<AlertHub, IAlertHubClient> hubContext, ILogger<AlertBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastAlertTriggeredAsync(AlertNotification alert, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            // Broadcast to all alerts group
            _hubContext.Clients.Group("alerts:all").AlertTriggered(alert),
            
            // Broadcast to alert type group
            _hubContext.Clients.Group(AlertHub.GetTypeGroup(alert.AlertType)).AlertTriggered(alert)
        };

        // Broadcast to severity-based groups
        switch (alert.Severity)
        {
            case AlertSeverity.Critical:
                tasks.Add(_hubContext.Clients.Group("alerts:critical").AlertTriggered(alert));
                tasks.Add(_hubContext.Clients.Group("alerts:high").AlertTriggered(alert));
                break;
            case AlertSeverity.High:
                tasks.Add(_hubContext.Clients.Group("alerts:high").AlertTriggered(alert));
                break;
        }

        // Broadcast to job-specific group if applicable
        if (!string.IsNullOrEmpty(alert.JobId))
        {
            tasks.Add(_hubContext.Clients.Group(AlertHub.GetJobGroup(alert.JobId)).AlertTriggered(alert));
        }

        // Broadcast to server-specific group if applicable
        if (!string.IsNullOrEmpty(alert.ServerName))
        {
            tasks.Add(_hubContext.Clients.Group(AlertHub.GetServerGroup(alert.ServerName)).AlertTriggered(alert));
        }

        try
        {
            await Task.WhenAll(tasks);
            _logger.LogDebug("Broadcasted alert triggered: {AlertId}, Severity: {Severity}", 
                alert.InstanceId, alert.Severity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting alert triggered notification");
        }
    }

    public async Task BroadcastAlertAcknowledgedAsync(AlertActionNotification action, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group("alerts:all").AlertAcknowledged(action);
            _logger.LogDebug("Broadcasted alert acknowledged: {InstanceId}", action.InstanceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting alert acknowledged notification");
        }
    }

    public async Task BroadcastAlertResolvedAsync(AlertActionNotification action, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group("alerts:all").AlertResolved(action);
            _logger.LogDebug("Broadcasted alert resolved: {InstanceId}", action.InstanceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting alert resolved notification");
        }
    }

    public async Task BroadcastAlertSummaryAsync(AlertSummaryNotification summary, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group("alerts:summary").AlertSummaryUpdated(summary);
            _logger.LogDebug("Broadcasted alert summary update");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting alert summary notification");
        }
    }
}
