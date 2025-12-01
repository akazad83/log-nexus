using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Interfaces.Services;

/// <summary>
/// Service interface for alert operations.
/// Handles alert rule management, instance lifecycle, and notifications.
/// </summary>
public interface IAlertService
{
    #region Alert Rule Management

    /// <summary>
    /// Creates a new alert rule.
    /// </summary>
    /// <param name="request">Alert creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created alert response.</returns>
    Task<AlertDetailResponse> CreateAlertAsync(
        CreateAlertRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an alert by ID.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Alert detail response or null.</returns>
    Task<AlertDetailResponse?> GetAlertAsync(
        Guid alertId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an alert rule.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="request">Update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated alert response.</returns>
    Task<AlertDetailResponse?> UpdateAlertAsync(
        Guid alertId,
        UpdateAlertRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an alert rule.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted.</returns>
    Task<bool> DeleteAlertAsync(
        Guid alertId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all alert rules.
    /// </summary>
    /// <param name="activeOnly">Only active alerts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Alert list.</returns>
    Task<IReadOnlyList<AlertListItem>> GetAlertsAsync(
        bool activeOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates an alert.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if activated.</returns>
    Task<bool> ActivateAlertAsync(
        Guid alertId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates an alert.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deactivated.</returns>
    Task<bool> DeactivateAlertAsync(
        Guid alertId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Alert Instance Management

    /// <summary>
    /// Gets an alert instance by ID.
    /// </summary>
    /// <param name="instanceId">Instance ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Alert instance detail or null.</returns>
    Task<AlertInstanceDetailResponse?> GetInstanceAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches alert instances with filtering and pagination.
    /// </summary>
    /// <param name="request">Search request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated alert instances.</returns>
    Task<PaginatedResult<AlertInstanceListItem>> SearchInstancesAsync(
        AlertInstanceSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active (unresolved) alert instances.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active alert instances.</returns>
    Task<IReadOnlyList<AlertInstanceListItem>> GetActiveInstancesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unacknowledged alert instances.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unacknowledged alert instances.</returns>
    Task<IReadOnlyList<AlertInstanceListItem>> GetUnacknowledgedInstancesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledges an alert instance.
    /// </summary>
    /// <param name="instanceId">Instance ID.</param>
    /// <param name="request">Acknowledge request.</param>
    /// <param name="username">User acknowledging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if acknowledged.</returns>
    Task<bool> AcknowledgeInstanceAsync(
        Guid instanceId,
        AcknowledgeAlertRequest request,
        string username,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an alert instance.
    /// </summary>
    /// <param name="instanceId">Instance ID.</param>
    /// <param name="request">Resolve request.</param>
    /// <param name="username">User resolving.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if resolved.</returns>
    Task<bool> ResolveInstanceAsync(
        Guid instanceId,
        ResolveAlertRequest request,
        string username,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suppresses an alert instance.
    /// </summary>
    /// <param name="instanceId">Instance ID.</param>
    /// <param name="note">Suppression note.</param>
    /// <param name="username">User suppressing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if suppressed.</returns>
    Task<bool> SuppressInstanceAsync(
        Guid instanceId,
        string? note,
        string username,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk acknowledges multiple instances.
    /// </summary>
    /// <param name="instanceIds">Instance IDs.</param>
    /// <param name="note">Optional note.</param>
    /// <param name="username">User acknowledging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number acknowledged.</returns>
    Task<int> BulkAcknowledgeAsync(
        IEnumerable<Guid> instanceIds,
        string? note,
        string username,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk resolves multiple instances.
    /// </summary>
    /// <param name="instanceIds">Instance IDs.</param>
    /// <param name="note">Optional note.</param>
    /// <param name="username">User resolving.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number resolved.</returns>
    Task<int> BulkResolveAsync(
        IEnumerable<Guid> instanceIds,
        string? note,
        string username,
        CancellationToken cancellationToken = default);

    #endregion

    #region Alert Evaluation

    /// <summary>
    /// Evaluates all active alerts.
    /// Should be called periodically by a background service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of alerts triggered.</returns>
    Task<int> EvaluateAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers an alert manually (creates an instance).
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="message">Alert message.</param>
    /// <param name="context">Additional context.</param>
    /// <param name="jobId">Related job ID.</param>
    /// <param name="serverName">Related server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created alert instance.</returns>
    Task<AlertInstanceListItem?> TriggerAlertAsync(
        Guid alertId,
        string message,
        Dictionary<string, object>? context = null,
        string? jobId = null,
        string? serverName = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets alert summary for dashboard.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Alert summary.</returns>
    Task<DashboardAlertStats> GetSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets instance counts by status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Counts by status.</returns>
    Task<Dictionary<AlertInstanceStatus, int>> GetCountsByStatusAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets instance counts by severity (unresolved only).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Counts by severity.</returns>
    Task<Dictionary<AlertSeverity, int>> GetCountsBySeverityAsync(
        CancellationToken cancellationToken = default);

    #endregion
}
