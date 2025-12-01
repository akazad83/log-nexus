using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Interfaces.Services;

/// <summary>
/// Service interface for real-time notifications via SignalR.
/// </summary>
public interface IRealTimeService
{
    #region Dashboard Updates

    /// <summary>
    /// Broadcasts dashboard statistics update.
    /// </summary>
    /// <param name="stats">Updated statistics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastDashboardStatsAsync(
        DashboardSummaryResponse stats,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts log statistics update.
    /// </summary>
    /// <param name="stats">Updated log statistics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastLogStatsAsync(
        DashboardLogStats stats,
        CancellationToken cancellationToken = default);

    #endregion

    #region Log Events

    /// <summary>
    /// Broadcasts a new log entry (for live log streaming).
    /// </summary>
    /// <param name="log">New log entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastNewLogAsync(
        LogEntryResponse log,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts a new error log.
    /// </summary>
    /// <param name="error">Error log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastNewErrorAsync(
        RecentErrorResponse error,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends logs to subscribers of a specific job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="log">New log entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendJobLogAsync(
        string jobId,
        LogEntryResponse log,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends logs to subscribers of a specific execution.
    /// </summary>
    /// <param name="executionId">Execution ID.</param>
    /// <param name="log">New log entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendExecutionLogAsync(
        Guid executionId,
        LogEntryResponse log,
        CancellationToken cancellationToken = default);

    #endregion

    #region Job Events

    /// <summary>
    /// Broadcasts job execution started.
    /// </summary>
    /// <param name="execution">Execution info.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastExecutionStartedAsync(
        ExecutionListItem execution,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts job execution completed.
    /// </summary>
    /// <param name="execution">Execution info.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastExecutionCompletedAsync(
        ExecutionListItem execution,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts job execution progress update.
    /// </summary>
    /// <param name="executionId">Execution ID.</param>
    /// <param name="progress">Progress info.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastExecutionProgressAsync(
        Guid executionId,
        ExecutionProgress progress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts running jobs update.
    /// </summary>
    /// <param name="runningJobs">Currently running jobs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastRunningJobsAsync(
        IReadOnlyList<RunningJobInfo> runningJobs,
        CancellationToken cancellationToken = default);

    #endregion

    #region Alert Events

    /// <summary>
    /// Broadcasts a new alert instance.
    /// </summary>
    /// <param name="alert">Alert instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastNewAlertAsync(
        AlertInstanceListItem alert,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts alert acknowledged.
    /// </summary>
    /// <param name="instanceId">Alert instance ID.</param>
    /// <param name="acknowledgedBy">User who acknowledged.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastAlertAcknowledgedAsync(
        Guid instanceId,
        string acknowledgedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts alert resolved.
    /// </summary>
    /// <param name="instanceId">Alert instance ID.</param>
    /// <param name="resolvedBy">User who resolved.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastAlertResolvedAsync(
        Guid instanceId,
        string resolvedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts alert statistics update.
    /// </summary>
    /// <param name="stats">Updated alert statistics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastAlertStatsAsync(
        DashboardAlertStats stats,
        CancellationToken cancellationToken = default);

    #endregion

    #region Server Events

    /// <summary>
    /// Broadcasts server status change.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="oldStatus">Previous status.</param>
    /// <param name="newStatus">New status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastServerStatusChangeAsync(
        string serverName,
        ServerStatus oldStatus,
        ServerStatus newStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts server heartbeat received.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastServerHeartbeatAsync(
        string serverName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts server statistics update.
    /// </summary>
    /// <param name="stats">Updated server statistics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastServerStatsAsync(
        DashboardServerStats stats,
        CancellationToken cancellationToken = default);

    #endregion

    #region Notifications

    /// <summary>
    /// Sends a notification to a specific user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="notification">Notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendUserNotificationAsync(
        Guid userId,
        DashboardNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts a notification to all connected users.
    /// </summary>
    /// <param name="notification">Notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastNotificationAsync(
        DashboardNotification notification,
        CancellationToken cancellationToken = default);

    #endregion

    #region Connection Management

    /// <summary>
    /// Gets the number of connected clients.
    /// </summary>
    /// <returns>Connected client count.</returns>
    int GetConnectedClientCount();

    /// <summary>
    /// Checks if a user is connected.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>True if connected.</returns>
    bool IsUserConnected(Guid userId);

    #endregion
}

/// <summary>
/// Client-side SignalR hub methods interface.
/// Used for strongly-typed hub clients.
/// </summary>
public interface ILogNexusHubClient
{
    // Dashboard
    Task ReceiveDashboardStats(DashboardSummaryResponse stats);
    Task ReceiveLogStats(DashboardLogStats stats);
    Task ReceiveJobStats(DashboardJobStats stats);
    Task ReceiveServerStats(DashboardServerStats stats);
    Task ReceiveAlertStats(DashboardAlertStats stats);

    // Logs
    Task ReceiveNewLog(LogEntryResponse log);
    Task ReceiveNewError(RecentErrorResponse error);

    // Jobs
    Task ReceiveExecutionStarted(ExecutionListItem execution);
    Task ReceiveExecutionCompleted(ExecutionListItem execution);
    Task ReceiveExecutionProgress(Guid executionId, ExecutionProgress progress);
    Task ReceiveRunningJobs(IReadOnlyList<RunningJobInfo> runningJobs);

    // Alerts
    Task ReceiveNewAlert(AlertInstanceListItem alert);
    Task ReceiveAlertAcknowledged(Guid instanceId, string acknowledgedBy);
    Task ReceiveAlertResolved(Guid instanceId, string resolvedBy);

    // Servers
    Task ReceiveServerStatusChange(string serverName, ServerStatus oldStatus, ServerStatus newStatus);
    Task ReceiveServerHeartbeat(string serverName);

    // Notifications
    Task ReceiveNotification(DashboardNotification notification);
}

/// <summary>
/// Execution progress information for real-time updates.
/// </summary>
public class ExecutionProgress
{
    public Guid ExecutionId { get; set; }
    public string JobId { get; set; } = string.Empty;
    public long RunningDurationMs { get; set; }
    public int LogCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public string? CurrentStep { get; set; }
    public int? PercentComplete { get; set; }
}
