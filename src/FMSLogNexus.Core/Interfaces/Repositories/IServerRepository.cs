using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for server/machine operations.
/// </summary>
public interface IServerRepository : IRepository<Server, string>
{
    #region Query Operations

    /// <summary>
    /// Gets all active servers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active servers.</returns>
    Task<IReadOnlyList<Server>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets servers by status.
    /// </summary>
    /// <param name="status">Server status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Servers with the status.</returns>
    Task<IReadOnlyList<Server>> GetByStatusAsync(
        ServerStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a server with its jobs.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server with jobs.</returns>
    Task<Server?> GetWithJobsAsync(
        string serverName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a server with running executions.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server with running executions.</returns>
    Task<Server?> GetWithRunningExecutionsAsync(
        string serverName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets servers that haven't sent heartbeat recently.
    /// </summary>
    /// <param name="timeoutSeconds">Seconds since last heartbeat.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unresponsive servers.</returns>
    Task<IReadOnlyList<Server>> GetUnresponsiveAsync(
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recently active servers (had heartbeat recently).
    /// </summary>
    /// <param name="withinSeconds">Seconds threshold.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recently active servers.</returns>
    Task<IReadOnlyList<Server>> GetRecentlyActiveAsync(
        int withinSeconds = 300,
        CancellationToken cancellationToken = default);

    #endregion

    #region Status Operations

    /// <summary>
    /// Gets server status summary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Status counts.</returns>
    Task<ServerStatusSummary> GetStatusSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets server statuses for all servers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server statuses.</returns>
    Task<IReadOnlyList<ServerStatusInfo>> GetAllStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates current status for a server based on heartbeat.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Calculated status.</returns>
    Task<ServerStatus> CalculateStatusAsync(
        string serverName,
        CancellationToken cancellationToken = default);

    #endregion

    #region Heartbeat Operations

    /// <summary>
    /// Updates server heartbeat (creates server if not exists).
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="ipAddress">Client IP address.</param>
    /// <param name="agentVersion">Agent version.</param>
    /// <param name="agentType">Agent type.</param>
    /// <param name="metadata">Server metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated server.</returns>
    Task<Server> UpdateHeartbeatAsync(
        string serverName,
        string? ipAddress,
        string? agentVersion,
        string? agentType,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks servers as offline that haven't sent heartbeat.
    /// </summary>
    /// <param name="timeoutSeconds">Seconds threshold.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of servers marked offline.</returns>
    Task<int> MarkOfflineAsync(
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default);

    #endregion

    #region Lookup Operations

    /// <summary>
    /// Gets server lookup items for dropdowns.
    /// </summary>
    /// <param name="activeOnly">Only active servers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Lookup items.</returns>
    Task<IReadOnlyList<LookupItem>> GetLookupItemsAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a server exists.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists.</returns>
    Task<bool> ExistsAsync(string serverName, CancellationToken cancellationToken = default);

    #endregion

    #region Registration

    /// <summary>
    /// Registers or updates a server (upsert).
    /// </summary>
    /// <param name="server">Server to register.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if created, false if updated.</returns>
    Task<bool> RegisterOrUpdateAsync(Server server, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a server.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if found and activated.</returns>
    Task<bool> ActivateAsync(string serverName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a server.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if found and deactivated.</returns>
    Task<bool> DeactivateAsync(string serverName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets server to maintenance mode.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if found and set.</returns>
    Task<bool> SetMaintenanceModeAsync(string serverName, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Server status summary.
/// </summary>
public class ServerStatusSummary
{
    public int Total { get; set; }
    public int Online { get; set; }
    public int Offline { get; set; }
    public int Degraded { get; set; }
    public int Maintenance { get; set; }
    public int Unknown { get; set; }
}

/// <summary>
/// Server status information.
/// </summary>
public class ServerStatusInfo
{
    public string ServerName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ServerStatus Status { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public int? SecondsSinceHeartbeat { get; set; }
    public string? AgentVersion { get; set; }
    public int ActiveJobCount { get; set; }
    public int RunningJobCount { get; set; }
}
