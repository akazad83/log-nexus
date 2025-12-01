using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace FMSLogNexus.Api.Hubs;

/// <summary>
/// Base hub providing common functionality for all SignalR hubs.
/// </summary>
[Authorize]
public abstract class BaseHub<T> : Hub<T> where T : class
{
    protected readonly ILogger _logger;
    protected readonly IConnectionManager _connectionManager;

    protected BaseHub(ILogger logger, IConnectionManager connectionManager)
    {
        _logger = logger;
        _connectionManager = connectionManager;
    }

    /// <summary>
    /// Gets the current user's ID from claims.
    /// </summary>
    protected Guid? CurrentUserId
    {
        get
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirst("sub");
            return claim != null && Guid.TryParse(claim.Value, out var id) ? id : null;
        }
    }

    /// <summary>
    /// Gets the current user's username from claims.
    /// </summary>
    protected string? CurrentUsername => Context.User?.FindFirst(ClaimTypes.Name)?.Value 
        ?? Context.User?.FindFirst("unique_name")?.Value;

    /// <summary>
    /// Gets the current user's role from claims.
    /// </summary>
    protected string? CurrentRole => Context.User?.FindFirst(ClaimTypes.Role)?.Value;

    /// <summary>
    /// Gets the client IP address.
    /// </summary>
    protected string? ClientIpAddress
    {
        get
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null) return null;

            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',').First().Trim();
            }

            return httpContext.Connection.RemoteIpAddress?.ToString();
        }
    }

    /// <summary>
    /// Called when a client connects.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = CurrentUserId?.ToString() ?? "anonymous";
        var username = CurrentUsername ?? "unknown";

        _logger.LogInformation(
            "Client connected: ConnectionId={ConnectionId}, UserId={UserId}, Username={Username}, IP={IP}",
            Context.ConnectionId, userId, username, ClientIpAddress);

        if (CurrentUserId.HasValue)
        {
            _connectionManager.AddConnection(CurrentUserId.Value, Context.ConnectionId, GetHubName());
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = CurrentUserId?.ToString() ?? "anonymous";

        if (exception != null)
        {
            _logger.LogWarning(exception,
                "Client disconnected with error: ConnectionId={ConnectionId}, UserId={UserId}",
                Context.ConnectionId, userId);
        }
        else
        {
            _logger.LogInformation(
                "Client disconnected: ConnectionId={ConnectionId}, UserId={UserId}",
                Context.ConnectionId, userId);
        }

        if (CurrentUserId.HasValue)
        {
            _connectionManager.RemoveConnection(CurrentUserId.Value, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Gets the hub name for connection tracking.
    /// </summary>
    protected abstract string GetHubName();

    /// <summary>
    /// Adds the current connection to a group.
    /// </summary>
    protected async Task JoinGroupAsync(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Connection {ConnectionId} joined group {Group}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Removes the current connection from a group.
    /// </summary>
    protected async Task LeaveGroupAsync(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Connection {ConnectionId} left group {Group}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Sends to all clients in a group except the caller.
    /// </summary>
    protected T OthersInGroup(string groupName)
    {
        return Clients.OthersInGroup(groupName);
    }
}

/// <summary>
/// Manages SignalR connections.
/// </summary>
public interface IConnectionManager
{
    void AddConnection(Guid userId, string connectionId, string hubName);
    void RemoveConnection(Guid userId, string connectionId);
    IEnumerable<string> GetConnections(Guid userId);
    IEnumerable<string> GetConnectionsByHub(Guid userId, string hubName);
    int GetConnectionCount();
    int GetUserCount();
    bool IsUserConnected(Guid userId);
    IEnumerable<Guid> GetConnectedUsers();
}

/// <summary>
/// In-memory connection manager implementation.
/// </summary>
public class ConnectionManager : IConnectionManager
{
    private readonly object _lock = new();
    private readonly Dictionary<Guid, HashSet<ConnectionInfo>> _connections = new();

    public void AddConnection(Guid userId, string connectionId, string hubName)
    {
        lock (_lock)
        {
            if (!_connections.TryGetValue(userId, out var userConnections))
            {
                userConnections = new HashSet<ConnectionInfo>();
                _connections[userId] = userConnections;
            }

            userConnections.Add(new ConnectionInfo(connectionId, hubName));
        }
    }

    public void RemoveConnection(Guid userId, string connectionId)
    {
        lock (_lock)
        {
            if (_connections.TryGetValue(userId, out var userConnections))
            {
                userConnections.RemoveWhere(c => c.ConnectionId == connectionId);

                if (userConnections.Count == 0)
                {
                    _connections.Remove(userId);
                }
            }
        }
    }

    public IEnumerable<string> GetConnections(Guid userId)
    {
        lock (_lock)
        {
            if (_connections.TryGetValue(userId, out var userConnections))
            {
                return userConnections.Select(c => c.ConnectionId).ToList();
            }

            return Enumerable.Empty<string>();
        }
    }

    public IEnumerable<string> GetConnectionsByHub(Guid userId, string hubName)
    {
        lock (_lock)
        {
            if (_connections.TryGetValue(userId, out var userConnections))
            {
                return userConnections
                    .Where(c => c.HubName == hubName)
                    .Select(c => c.ConnectionId)
                    .ToList();
            }

            return Enumerable.Empty<string>();
        }
    }

    public int GetConnectionCount()
    {
        lock (_lock)
        {
            return _connections.Values.Sum(c => c.Count);
        }
    }

    public int GetUserCount()
    {
        lock (_lock)
        {
            return _connections.Count;
        }
    }

    public bool IsUserConnected(Guid userId)
    {
        lock (_lock)
        {
            return _connections.ContainsKey(userId) && _connections[userId].Count > 0;
        }
    }

    public IEnumerable<Guid> GetConnectedUsers()
    {
        lock (_lock)
        {
            return _connections.Keys.ToList();
        }
    }

    private record ConnectionInfo(string ConnectionId, string HubName);
}
