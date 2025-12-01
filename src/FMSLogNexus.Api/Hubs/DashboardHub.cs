using Microsoft.AspNetCore.SignalR;

namespace FMSLogNexus.Api.Hubs;

/// <summary>
/// Hub for real-time dashboard updates and system notifications.
/// </summary>
public class DashboardHub : BaseHub<IDashboardHubClient>
{
    private const string HubName = "DashboardHub";
    private const string DashboardGroup = "dashboard:main";
    private const string SystemNotificationsGroup = "dashboard:notifications";

    public DashboardHub(
        ILogger<DashboardHub> logger,
        IConnectionManager connectionManager)
        : base(logger, connectionManager)
    {
    }

    protected override string GetHubName() => HubName;

    #region Subscription Methods

    /// <summary>
    /// Subscribes to dashboard updates.
    /// </summary>
    public async Task SubscribeToDashboard()
    {
        await JoinGroupAsync(DashboardGroup);
        _logger.LogInformation("User {UserId} subscribed to dashboard", CurrentUserId);
    }

    /// <summary>
    /// Unsubscribes from dashboard updates.
    /// </summary>
    public async Task UnsubscribeFromDashboard()
    {
        await LeaveGroupAsync(DashboardGroup);
        _logger.LogInformation("User {UserId} unsubscribed from dashboard", CurrentUserId);
    }

    /// <summary>
    /// Subscribes to system notifications.
    /// </summary>
    public async Task SubscribeToNotifications()
    {
        await JoinGroupAsync(SystemNotificationsGroup);
        _logger.LogInformation("User {UserId} subscribed to system notifications", CurrentUserId);
    }

    /// <summary>
    /// Unsubscribes from system notifications.
    /// </summary>
    public async Task UnsubscribeFromNotifications()
    {
        await LeaveGroupAsync(SystemNotificationsGroup);
        _logger.LogInformation("User {UserId} unsubscribed from system notifications", CurrentUserId);
    }

    /// <summary>
    /// Gets connection statistics.
    /// </summary>
    public Task<ConnectionStats> GetConnectionStats()
    {
        var stats = new ConnectionStats
        {
            TotalConnections = _connectionManager.GetConnectionCount(),
            UniqueUsers = _connectionManager.GetUserCount(),
            ConnectedUsers = _connectionManager.GetConnectedUsers().ToList()
        };

        return Task.FromResult(stats);
    }

    #endregion
}

/// <summary>
/// Connection statistics.
/// </summary>
public class ConnectionStats
{
    public int TotalConnections { get; set; }
    public int UniqueUsers { get; set; }
    public List<Guid> ConnectedUsers { get; set; } = new();
}

/// <summary>
/// Service for broadcasting dashboard events to connected clients.
/// </summary>
public interface IDashboardBroadcaster
{
    Task BroadcastDashboardUpdateAsync(DashboardNotification dashboard, CancellationToken cancellationToken = default);
    Task BroadcastSystemNotificationAsync(SystemNotificationMessage notification, CancellationToken cancellationToken = default);
    Task BroadcastToUserAsync(Guid userId, SystemNotificationMessage notification, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of dashboard broadcaster using SignalR.
/// </summary>
public class DashboardBroadcaster : IDashboardBroadcaster
{
    private readonly IHubContext<DashboardHub, IDashboardHubClient> _hubContext;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<DashboardBroadcaster> _logger;

    public DashboardBroadcaster(
        IHubContext<DashboardHub, IDashboardHubClient> hubContext,
        IConnectionManager connectionManager,
        ILogger<DashboardBroadcaster> logger)
    {
        _hubContext = hubContext;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public async Task BroadcastDashboardUpdateAsync(DashboardNotification dashboard, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group("dashboard:main").DashboardUpdated(dashboard);
            _logger.LogDebug("Broadcasted dashboard update");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting dashboard update");
        }
    }

    public async Task BroadcastSystemNotificationAsync(SystemNotificationMessage notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group("dashboard:notifications").SystemNotification(notification);
            _logger.LogDebug("Broadcasted system notification: {Type} - {Title}", notification.Type, notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting system notification");
        }
    }

    public async Task BroadcastToUserAsync(Guid userId, SystemNotificationMessage notification, CancellationToken cancellationToken = default)
    {
        var connections = _connectionManager.GetConnections(userId).ToList();
        if (connections.Count == 0)
        {
            _logger.LogDebug("User {UserId} has no active connections", userId);
            return;
        }

        try
        {
            await _hubContext.Clients.Clients(connections).SystemNotification(notification);
            _logger.LogDebug("Sent notification to user {UserId}: {Type} - {Title}", userId, notification.Type, notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
        }
    }
}
