using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Interfaces.Services;

/// <summary>
/// Service interface for dashboard operations.
/// Provides aggregated data for the UI dashboard.
/// </summary>
public interface IDashboardService
{
    #region Summary Data

    /// <summary>
    /// Gets the main dashboard summary.
    /// </summary>
    /// <param name="request">Dashboard request with filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard summary response.</returns>
    Task<DashboardSummaryResponse> GetSummaryAsync(
        DashboardRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets log statistics for the dashboard.
    /// </summary>
    /// <param name="period">Time period.</param>
    /// <param name="serverName">Optional server filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Log statistics.</returns>
    Task<DashboardLogStats> GetLogStatsAsync(
        TimePeriod period,
        string? serverName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets job statistics for the dashboard.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job statistics.</returns>
    Task<DashboardJobStats> GetJobStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets server statistics for the dashboard.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server statistics.</returns>
    Task<DashboardServerStats> GetServerStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alert statistics for the dashboard.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Alert statistics.</returns>
    Task<DashboardAlertStats> GetAlertStatsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Trend Data

    /// <summary>
    /// Gets log volume trend data.
    /// </summary>
    /// <param name="period">Time period.</param>
    /// <param name="interval">Aggregation interval.</param>
    /// <param name="serverName">Optional server filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Trend data response.</returns>
    Task<DashboardTrendsResponse> GetLogTrendsAsync(
        TimePeriod period,
        TrendInterval interval,
        string? serverName = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Lists

    /// <summary>
    /// Gets recent error logs for the dashboard.
    /// </summary>
    /// <param name="count">Number of errors.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent errors.</returns>
    Task<IReadOnlyList<RecentErrorResponse>> GetRecentErrorsAsync(
        int count = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets currently running jobs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Running jobs.</returns>
    Task<IReadOnlyList<RunningJobInfo>> GetRunningJobsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent job failures.
    /// </summary>
    /// <param name="count">Number of failures.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent failures.</returns>
    Task<IReadOnlyList<RecentFailure>> GetRecentFailuresAsync(
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active (unresolved) alerts.
    /// </summary>
    /// <param name="count">Number of alerts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active alerts.</returns>
    Task<IReadOnlyList<AlertInstanceListItem>> GetActiveAlertsAsync(
        int count = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets server status list.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server statuses.</returns>
    Task<IReadOnlyList<ServerStatusResponse>> GetServerStatusesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unhealthy jobs for attention.
    /// </summary>
    /// <param name="count">Number of jobs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unhealthy jobs.</returns>
    Task<IReadOnlyList<JobListItem>> GetUnhealthyJobsAsync(
        int count = 10,
        CancellationToken cancellationToken = default);

    #endregion

    #region Health

    /// <summary>
    /// Gets overall system health.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>System health response.</returns>
    Task<SystemHealthResponse> GetSystemHealthAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Real-time Updates

    /// <summary>
    /// Gets dashboard data that has changed since a timestamp.
    /// Used for incremental updates via SignalR.
    /// </summary>
    /// <param name="since">Timestamp to check changes from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Changed dashboard data.</returns>
    Task<DashboardUpdateResponse> GetUpdatesAsync(
        DateTime since,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Response for incremental dashboard updates.
/// </summary>
public class DashboardUpdateResponse
{
    /// <summary>
    /// Timestamp of this update.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether log stats have changed.
    /// </summary>
    public bool LogStatsChanged { get; set; }

    /// <summary>
    /// Updated log stats (if changed).
    /// </summary>
    public DashboardLogStats? LogStats { get; set; }

    /// <summary>
    /// Whether job stats have changed.
    /// </summary>
    public bool JobStatsChanged { get; set; }

    /// <summary>
    /// Updated job stats (if changed).
    /// </summary>
    public DashboardJobStats? JobStats { get; set; }

    /// <summary>
    /// Whether alert stats have changed.
    /// </summary>
    public bool AlertStatsChanged { get; set; }

    /// <summary>
    /// Updated alert stats (if changed).
    /// </summary>
    public DashboardAlertStats? AlertStats { get; set; }

    /// <summary>
    /// New errors since last update.
    /// </summary>
    public IReadOnlyList<RecentErrorResponse>? NewErrors { get; set; }

    /// <summary>
    /// New alert instances since last update.
    /// </summary>
    public IReadOnlyList<AlertInstanceListItem>? NewAlerts { get; set; }

    /// <summary>
    /// Job execution status changes.
    /// </summary>
    public IReadOnlyList<ExecutionStatusChange>? ExecutionChanges { get; set; }

    /// <summary>
    /// Server status changes.
    /// </summary>
    public IReadOnlyList<ServerStatusChange>? ServerChanges { get; set; }
}

/// <summary>
/// Execution status change for real-time updates.
/// </summary>
public class ExecutionStatusChange
{
    public Guid ExecutionId { get; set; }
    public string JobId { get; set; } = string.Empty;
    public JobStatus OldStatus { get; set; }
    public JobStatus NewStatus { get; set; }
    public DateTime ChangedAt { get; set; }
}

/// <summary>
/// Server status change for real-time updates.
/// </summary>
public class ServerStatusChange
{
    public string ServerName { get; set; } = string.Empty;
    public ServerStatus OldStatus { get; set; }
    public ServerStatus NewStatus { get; set; }
    public DateTime ChangedAt { get; set; }
}
