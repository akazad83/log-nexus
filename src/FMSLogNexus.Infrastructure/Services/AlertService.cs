using Microsoft.Extensions.Logging;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Repositories;
using FMSLogNexus.Core.Interfaces.Services;
using System.Text.Json;

namespace FMSLogNexus.Infrastructure.Services;

/// <summary>
/// Service implementation for alert operations.
/// </summary>
public class AlertService : ServiceBase, IAlertService
{
    private readonly INotificationService? _notificationService;
    private readonly IRealTimeService? _realTimeService;

    public AlertService(
        IUnitOfWork unitOfWork,
        ILogger<AlertService> logger,
        INotificationService? notificationService = null,
        IRealTimeService? realTimeService = null)
        : base(unitOfWork, logger)
    {
        _notificationService = notificationService;
        _realTimeService = realTimeService;
    }

    #region Alert Rule Management

    /// <inheritdoc />
    public async Task<AlertDetailResponse> CreateAlertAsync(
        CreateAlertRequest request,
        CancellationToken cancellationToken = default)
    {
        var alert = new Alert
        {
            Name = request.Name,
            Description = request.Description,
            AlertType = request.AlertType,
            Severity = request.Severity,
            Condition = JsonSerializer.Serialize(request.Condition),
            JobId = request.JobId,
            ServerName = request.ServerName,
            NotificationChannels = request.NotificationChannels != null
                ? JsonSerializer.Serialize(request.NotificationChannels)
                : null,
            ThrottleMinutes = request.ThrottleMinutes,
            IsActive = true
        };

        await UnitOfWork.Alerts.AddAsync(alert, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Created alert {AlertName} ({AlertId})", alert.Name, alert.Id);

        return MapToDetailResponse(alert);
    }

    /// <inheritdoc />
    public async Task<AlertDetailResponse?> GetAlertAsync(
        Guid alertId,
        CancellationToken cancellationToken = default)
    {
        var alert = await UnitOfWork.Alerts.GetByIdAsync(alertId, cancellationToken);
        return alert == null ? null : MapToDetailResponse(alert);
    }

    /// <inheritdoc />
    public async Task<AlertDetailResponse?> UpdateAlertAsync(
        Guid alertId,
        UpdateAlertRequest request,
        CancellationToken cancellationToken = default)
    {
        var alert = await UnitOfWork.Alerts.GetByIdAsync(alertId, cancellationToken);
        if (alert == null)
            return null;

        if (request.Name != null) alert.Name = request.Name;
        if (request.Description != null) alert.Description = request.Description;
        if (request.Severity.HasValue) alert.Severity = request.Severity.Value;
        if (request.Condition != null) alert.Condition = JsonSerializer.Serialize(request.Condition);
        if (request.NotificationChannels != null) alert.NotificationChannels = JsonSerializer.Serialize(request.NotificationChannels);
        if (request.ThrottleMinutes.HasValue) alert.ThrottleMinutes = request.ThrottleMinutes.Value;
        if (request.IsActive.HasValue) alert.IsActive = request.IsActive.Value;

        alert.UpdatedAt = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Updated alert {AlertName} ({AlertId})", alert.Name, alertId);

        return MapToDetailResponse(alert);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAlertAsync(
        Guid alertId,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.Alerts.DeactivateAsync(alertId, cancellationToken);
        if (result)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Deactivated alert {AlertId}", alertId);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertListItem>> GetAlertsAsync(
        bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var alerts = activeOnly
            ? await UnitOfWork.Alerts.GetActiveAsync(cancellationToken)
            : await UnitOfWork.Alerts.GetAllAsync(cancellationToken);

        return alerts.Select(MapToListItem).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> ActivateAlertAsync(
        Guid alertId,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.Alerts.ActivateAsync(alertId, cancellationToken);
        if (result)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Activated alert {AlertId}", alertId);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateAlertAsync(
        Guid alertId,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.Alerts.DeactivateAsync(alertId, cancellationToken);
        if (result)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Deactivated alert {AlertId}", alertId);
        }
        return result;
    }

    #endregion

    #region Alert Instance Management

    /// <inheritdoc />
    public async Task<AlertInstanceDetailResponse?> GetInstanceAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default)
    {
        var instance = await UnitOfWork.AlertInstances.GetWithAlertAsync(instanceId, cancellationToken);
        return instance == null ? null : MapToInstanceDetailResponse(instance);
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<AlertInstanceListItem>> SearchInstancesAsync(
        AlertInstanceSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.AlertInstances.SearchAsync(request, cancellationToken);

        return PaginatedResult<AlertInstanceListItem>.Create(
            result.Items.Select(MapToInstanceListItem),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertInstanceListItem>> GetActiveInstancesAsync(
        CancellationToken cancellationToken = default)
    {
        var instances = await UnitOfWork.AlertInstances.GetActiveAsync(cancellationToken);
        return instances.Select(MapToInstanceListItem).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertInstanceListItem>> GetUnacknowledgedInstancesAsync(
        CancellationToken cancellationToken = default)
    {
        var instances = await UnitOfWork.AlertInstances.GetUnacknowledgedAsync(cancellationToken);
        return instances.Select(MapToInstanceListItem).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> AcknowledgeInstanceAsync(
        Guid instanceId,
        AcknowledgeAlertRequest request,
        string username,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.AlertInstances.AcknowledgeAsync(instanceId, username, request.Note, cancellationToken);
        if (result)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Acknowledged alert instance {InstanceId} by {User}", instanceId, username);

            if (_realTimeService != null)
            {
                await _realTimeService.BroadcastAlertAcknowledgedAsync(instanceId, username, cancellationToken);
            }
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> ResolveInstanceAsync(
        Guid instanceId,
        ResolveAlertRequest request,
        string username,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.AlertInstances.ResolveAsync(instanceId, username, request.Note, cancellationToken);
        if (result)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Resolved alert instance {InstanceId} by {User}", instanceId, username);

            if (_realTimeService != null)
            {
                await _realTimeService.BroadcastAlertResolvedAsync(instanceId, username, cancellationToken);
            }
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> SuppressInstanceAsync(
        Guid instanceId,
        string? note,
        string username,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.AlertInstances.SuppressAsync(instanceId, username, note, cancellationToken);
        if (result)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Suppressed alert instance {InstanceId} by {User}", instanceId, username);

            // TODO: No BroadcastAlertSuppressedAsync method exists in IRealTimeService
            // Consider adding this method to the interface or using a generic notification
            // if (_realTimeService != null)
            // {
            //     await _realTimeService.BroadcastAlertSuppressedAsync(instanceId, username, cancellationToken);
            // }
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<int> BulkAcknowledgeAsync(
        IEnumerable<Guid> instanceIds,
        string? note,
        string username,
        CancellationToken cancellationToken = default)
    {
        var count = await UnitOfWork.AlertInstances.BulkAcknowledgeAsync(instanceIds, username, note, cancellationToken);
        if (count > 0)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Bulk acknowledged {Count} alert instances by {User}", count, username);
        }
        return count;
    }

    /// <inheritdoc />
    public async Task<int> BulkResolveAsync(
        IEnumerable<Guid> instanceIds,
        string? note,
        string username,
        CancellationToken cancellationToken = default)
    {
        var count = await UnitOfWork.AlertInstances.BulkResolveAsync(instanceIds, username, note, cancellationToken);
        if (count > 0)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Bulk resolved {Count} alert instances by {User}", count, username);
        }
        return count;
    }

    #endregion

    #region Alert Evaluation

    /// <inheritdoc />
    public async Task<int> EvaluateAlertsAsync(CancellationToken cancellationToken = default)
    {
        var alerts = await UnitOfWork.Alerts.GetActiveAsync(cancellationToken);
        var triggeredCount = 0;

        foreach (var alert in alerts)
        {
            try
            {
                var shouldTrigger = await EvaluateAlertConditionAsync(alert, cancellationToken);
                if (shouldTrigger)
                {
                    await TriggerAlertAsync(
                        alert.Id,
                        $"Alert condition met for '{alert.Name}'",
                        null,
                        alert.JobId,
                        alert.ServerName,
                        cancellationToken);
                    triggeredCount++;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error evaluating alert {AlertId}", alert.Id);
            }
        }

        return triggeredCount;
    }

    /// <inheritdoc />
    public async Task<AlertInstanceListItem?> TriggerAlertAsync(
        Guid alertId,
        string message,
        Dictionary<string, object>? context = null,
        string? jobId = null,
        string? serverName = null,
        CancellationToken cancellationToken = default)
    {
        var alert = await UnitOfWork.Alerts.GetByIdAsync(alertId, cancellationToken);
        if (alert == null || !alert.IsActive)
            return null;

        // Check throttle
        if (alert.LastTriggeredAt.HasValue &&
            (DateTime.UtcNow - alert.LastTriggeredAt.Value).TotalMinutes < alert.ThrottleMinutes)
        {
            Logger.LogDebug("Alert {AlertId} is throttled, skipping", alertId);
            return null;
        }

        var instance = new AlertInstance
        {
            AlertId = alertId,
            Severity = alert.Severity,
            Message = message,
            JobId = jobId ?? alert.JobId,
            ServerName = serverName ?? alert.ServerName,
            Context = context != null ? JsonSerializer.Serialize(context) : null,
            Status = AlertInstanceStatus.New,
            TriggeredAt = DateTime.UtcNow
        };

        await UnitOfWork.AlertInstances.AddAsync(instance, cancellationToken);
        await UnitOfWork.Alerts.RecordTriggerAsync(alertId, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogWarning("Triggered alert {AlertName} ({AlertId}): {Message}", alert.Name, alertId, message);

        if (_notificationService != null && !string.IsNullOrEmpty(alert.NotificationChannels))
        {
            await _notificationService.SendAlertNotificationAsync(instance, alert, cancellationToken);
        }

        if (_realTimeService != null)
        {
            var alertListItem = MapToInstanceListItem(instance);
            await _realTimeService.BroadcastNewAlertAsync(alertListItem, cancellationToken);
        }

        return MapToInstanceListItem(instance);
    }

    #endregion

    #region Statistics

    /// <inheritdoc />
    public async Task<DashboardAlertStats> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await UnitOfWork.AlertInstances.GetSummaryAsync(cancellationToken);
        return new DashboardAlertStats
        {
            Total = summary.Total,
            New = summary.New,
            Critical = summary.Critical,
            High = summary.High
        };
    }

    /// <inheritdoc />
    public async Task<Dictionary<AlertInstanceStatus, int>> GetCountsByStatusAsync(
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.AlertInstances.GetCountsByStatusAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Dictionary<AlertSeverity, int>> GetCountsBySeverityAsync(
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.AlertInstances.GetCountsBySeverityAsync(true, cancellationToken);
    }

    #endregion

    #region Private Methods

    private async Task<bool> EvaluateAlertConditionAsync(Alert alert, CancellationToken cancellationToken)
    {
        // Basic condition evaluation based on alert type
        return alert.AlertType switch
        {
            AlertType.ErrorThreshold => await EvaluateErrorThresholdConditionAsync(alert, cancellationToken),
            AlertType.JobFailure => await EvaluateJobFailureConditionAsync(alert, cancellationToken),
            AlertType.ServerOffline => await EvaluateServerOfflineConditionAsync(alert, cancellationToken),
            _ => false
        };
    }

    private async Task<bool> EvaluateErrorThresholdConditionAsync(Alert alert, CancellationToken cancellationToken)
    {
        if (alert.Condition == null) return false;

        // Parse condition and check error counts
        // This is a simplified implementation
        return false;
    }

    private async Task<bool> EvaluateJobFailureConditionAsync(Alert alert, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(alert.JobId)) return false;

        // Check for recent job failures
        // This is a simplified implementation
        return false;
    }

    private async Task<bool> EvaluateServerOfflineConditionAsync(Alert alert, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(alert.ServerName)) return false;

        // Check server status
        // This is a simplified implementation
        return false;
    }

    private static AlertDetailResponse MapToDetailResponse(Alert alert)
    {
        return new AlertDetailResponse
        {
            Id = alert.Id,
            Name = alert.Name,
            Description = alert.Description,
            AlertType = alert.AlertType,
            Severity = alert.Severity,
            IsActive = alert.IsActive,
            TriggerCount = alert.TriggerCount,
            LastTriggeredAt = alert.LastTriggeredAt,
            CreatedAt = alert.CreatedAt
        };
    }

    private static AlertListItem MapToListItem(Alert alert)
    {
        return new AlertListItem
        {
            Id = alert.Id,
            Name = alert.Name,
            Description = alert.Description,
            AlertType = alert.AlertType,
            Severity = alert.Severity,
            IsActive = alert.IsActive,
            TriggerCount = alert.TriggerCount,
            LastTriggeredAt = alert.LastTriggeredAt,
            ThrottleMinutes = alert.ThrottleMinutes,
            JobId = alert.JobId,
            ServerName = alert.ServerName
        };
    }

    private static AlertInstanceDetailResponse MapToInstanceDetailResponse(AlertInstance instance)
    {
        return new AlertInstanceDetailResponse
        {
            Id = instance.Id,
            AlertId = instance.AlertId,
            AlertName = instance.Alert?.Name ?? string.Empty,
            Severity = instance.Severity,
            Message = instance.Message,
            Status = instance.Status,
            TriggeredAt = instance.TriggeredAt,
            AcknowledgedAt = instance.AcknowledgedAt,
            AcknowledgedBy = instance.AcknowledgedBy,
            ResolvedAt = instance.ResolvedAt,
            ResolvedBy = instance.ResolvedBy
        };
    }

    private static AlertInstanceListItem MapToInstanceListItem(AlertInstance instance)
    {
        return new AlertInstanceListItem
        {
            Id = instance.Id,
            AlertId = instance.AlertId,
            AlertName = instance.Alert?.Name ?? string.Empty,
            Severity = instance.Severity,
            Message = instance.Message,
            Status = instance.Status,
            TriggeredAt = instance.TriggeredAt,
            AcknowledgedAt = instance.AcknowledgedAt,
            ResolvedAt = instance.ResolvedAt
        };
    }

    #endregion
}
