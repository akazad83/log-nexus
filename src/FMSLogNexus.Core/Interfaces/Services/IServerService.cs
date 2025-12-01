using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Interfaces.Services;

/// <summary>
/// Service interface for server/machine operations.
/// Handles server registration, heartbeats, and status monitoring.
/// </summary>
public interface IServerService
{
    #region Server Management

    /// <summary>
    /// Creates or registers a new server.
    /// </summary>
    /// <param name="request">Server creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server status response.</returns>
    Task<ServerStatusResponse> CreateServerAsync(
        CreateServerRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a server by name.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server status response or null.</returns>
    Task<ServerStatusResponse?> GetServerAsync(
        string serverName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a server.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="request">Update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated server response.</returns>
    Task<ServerStatusResponse?> UpdateServerAsync(
        string serverName,
        UpdateServerRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a server (soft delete).
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted.</returns>
    Task<bool> DeleteServerAsync(
        string serverName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all servers with their status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server status list.</returns>
    Task<IReadOnlyList<ServerStatusResponse>> GetAllServersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets servers by status.
    /// </summary>
    /// <param name="status">Server status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Filtered server list.</returns>
    Task<IReadOnlyList<ServerStatusResponse>> GetServersByStatusAsync(
        ServerStatus status,
        CancellationToken cancellationToken = default);

    #endregion

    #region Heartbeat

    /// <summary>
    /// Processes a server heartbeat.
    /// Creates the server if it doesn't exist.
    /// </summary>
    /// <param name="request">Heartbeat request.</param>
    /// <param name="clientIp">Client IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server status response.</returns>
    Task<ServerStatusResponse> ProcessHeartbeatAsync(
        ServerHeartbeatRequest request,
        string? clientIp,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks for and marks offline servers.
    /// Should be called periodically by a background service.
    /// </summary>
    /// <param name="timeoutSeconds">Seconds since last heartbeat.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of servers marked offline.</returns>
    Task<int> CheckAndMarkOfflineServersAsync(
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default);

    #endregion

    #region Status and Health

    /// <summary>
    /// Gets server status summary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Status summary counts.</returns>
    Task<DashboardServerStats> GetStatusSummaryAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unresponsive servers.
    /// </summary>
    /// <param name="timeoutSeconds">Seconds threshold.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unresponsive servers.</returns>
    Task<IReadOnlyList<ServerStatusResponse>> GetUnresponsiveServersAsync(
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default);

    #endregion

    #region Server Actions

    /// <summary>
    /// Activates a server.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if activated.</returns>
    Task<bool> ActivateServerAsync(
        string serverName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a server.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deactivated.</returns>
    Task<bool> DeactivateServerAsync(
        string serverName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a server to maintenance mode.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if set.</returns>
    Task<bool> SetMaintenanceModeAsync(
        string serverName,
        CancellationToken cancellationToken = default);

    #endregion

    #region Lookups

    /// <summary>
    /// Gets server lookup items for dropdowns.
    /// </summary>
    /// <param name="activeOnly">Only active servers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Lookup items.</returns>
    Task<IReadOnlyList<LookupItem>> GetServerLookupAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a server exists.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists.</returns>
    Task<bool> ServerExistsAsync(
        string serverName,
        CancellationToken cancellationToken = default);

    #endregion
}
