using Microsoft.AspNetCore.SignalR;

namespace FMSLogNexus.Api.Hubs;

/// <summary>
/// Hub for real-time server status updates.
/// </summary>
public class ServerHub : BaseHub<IServerHubClient>
{
    private const string HubName = "ServerHub";
    private const string AllServersGroup = "servers:all";
    private const string HeartbeatsGroup = "servers:heartbeats";
    private const string MetricsGroup = "servers:metrics";

    public ServerHub(
        ILogger<ServerHub> logger,
        IConnectionManager connectionManager)
        : base(logger, connectionManager)
    {
    }

    protected override string GetHubName() => HubName;

    #region Subscription Methods

    /// <summary>
    /// Subscribes to all server updates.
    /// </summary>
    public async Task SubscribeToAllServers()
    {
        await JoinGroupAsync(AllServersGroup);
        _logger.LogInformation("User {UserId} subscribed to all servers", CurrentUserId);
    }

    /// <summary>
    /// Unsubscribes from all server updates.
    /// </summary>
    public async Task UnsubscribeFromAllServers()
    {
        await LeaveGroupAsync(AllServersGroup);
        _logger.LogInformation("User {UserId} unsubscribed from all servers", CurrentUserId);
    }

    /// <summary>
    /// Subscribes to updates for a specific server.
    /// </summary>
    public async Task SubscribeToServer(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            throw new HubException("Server name is required.");

        var groupName = GetServerGroup(serverName);
        await JoinGroupAsync(groupName);
        _logger.LogInformation("User {UserId} subscribed to server {Server}", CurrentUserId, serverName);
    }

    /// <summary>
    /// Unsubscribes from updates for a specific server.
    /// </summary>
    public async Task UnsubscribeFromServer(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            throw new HubException("Server name is required.");

        var groupName = GetServerGroup(serverName);
        await LeaveGroupAsync(groupName);
        _logger.LogInformation("User {UserId} unsubscribed from server {Server}", CurrentUserId, serverName);
    }

    /// <summary>
    /// Subscribes to heartbeat notifications.
    /// </summary>
    public async Task SubscribeToHeartbeats()
    {
        await JoinGroupAsync(HeartbeatsGroup);
        _logger.LogInformation("User {UserId} subscribed to heartbeats", CurrentUserId);
    }

    /// <summary>
    /// Unsubscribes from heartbeat notifications.
    /// </summary>
    public async Task UnsubscribeFromHeartbeats()
    {
        await LeaveGroupAsync(HeartbeatsGroup);
        _logger.LogInformation("User {UserId} unsubscribed from heartbeats", CurrentUserId);
    }

    /// <summary>
    /// Subscribes to server metrics updates.
    /// </summary>
    public async Task SubscribeToMetrics()
    {
        await JoinGroupAsync(MetricsGroup);
        _logger.LogInformation("User {UserId} subscribed to server metrics", CurrentUserId);
    }

    /// <summary>
    /// Unsubscribes from server metrics updates.
    /// </summary>
    public async Task UnsubscribeFromMetrics()
    {
        await LeaveGroupAsync(MetricsGroup);
        _logger.LogInformation("User {UserId} unsubscribed from server metrics", CurrentUserId);
    }

    /// <summary>
    /// Subscribes to offline server alerts.
    /// </summary>
    public async Task SubscribeToOfflineAlerts()
    {
        await JoinGroupAsync("servers:offline");
        _logger.LogInformation("User {UserId} subscribed to offline server alerts", CurrentUserId);
    }

    /// <summary>
    /// Unsubscribes from offline server alerts.
    /// </summary>
    public async Task UnsubscribeFromOfflineAlerts()
    {
        await LeaveGroupAsync("servers:offline");
        _logger.LogInformation("User {UserId} unsubscribed from offline server alerts", CurrentUserId);
    }

    #endregion

    #region Group Name Helpers

    public static string GetServerGroup(string serverName) => $"servers:server:{serverName.ToLowerInvariant()}";

    #endregion
}

/// <summary>
/// Service for broadcasting server events to connected clients.
/// </summary>
public interface IServerBroadcaster
{
    Task BroadcastServerStatusChangedAsync(ServerStatusNotification status, CancellationToken cancellationToken = default);
    Task BroadcastServerHeartbeatAsync(ServerHeartbeatNotification heartbeat, CancellationToken cancellationToken = default);
    Task BroadcastServerMetricsAsync(ServerMetricsNotification metrics, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of server broadcaster using SignalR.
/// </summary>
public class ServerBroadcaster : IServerBroadcaster
{
    private readonly IHubContext<ServerHub, IServerHubClient> _hubContext;
    private readonly ILogger<ServerBroadcaster> _logger;

    public ServerBroadcaster(IHubContext<ServerHub, IServerHubClient> hubContext, ILogger<ServerBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastServerStatusChangedAsync(ServerStatusNotification status, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            // Broadcast to all servers group
            _hubContext.Clients.Group("servers:all").ServerStatusChanged(status),
            
            // Broadcast to server-specific group
            _hubContext.Clients.Group(ServerHub.GetServerGroup(status.ServerName)).ServerStatusChanged(status)
        };

        // If server went offline, also broadcast to offline alerts group
        if (status.Status == Core.Enums.ServerStatus.Offline)
        {
            tasks.Add(_hubContext.Clients.Group("servers:offline").ServerStatusChanged(status));
        }

        try
        {
            await Task.WhenAll(tasks);
            _logger.LogDebug("Broadcasted server status changed: {Server}, Status: {Status}", 
                status.ServerName, status.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting server status notification");
        }
    }

    public async Task BroadcastServerHeartbeatAsync(ServerHeartbeatNotification heartbeat, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            _hubContext.Clients.Group("servers:heartbeats").ServerHeartbeat(heartbeat),
            _hubContext.Clients.Group(ServerHub.GetServerGroup(heartbeat.ServerName)).ServerHeartbeat(heartbeat)
        };

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting server heartbeat notification");
        }
    }

    public async Task BroadcastServerMetricsAsync(ServerMetricsNotification metrics, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            _hubContext.Clients.Group("servers:metrics").ServerMetrics(metrics),
            _hubContext.Clients.Group(ServerHub.GetServerGroup(metrics.ServerName)).ServerMetrics(metrics)
        };

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting server metrics notification");
        }
    }
}
