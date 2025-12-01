using Microsoft.EntityFrameworkCore;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Repositories;
using RepoAlertInstanceSummary = FMSLogNexus.Core.Interfaces.Repositories.AlertInstanceSummary;

namespace FMSLogNexus.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for alert rule operations.
/// </summary>
public class AlertRepository : RepositoryBase<Alert>, IAlertRepository
{
    public AlertRepository(FMSLogNexusDbContext context) : base(context)
    {
    }

    #region Query Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<Alert>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Alert>> GetByTypeAsync(
        AlertType alertType,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(a => a.AlertType == alertType);
        if (activeOnly)
            query = query.Where(a => a.IsActive);

        return await query.OrderBy(a => a.Name).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Alert>> GetByJobAsync(
        string jobId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(a => a.JobId == jobId);
        if (activeOnly)
            query = query.Where(a => a.IsActive);

        return await query.OrderBy(a => a.Name).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Alert>> GetByServerAsync(
        string serverName,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(a => a.ServerName == serverName);
        if (activeOnly)
            query = query.Where(a => a.IsActive);

        return await query.OrderBy(a => a.Name).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Alert?> GetWithInstancesAsync(
        Guid alertId,
        int instanceCount = 10,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.Instances
                .OrderByDescending(i => i.TriggeredAt)
                .Take(instanceCount))
            .FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Alert>> GetTriggerableAsync(CancellationToken cancellationToken = default)
    {
        // Get active alerts that haven't been triggered recently (based on throttle)
        return await DbSet.AsNoTracking()
            .Where(a => a.IsActive)
            .Where(a => !a.LastTriggeredAt.HasValue ||
                        a.LastTriggeredAt < DateTime.UtcNow.AddMinutes(-a.ThrottleMinutes))
            .OrderBy(a => a.LastTriggeredAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> CanTriggerAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        var alert = await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);

        if (alert == null || !alert.IsActive)
            return false;

        if (!alert.LastTriggeredAt.HasValue)
            return true;

        return alert.LastTriggeredAt < DateTime.UtcNow.AddMinutes(-alert.ThrottleMinutes);
    }

    #endregion

    #region Lifecycle

    /// <inheritdoc />
    public async Task<bool> ActivateAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        var alert = await DbSet.FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);
        if (alert == null)
            return false;

        alert.IsActive = true;
        alert.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        var alert = await DbSet.FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);
        if (alert == null)
            return false;

        alert.IsActive = false;
        alert.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <inheritdoc />
    public async Task RecordTriggerAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        var alert = await DbSet.FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);
        if (alert == null)
            return;

        alert.LastTriggeredAt = DateTime.UtcNow;
        alert.TriggerCount++;
        alert.UpdatedAt = DateTime.UtcNow;
    }

    #endregion
}

/// <summary>
/// Repository implementation for alert instance operations.
/// </summary>
public class AlertInstanceRepository : RepositoryBase<AlertInstance>, IAlertInstanceRepository
{
    public AlertInstanceRepository(FMSLogNexusDbContext context) : base(context)
    {
    }

    #region Query Operations

