using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for alert definition operations.
/// </summary>
public interface IAlertRepository : IRepository<Alert>
{
    #region Query Operations

    /// <summary>
    /// Gets all active alerts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active alerts.</returns>
    Task<IReadOnlyList<Alert>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alerts by type.
    /// </summary>
    /// <param name="alertType">Alert type.</param>
    /// <param name="activeOnly">Only active alerts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching alerts.</returns>
    Task<IReadOnlyList<Alert>> GetByTypeAsync(
        AlertType alertType,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alerts for a specific job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="activeOnly">Only active alerts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job alerts.</returns>
    Task<IReadOnlyList<Alert>> GetByJobAsync(
        string jobId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alerts for a specific server.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="activeOnly">Only active alerts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server alerts.</returns>
    Task<IReadOnlyList<Alert>> GetByServerAsync(
        string serverName,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an alert with its recent instances.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="instanceCount">Number of recent instances.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Alert with instances.</returns>
    Task<Alert?> GetWithInstancesAsync(
        Guid alertId,
        int instanceCount = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alerts that can be triggered (not throttled).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Triggerable alerts.</returns>
    Task<IReadOnlyList<Alert>> GetTriggerableAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Trigger Operations

    /// <summary>
    /// Records that an alert was triggered.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordTriggerAsync(Guid alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an alert can be triggered (not in throttle period).
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if can trigger.</returns>
    Task<bool> CanTriggerAsync(Guid alertId, CancellationToken cancellationToken = default);

    #endregion

    #region Activation

    /// <summary>
    /// Activates an alert.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if found and activated.</returns>
    Task<bool> ActivateAsync(Guid alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates an alert.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if found and deactivated.</returns>
    Task<bool> DeactivateAsync(Guid alertId, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Repository interface for alert instance operations.
/// </summary>
public interface IAlertInstanceRepository : IRepository<AlertInstance>
{
    #region Query Operations

    /// <summary>
    /// Searches alert instances with filtering and pagination.
    /// </summary>
    /// <param name="request">Search request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated instances.</returns>
    Task<PaginatedResult<AlertInstance>> SearchAsync(
        DTOs.Requests.AlertInstanceSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active (unresolved) alert instances.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active instances.</returns>
    Task<IReadOnlyList<AlertInstance>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unacknowledged alert instances.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New instances.</returns>
    Task<IReadOnlyList<AlertInstance>> GetUnacknowledgedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets instances by alert definition.
    /// </summary>
    /// <param name="alertId">Alert definition ID.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated instances.</returns>
    Task<PaginatedResult<AlertInstance>> GetByAlertAsync(
        Guid alertId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets instances for a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="activeOnly">Only active instances.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job alert instances.</returns>
    Task<IReadOnlyList<AlertInstance>> GetByJobAsync(
        string jobId,
        bool activeOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets instances for a server.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="activeOnly">Only active instances.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server alert instances.</returns>
    Task<IReadOnlyList<AlertInstance>> GetByServerAsync(
        string serverName,
        bool activeOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an instance with its alert definition.
    /// </summary>
    /// <param name="instanceId">Instance ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance with alert.</returns>
    Task<AlertInstance?> GetWithAlertAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent instances by severity.
    /// </summary>
    /// <param name="severity">Minimum severity.</param>
    /// <param name="count">Number to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent instances.</returns>
    Task<IReadOnlyList<AlertInstance>> GetRecentBySeverityAsync(
        AlertSeverity severity,
        int count = 20,
        CancellationToken cancellationToken = default);

    #endregion

    #region Summary Operations

    /// <summary>
    /// Gets alert instance counts by status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Counts by status.</returns>
    Task<Dictionary<AlertInstanceStatus, int>> GetCountsByStatusAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alert instance counts by severity.
    /// </summary>
    /// <param name="unresolvedOnly">Only count unresolved.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Counts by severity.</returns>
    Task<Dictionary<AlertSeverity, int>> GetCountsBySeverityAsync(
        bool unresolvedOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alert summary for dashboard.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Alert summary.</returns>
    Task<AlertInstanceSummary> GetSummaryAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Lifecycle Operations

    /// <summary>
    /// Creates a new alert instance.
    /// </summary>
    /// <param name="instance">Instance to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created instance.</returns>
    Task<AlertInstance> CreateAsync(
        AlertInstance instance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledges an alert instance.
    /// </summary>
    /// <param name="instanceId">Instance ID.</param>
    /// <param name="username">User acknowledging.</param>
    /// <param name="note">Optional note.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if found and acknowledged.</returns>
    Task<bool> AcknowledgeAsync(
        Guid instanceId,
        string username,
        string? note = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an alert instance.
    /// </summary>
    /// <param name="instanceId">Instance ID.</param>
    /// <param name="username">User resolving.</param>
    /// <param name="note">Optional resolution note.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if found and resolved.</returns>
    Task<bool> ResolveAsync(
        Guid instanceId,
        string username,
        string? note = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suppresses an alert instance.
    /// </summary>
    /// <param name="instanceId">Instance ID.</param>
    /// <param name="username">User suppressing.</param>
    /// <param name="note">Reason for suppression.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if found and suppressed.</returns>
    Task<bool> SuppressAsync(
        Guid instanceId,
        string username,
        string? note = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk acknowledges multiple instances.
    /// </summary>
    /// <param name="instanceIds">Instance IDs.</param>
    /// <param name="username">User acknowledging.</param>
    /// <param name="note">Optional note.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number acknowledged.</returns>
    Task<int> BulkAcknowledgeAsync(
        IEnumerable<Guid> instanceIds,
        string username,
        string? note = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk resolves multiple instances.
    /// </summary>
    /// <param name="instanceIds">Instance IDs.</param>
    /// <param name="username">User resolving.</param>
    /// <param name="note">Optional note.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number resolved.</returns>
    Task<int> BulkResolveAsync(
        IEnumerable<Guid> instanceIds,
        string username,
        string? note = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that a notification was sent.
    /// </summary>
    /// <param name="instanceId">Instance ID.</param>
    /// <param name="channel">Notification channel.</param>
    /// <param name="recipient">Recipient.</param>
    /// <param name="success">Whether successful.</param>
    /// <param name="errorMessage">Error message if failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordNotificationAsync(
        Guid instanceId,
        string channel,
        string recipient,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Maintenance

    /// <summary>
    /// Deletes old resolved instances.
    /// </summary>
    /// <param name="olderThan">Delete instances resolved before this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number deleted.</returns>
    Task<int> DeleteOldResolvedAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Alert instance summary for dashboard.
/// </summary>
public class AlertInstanceSummary
{
    public int Total { get; set; }
    public int New { get; set; }
    public int Acknowledged { get; set; }
    public int Resolved { get; set; }
    public int Suppressed { get; set; }
    public int Critical { get; set; }
    public int High { get; set; }
    public int Medium { get; set; }
    public int Low { get; set; }
}