    /// <inheritdoc />
    public async Task<PaginatedResult<AlertInstance>> SearchAsync(
        AlertInstanceSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        if (request.AlertId.HasValue)
            query = query.Where(i => i.AlertId == request.AlertId.Value);

        if (request.Status.HasValue)
            query = query.Where(i => i.Status == request.Status.Value);

        if (request.Severity.HasValue)
            query = query.Where(i => i.Severity == request.Severity.Value);

        if (request.StartDate.HasValue)
            query = query.Where(i => i.TriggeredAt >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(i => i.TriggeredAt <= request.EndDate.Value);

        if (!string.IsNullOrWhiteSpace(request.JobId))
            query = query.Where(i => i.JobId == request.JobId);

        if (!string.IsNullOrWhiteSpace(request.ServerName))
            query = query.Where(i => i.ServerName == request.ServerName);

        var totalCount = await query.CountAsync(cancellationToken);
        query = query.OrderByDescending(i => i.TriggeredAt);

        var page = request.Page;
        var pageSize = request.PageSize;

        var items = await query
            .Include(i => i.Alert)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PaginatedResult<AlertInstance>.Create(items, totalCount, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertInstance>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(i => i.Status == AlertInstanceStatus.New || i.Status == AlertInstanceStatus.Acknowledged)
            .Include(i => i.Alert)
            .OrderByDescending(i => i.Severity)
            .ThenByDescending(i => i.TriggeredAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertInstance>> GetUnacknowledgedAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(i => i.Status == AlertInstanceStatus.New)
            .Include(i => i.Alert)
            .OrderByDescending(i => i.Severity)
            .ThenByDescending(i => i.TriggeredAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<AlertInstance>> GetByAlertAsync(
        Guid alertId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(i => i.AlertId == alertId);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.TriggeredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PaginatedResult<AlertInstance>.Create(items, totalCount, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertInstance>> GetByJobAsync(
        string jobId,
        bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(i => i.JobId == jobId);
        if (activeOnly)
            query = query.Where(i => i.Status == AlertInstanceStatus.New || i.Status == AlertInstanceStatus.Acknowledged);

        return await query.OrderByDescending(i => i.TriggeredAt).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertInstance>> GetByServerAsync(
        string serverName,
        bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(i => i.ServerName == serverName);
        if (activeOnly)
            query = query.Where(i => i.Status == AlertInstanceStatus.New || i.Status == AlertInstanceStatus.Acknowledged);

        return await query.OrderByDescending(i => i.TriggeredAt).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AlertInstance?> GetWithAlertAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.Alert)
            .FirstOrDefaultAsync(i => i.Id == instanceId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertInstance>> GetRecentBySeverityAsync(
        AlertSeverity severity,
        int count = 20,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(i => i.Severity >= severity)
            .OrderByDescending(i => i.TriggeredAt)
            .Take(count)
            .Include(i => i.Alert)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Summary Operations

    /// <inheritdoc />
    public async Task<Dictionary<AlertInstanceStatus, int>> GetCountsByStatusAsync(CancellationToken cancellationToken = default)
    {
        var counts = await DbSet.AsNoTracking()
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.Status, x => x.Count);
    }

    /// <inheritdoc />
    public async Task<Dictionary<AlertSeverity, int>> GetCountsBySeverityAsync(
        bool unresolvedOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();
        if (unresolvedOnly)
            query = query.Where(i => i.Status != AlertInstanceStatus.Resolved);

        var counts = await query
            .GroupBy(i => i.Severity)
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.Severity, x => x.Count);
    }

    /// <inheritdoc />
    public async Task<RepoAlertInstanceSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var instances = await DbSet.AsNoTracking()
            .Where(i => i.Status != AlertInstanceStatus.Resolved)
            .ToListAsync(cancellationToken);

        return new RepoAlertInstanceSummary
        {
            Total = instances.Count,
            New = instances.Count(i => i.Status == AlertInstanceStatus.New),
            Acknowledged = instances.Count(i => i.Status == AlertInstanceStatus.Acknowledged),
            Resolved = 0,
            Suppressed = instances.Count(i => i.Status == AlertInstanceStatus.Suppressed),
            Critical = instances.Count(i => i.Severity == AlertSeverity.Critical),
            High = instances.Count(i => i.Severity == AlertSeverity.High),
            Medium = instances.Count(i => i.Severity == AlertSeverity.Medium),
            Low = instances.Count(i => i.Severity == AlertSeverity.Low)
        };
    }

    #endregion

    #region Lifecycle Operations

    /// <inheritdoc />
    public async Task<AlertInstance> CreateAsync(AlertInstance instance, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(instance, cancellationToken);
        return instance;
    }

    /// <inheritdoc />
    public async Task<bool> AcknowledgeAsync(
        Guid instanceId,
        string username,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var instance = await DbSet.FirstOrDefaultAsync(i => i.Id == instanceId, cancellationToken);
        if (instance == null || instance.Status != AlertInstanceStatus.New)
            return false;

        instance.Status = AlertInstanceStatus.Acknowledged;
        instance.AcknowledgedAt = DateTime.UtcNow;
        instance.AcknowledgedBy = username;
        instance.AcknowledgeNote = note;
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ResolveAsync(
        Guid instanceId,
        string username,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var instance = await DbSet.FirstOrDefaultAsync(i => i.Id == instanceId, cancellationToken);
        if (instance == null || instance.Status == AlertInstanceStatus.Resolved)
            return false;

        instance.Status = AlertInstanceStatus.Resolved;
        instance.ResolvedAt = DateTime.UtcNow;
        instance.ResolvedBy = username;
        instance.ResolutionNote = note;
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> SuppressAsync(
        Guid instanceId,
        string username,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var instance = await DbSet.FirstOrDefaultAsync(i => i.Id == instanceId, cancellationToken);
        if (instance == null)
            return false;

        instance.Status = AlertInstanceStatus.Suppressed;
        return true;
    }

    /// <inheritdoc />
    public async Task<int> BulkAcknowledgeAsync(
        IEnumerable<Guid> instanceIds,
        string username,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var ids = instanceIds.ToList();
        var instances = await DbSet
            .Where(i => ids.Contains(i.Id) && i.Status == AlertInstanceStatus.New)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var instance in instances)
        {
            instance.Status = AlertInstanceStatus.Acknowledged;
            instance.AcknowledgedAt = now;
            instance.AcknowledgedBy = username;
            instance.AcknowledgeNote = note;
        }

        return instances.Count;
    }

    /// <inheritdoc />
    public async Task<int> BulkResolveAsync(
        IEnumerable<Guid> instanceIds,
        string username,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var ids = instanceIds.ToList();
        var instances = await DbSet
            .Where(i => ids.Contains(i.Id) && i.Status != AlertInstanceStatus.Resolved)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var instance in instances)
        {
            instance.Status = AlertInstanceStatus.Resolved;
            instance.ResolvedAt = now;
            instance.ResolvedBy = username;
            instance.ResolutionNote = note;
        }

        return instances.Count;
    }

    /// <inheritdoc />
    public Task RecordNotificationAsync(
        Guid instanceId,
        string channel,
        string recipient,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        // This would typically record to a separate notifications table
        // For now, just complete the task
        return Task.CompletedTask;
    }

    #endregion

    #region Maintenance

    /// <inheritdoc />
    public async Task<int> DeleteOldResolvedAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var oldInstances = await DbSet
            .Where(i => i.TriggeredAt < olderThan)
            .Where(i => i.Status == AlertInstanceStatus.Resolved || i.Status == AlertInstanceStatus.Suppressed)
            .ToListAsync(cancellationToken);

        DbSet.RemoveRange(oldInstances);
        return oldInstances.Count;
    }

    #endregion
}
