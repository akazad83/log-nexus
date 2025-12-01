using Microsoft.Extensions.Logging;
using System.Text.Json;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Interfaces.Repositories;
using FMSLogNexus.Core.Interfaces.Services;

namespace FMSLogNexus.Infrastructure.Services;

/// <summary>
/// Service implementation for system configuration.
/// </summary>
public class ConfigurationService : ServiceBase, IConfigurationService
{
    private readonly ICacheService? _cacheService;

    public ConfigurationService(
        IUnitOfWork unitOfWork,
        ILogger<ConfigurationService> logger,
        ICacheService? cacheService = null)
        : base(unitOfWork, logger)
    {
        _cacheService = cacheService;
    }

    /// <inheritdoc />
    public async Task<string?> GetValueAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        // Try cache first
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<string>($"config:{key}", cancellationToken);
            if (cached != null)
                return cached;
        }

        var value = await UnitOfWork.SystemConfiguration.GetValueAsync(key, cancellationToken);

        // Cache the value
        if (_cacheService != null && value != null)
        {
            await _cacheService.SetAsync($"config:{key}", value, TimeSpan.FromMinutes(5), cancellationToken);
        }

        return value;
    }

    /// <inheritdoc />
    public async Task<T?> GetValueAsync<T>(
        string key,
        T? defaultValue = default,
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.SystemConfiguration.GetValueAsync<T>(key, defaultValue, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConfigurationResponse> GetAllConfigurationAsync(
        CancellationToken cancellationToken = default)
    {
        var grouped = await UnitOfWork.SystemConfiguration.GetAllGroupedAsync(cancellationToken);

        var response = new ConfigurationResponse();

        foreach (var category in grouped)
        {
            var categoryDict = new Dictionary<string, ConfigurationItem>();
            foreach (var config in category.Value)
            {
                categoryDict[config.Id] = new ConfigurationItem
                {
                    Key = config.Id,
                    Value = config.Value,
                    DataType = config.DataType ?? "string",
                    Description = config.Description,
                    IsEncrypted = config.IsEncrypted,
                    UpdatedAt = config.UpdatedAt
                };
            }
            response.Categories[category.Key] = categoryDict;
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, ConfigurationItem>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        var items = await UnitOfWork.SystemConfiguration.GetByCategoryAsync(category, cancellationToken);

        var result = new Dictionary<string, ConfigurationItem>();
        foreach (var config in items)
        {
            result[config.Id] = new ConfigurationItem
            {
                Key = config.Id,
                Value = config.Value,
                DataType = config.DataType ?? "string",
                Description = config.Description,
                IsEncrypted = config.IsEncrypted,
                UpdatedAt = config.UpdatedAt
            };
        }

        return result;
    }

    /// <inheritdoc />
    public async Task SetValueAsync(
        string key,
        object? value,
        string? username = null,
        CancellationToken cancellationToken = default)
    {
        await UnitOfWork.SystemConfiguration.SetValueAsync(key, value, username, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        // Invalidate cache
        if (_cacheService != null)
        {
            await _cacheService.RemoveAsync($"config:{key}", cancellationToken);
        }

        Logger.LogInformation("Configuration {Key} updated by {User}", key, username ?? "system");
    }

    /// <inheritdoc />
    public async Task UpdateConfigurationAsync(
        UpdateConfigurationRequest request,
        string? username = null,
        CancellationToken cancellationToken = default)
    {
        await UnitOfWork.SystemConfiguration.SetValuesAsync(request.Values, username, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        // Invalidate cache for all updated keys
        if (_cacheService != null)
        {
            foreach (var key in request.Values.Keys)
            {
                await _cacheService.RemoveAsync($"config:{key}", cancellationToken);
            }
        }

        Logger.LogInformation("Configuration updated ({Count} values) by {User}",
            request.Values.Count, username ?? "system");
    }

    /// <inheritdoc />
    public async Task<int> GetLogRetentionDaysAsync(CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.SystemConfiguration.GetValueAsync("Retention.Logs.Days", 90, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetExecutionRetentionDaysAsync(CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.SystemConfiguration.GetValueAsync("Retention.Executions.Days", 365, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetAlertRetentionDaysAsync(CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.SystemConfiguration.GetValueAsync("Retention.Alerts.Days", 180, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetHeartbeatTimeoutSecondsAsync(CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.SystemConfiguration.GetValueAsync("Server.HeartbeatTimeout.Seconds", 300, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetMaxLoginAttemptsAsync(CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.SystemConfiguration.GetValueAsync("Security.MaxLoginAttempts", 5, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetLockoutMinutesAsync(CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.SystemConfiguration.GetValueAsync("Security.LockoutMinutes", 15, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        if (_cacheService == null)
            return;

        // Get all configuration values
        var allConfig = await UnitOfWork.SystemConfiguration.GetAllForCacheAsync(cancellationToken);

        // Cache each value
        foreach (var config in allConfig)
        {
            await _cacheService.SetAsync($"config:{config.Key}", config.Value ?? string.Empty,
                TimeSpan.FromMinutes(10), cancellationToken);
        }

        Logger.LogInformation("Configuration cache refreshed with {Count} values", allConfig.Count);
    }

    /// <inheritdoc />
    public async Task<bool> SetValueAsync(
        string key,
        string value,
        Guid? userId = null,
        string? username = null,
        CancellationToken cancellationToken = default)
    {
        var oldValue = await UnitOfWork.SystemConfiguration.GetValueAsync(key, cancellationToken);

        try
        {
            await UnitOfWork.SystemConfiguration.SetValueAsync(key, value, username, cancellationToken);
            await SaveChangesAsync(cancellationToken);

            // Invalidate cache
            if (_cacheService != null)
            {
                await _cacheService.RemoveAsync($"config:{key}", cancellationToken);
            }

            // Audit log
            await UnitOfWork.AuditLog.LogUpdateAsync(
                userId,
                username,
                "SystemConfiguration",
                key,
                new { Value = oldValue },
                new { Value = value },
                null,
                cancellationToken);
            await SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Configuration {Key} updated by {User}", key, username ?? "system");
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigurationItemResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await UnitOfWork.SystemConfiguration.GetAllAsync(cancellationToken);
        return items.Select(MapToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.SystemConfiguration.GetCategoriesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConfigurationItemResponse> CreateOrUpdateAsync(
        CreateConfigurationRequest request,
        Guid? userId = null,
        string? username = null,
        CancellationToken cancellationToken = default)
    {
        var config = new SystemConfiguration
        {
            Id = request.Key,
            Value = request.Value?.ToString() ?? string.Empty,
            Category = request.Category,
            Description = request.Description,
            DataType = request.DataType ?? "string",
            UpdatedBy = username,
            UpdatedAt = DateTime.UtcNow
        };

        await UnitOfWork.SystemConfiguration.UpsertAsync(config, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        // Invalidate cache
        if (_cacheService != null)
        {
            await _cacheService.RemoveAsync($"config:{request.Key}", cancellationToken);
        }

        Logger.LogInformation("Configuration {Key} created/updated by {User}", request.Key, username ?? "system");

        return MapToResponse(config);
    }

    private static ConfigurationItemResponse MapToResponse(SystemConfiguration config)
    {
        return new ConfigurationItemResponse
        {
            Key = config.Id,
            Value = config.Value ?? string.Empty,
            Category = config.Category,
            Description = config.Description,
            DataType = config.DataType,
            UpdatedAt = config.UpdatedAt,
            UpdatedBy = config.UpdatedBy
        };
    }
}

/// <summary>
/// Service implementation for audit log operations.
/// </summary>
public class AuditService : ServiceBase, IAuditService
{
    public AuditService(
        IUnitOfWork unitOfWork,
        ILogger<AuditService> logger)
        : base(unitOfWork, logger)
    {
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<AuditLogResponse>> SearchAsync(
        AuditSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        // Convert AuditSearchRequest to AuditLogSearchRequest
        var searchRequest = new Core.Interfaces.Repositories.AuditLogSearchRequest
        {
            UserId = request.UserId,
            Username = request.Username,
            Action = request.Action,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            // IpAddress might not be in AuditSearchRequest
            Page = request.Page,
            PageSize = request.PageSize
        };

        var result = await UnitOfWork.AuditLog.SearchAsync(searchRequest, cancellationToken);

        return new PaginatedResult<AuditLogResponse>
        {
            Items = result.Items.Select(MapToResponse).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLogResponse>> GetByUserAsync(
        Guid userId,
        int count = 100,
        CancellationToken cancellationToken = default)
    {
        // GetByUserAsync takes DateTime? parameters, not count
        var entries = await UnitOfWork.AuditLog.GetByUserAsync(userId, null, null, cancellationToken);
        // Limit to count
        return entries.Take(count).Select(MapToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLogResponse>> GetByEntityAsync(
        string entityType,
        string entityId,
        int count = 100,
        CancellationToken cancellationToken = default)
    {
        // GetByEntityAsync doesn't take a count parameter
        var entries = await UnitOfWork.AuditLog.GetByEntityAsync(entityType, entityId, cancellationToken);
        // Limit to count
        return entries.Take(count).Select(MapToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLogResponse>> GetRecentAsync(
        int count = 100,
        CancellationToken cancellationToken = default)
    {
        var entries = await UnitOfWork.AuditLog.GetRecentAsync(count, cancellationToken);
        return entries.Select(MapToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLogResponse>> GetLoginHistoryAsync(
        Guid? userId = null,
        int count = 100,
        CancellationToken cancellationToken = default)
    {
        // Use GetLoginsAsync with a date range (e.g., last 90 days)
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-90);

        var entries = await UnitOfWork.AuditLog.GetLoginsAsync(startDate, endDate, null, cancellationToken);

        // Filter by userId if provided
        if (userId.HasValue)
        {
            entries = entries.Where(e => e.UserId == userId.Value).ToList();
        }

        // Limit to count
        return entries.Take(count).Select(MapToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task LogCreateAsync<T>(
        Guid? userId,
        string? username,
        T entity,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var entityType = typeof(T).Name;
        var entityId = GetEntityId(entity);

        await UnitOfWork.AuditLog.LogCreateAsync(
            userId,
            username,
            entityType,
            entityId ?? "unknown",
            entity,
            ipAddress,
            cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogUpdateAsync<T>(
        Guid? userId,
        string? username,
        T oldEntity,
        T newEntity,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var entityType = typeof(T).Name;
        var entityId = GetEntityId(newEntity);

        await UnitOfWork.AuditLog.LogUpdateAsync(
            userId,
            username,
            entityType,
            entityId ?? "unknown",
            oldEntity,
            newEntity,
            ipAddress,
            cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogDeleteAsync<T>(
        Guid? userId,
        string? username,
        T entity,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var entityType = typeof(T).Name;
        var entityId = GetEntityId(entity);

        await UnitOfWork.AuditLog.LogDeleteAsync(
            userId,
            username,
            entityType,
            entityId ?? "unknown",
            entity,
            ipAddress,
            cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogLoginAsync(
        Guid? userId,
        string username,
        bool success,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        await UnitOfWork.AuditLog.LogLoginAsync(
            userId,
            username,
            success,
            ipAddress,
            userAgent,
            cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogActionAsync(
        Guid? userId,
        string? username,
        string action,
        string entityType,
        string entityId,
        object? details = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            Username = username,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = ipAddress,
            // Store details in NewValues since AdditionalData doesn't exist
            NewValues = details != null ? JsonSerializer.Serialize(details) : null
        };

        await UnitOfWork.AuditLog.LogAsync(entry, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogAsync(
        string action,
        string entityType,
        string? entityId,
        Guid? userId,
        string? username,
        string? ipAddress,
        object? oldValues = null,
        object? newValues = null,
        object? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        // Create AuditLogEntry manually since the repository LogAsync only accepts an entry
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            Username = username,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = ipAddress,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null
            // Note: additionalData parameter is ignored since AuditLogEntry doesn't have AdditionalData property
        };

        await UnitOfWork.AuditLog.LogAsync(entry, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    private static string? GetEntityId<T>(T entity)
    {
        if (entity == null) return null;

        var type = typeof(T);

        // Try to get Id property
        var idProp = type.GetProperty("Id");
        if (idProp != null)
        {
            var id = idProp.GetValue(entity);
            return id?.ToString();
        }

        // Try to get JobId property
        var jobIdProp = type.GetProperty("JobId");
        if (jobIdProp != null)
        {
            var id = jobIdProp.GetValue(entity);
            return id?.ToString();
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDistinctActionsAsync(
        CancellationToken cancellationToken = default)
    {
        // Use QueryNoTracking to get distinct actions
        var actions = await Task.FromResult(
            UnitOfWork.AuditLog.QueryNoTracking()
                .Select(a => a.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToList()
        );
        return actions;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDistinctEntityTypesAsync(
        CancellationToken cancellationToken = default)
    {
        // Use QueryNoTracking to get distinct entity types
        var entityTypes = await Task.FromResult(
            UnitOfWork.AuditLog.QueryNoTracking()
                .Select(a => a.EntityType)
                .Distinct()
                .OrderBy(a => a)
                .ToList()
        );
        return entityTypes;
    }

    private static AuditLogResponse MapToResponse(AuditLogEntry entry)
    {
        Dictionary<string, object>? oldValues = null;
        Dictionary<string, object>? newValues = null;

        if (!string.IsNullOrEmpty(entry.OldValues))
        {
            try
            {
                oldValues = JsonSerializer.Deserialize<Dictionary<string, object>>(entry.OldValues);
            }
            catch { /* Ignore deserialization errors */ }
        }

        if (!string.IsNullOrEmpty(entry.NewValues))
        {
            try
            {
                newValues = JsonSerializer.Deserialize<Dictionary<string, object>>(entry.NewValues);
            }
            catch { /* Ignore deserialization errors */ }
        }

        return new AuditLogResponse
        {
            Id = entry.Id,
            Timestamp = entry.Timestamp,
            UserId = entry.UserId,
            Username = entry.Username,
            Action = entry.Action,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            IpAddress = entry.IpAddress,
            OldValues = oldValues,
            NewValues = newValues
        };
    }
}

/// <summary>
/// Service implementation for maintenance operations.
/// </summary>
public class MaintenanceService : ServiceBase, IMaintenanceService
{
    public MaintenanceService(
        IUnitOfWork unitOfWork,
        ILogger<MaintenanceService> logger)
        : base(unitOfWork, logger)
    {
    }

    /// <inheritdoc />
    public async Task<MaintenanceResult> RunLogCleanupAsync(
        int? retentionDays = null,
        CancellationToken cancellationToken = default)
    {
        var days = retentionDays ?? await UnitOfWork.SystemConfiguration.GetValueAsync("Retention.Logs.Days", 90, cancellationToken);
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        Logger.LogInformation("Starting log cleanup for logs older than {CutoffDate}", cutoffDate);

        var deletedCount = await UnitOfWork.Logs.DeleteOldLogsAsync(cutoffDate, 10000, cancellationToken);

        Logger.LogInformation("Log cleanup completed: {DeletedCount} logs deleted", deletedCount);

        return new MaintenanceResult
        {
            Operation = "LogCleanup",
            Success = true,
            RecordsAffected = deletedCount,
            Message = $"Deleted {deletedCount} logs older than {days} days"
        };
    }

    /// <inheritdoc />
    public async Task<MaintenanceResult> RunExecutionCleanupAsync(
        int? retentionDays = null,
        int keepMinimumPerJob = 100,
        CancellationToken cancellationToken = default)
    {
        var days = retentionDays ?? await UnitOfWork.SystemConfiguration.GetValueAsync("Retention.Executions.Days", 365, cancellationToken);
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        Logger.LogInformation("Starting execution cleanup for executions older than {CutoffDate}", cutoffDate);

        var deletedCount = await UnitOfWork.JobExecutions.DeleteOldExecutionsAsync(cutoffDate, keepMinimumPerJob, cancellationToken);

        Logger.LogInformation("Execution cleanup completed: {DeletedCount} executions deleted", deletedCount);

        return new MaintenanceResult
        {
            Operation = "ExecutionCleanup",
            Success = true,
            RecordsAffected = deletedCount,
            Message = $"Deleted {deletedCount} executions older than {days} days (keeping minimum {keepMinimumPerJob} per job)"
        };
    }

    /// <inheritdoc />
    public async Task<MaintenanceResult> RunAlertCleanupAsync(
        int? retentionDays = null,
        CancellationToken cancellationToken = default)
    {
        var days = retentionDays ?? await UnitOfWork.SystemConfiguration.GetValueAsync("Retention.Alerts.Days", 180, cancellationToken);
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        Logger.LogInformation("Starting alert cleanup for instances older than {CutoffDate}", cutoffDate);

        var deletedCount = await UnitOfWork.AlertInstances.DeleteOldResolvedAsync(cutoffDate, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Alert cleanup completed: {DeletedCount} instances deleted", deletedCount);

        return new MaintenanceResult
        {
            Operation = "AlertCleanup",
            Success = true,
            RecordsAffected = deletedCount,
            Message = $"Deleted {deletedCount} alert instances older than {days} days"
        };
    }

    /// <inheritdoc />
    public async Task<MaintenanceResult> RunAuditLogCleanupAsync(
        int retentionDays = 365,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        Logger.LogInformation("Starting audit log cleanup for entries older than {CutoffDate}", cutoffDate);

        var deletedCount = await UnitOfWork.AuditLog.DeleteOldEntriesAsync(cutoffDate, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Audit log cleanup completed: {DeletedCount} entries deleted", deletedCount);

        return new MaintenanceResult
        {
            Operation = "AuditLogCleanup",
            Success = true,
            RecordsAffected = deletedCount,
            Message = $"Deleted {deletedCount} audit log entries older than {retentionDays} days"
        };
    }

    /// <inheritdoc />
    public async Task<MaintenanceResult> RunTokenCleanupAsync(
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Starting token cleanup");

        var deletedCount = await UnitOfWork.RefreshTokens.DeleteExpiredAsync(cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Token cleanup completed: {DeletedCount} tokens deleted", deletedCount);

        return new MaintenanceResult
        {
            Operation = "TokenCleanup",
            Success = true,
            RecordsAffected = deletedCount,
            Message = $"Deleted {deletedCount} expired/revoked tokens"
        };
    }

    /// <inheritdoc />
    public async Task<MaintenanceResult> RunServerHealthCheckAsync(
        CancellationToken cancellationToken = default)
    {
        var timeoutSeconds = await UnitOfWork.SystemConfiguration.GetValueAsync("Server.HeartbeatTimeout.Seconds", 300, cancellationToken);

        Logger.LogInformation("Starting server health check with {TimeoutSeconds}s timeout", timeoutSeconds);

        var offlineCount = await UnitOfWork.Servers.MarkOfflineAsync(timeoutSeconds, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Server health check completed: {OfflineCount} servers marked offline", offlineCount);

        return new MaintenanceResult
        {
            Operation = "ServerHealthCheck",
            Success = true,
            RecordsAffected = offlineCount,
            Message = $"Marked {offlineCount} servers as offline"
        };
    }

    /// <inheritdoc />
    public async Task<MaintenanceResult> RunExecutionTimeoutCheckAsync(
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Starting execution timeout check");

        var timedOutCount = await UnitOfWork.JobExecutions.TimeoutStuckExecutionsAsync(cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Execution timeout check completed: {TimedOutCount} executions timed out", timedOutCount);

        return new MaintenanceResult
        {
            Operation = "ExecutionTimeoutCheck",
            Success = true,
            RecordsAffected = timedOutCount,
            Message = $"Timed out {timedOutCount} stuck executions"
        };
    }

    /// <inheritdoc />
    public async Task<long> PurgeOldLogsAsync(
        int retentionDays,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        Logger.LogInformation("Purging logs older than {CutoffDate} ({RetentionDays} days)", cutoffDate, retentionDays);

        var deletedCount = await UnitOfWork.Logs.DeleteOldLogsAsync(cutoffDate, 10000, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Purged {DeletedCount} old logs", deletedCount);
        return deletedCount;
    }

    /// <inheritdoc />
    public async Task<long> ArchiveOldLogsAsync(
        int retentionDays,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        Logger.LogInformation("Archiving logs older than {CutoffDate} ({RetentionDays} days)", cutoffDate, retentionDays);

        var archivedCount = await UnitOfWork.Logs.ArchiveLogsAsync(cutoffDate, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Archived {ArchivedCount} logs", archivedCount);
        return archivedCount;
    }

    /// <inheritdoc />
    public async Task<int> PurgeOldExecutionsAsync(
        int retentionDays,
        int keepMinimum = 100,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        Logger.LogInformation("Purging executions older than {CutoffDate}, keeping minimum {KeepMinimum} per job",
            cutoffDate, keepMinimum);

        var deletedCount = await UnitOfWork.JobExecutions.DeleteOldExecutionsAsync(cutoffDate, keepMinimum, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Purged {DeletedCount} old executions", deletedCount);
        return deletedCount;
    }

    /// <inheritdoc />
    public async Task<int> TimeoutStuckExecutionsAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Timing out stuck executions");

        var timedOutCount = await UnitOfWork.JobExecutions.TimeoutStuckExecutionsAsync(cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Timed out {TimedOutCount} stuck executions", timedOutCount);
        return timedOutCount;
    }

    /// <inheritdoc />
    public async Task<int> PurgeOldAlertsAsync(
        int retentionDays,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        Logger.LogInformation("Purging alert instances older than {CutoffDate} ({RetentionDays} days)",
            cutoffDate, retentionDays);

        var deletedCount = await UnitOfWork.AlertInstances.DeleteOldResolvedAsync(cutoffDate, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Purged {DeletedCount} old alert instances", deletedCount);
        return deletedCount;
    }

    /// <inheritdoc />
    public async Task<int> PurgeExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Purging expired refresh tokens");

        var deletedCount = await UnitOfWork.RefreshTokens.DeleteExpiredAsync(cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Purged {DeletedCount} expired tokens", deletedCount);
        return deletedCount;
    }

    /// <inheritdoc />
    public async Task<int> PurgeOldApiKeysAsync(
        int retentionDays,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        Logger.LogInformation("Purging API keys revoked before {CutoffDate} ({RetentionDays} days)",
            cutoffDate, retentionDays);

        var deletedCount = await UnitOfWork.ApiKeys.DeleteOldRevokedAsync(cutoffDate, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Purged {DeletedCount} old API keys", deletedCount);
        return deletedCount;
    }

    /// <inheritdoc />
    public async Task<int> PurgeOldAuditLogsAsync(
        int retentionDays,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        Logger.LogInformation("Purging audit logs older than {CutoffDate} ({RetentionDays} days)",
            cutoffDate, retentionDays);

        var deletedCount = await UnitOfWork.AuditLog.DeleteOldEntriesAsync(cutoffDate, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Purged {DeletedCount} old audit log entries", deletedCount);
        return deletedCount;
    }

    /// <inheritdoc />
    public async Task<int> ClearExpiredCacheAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Clearing expired cache entries");

        // This method needs access to cache repository through UnitOfWork
        // For now, return 0 as cache cleanup is typically handled by the cache service itself
        // If there's a DashboardCache repository in UnitOfWork, use it
        Logger.LogWarning("Cache cleanup not implemented - cache service handles expiration automatically");
        return 0;
    }

    /// <inheritdoc />
    public async Task ClearAllCacheAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Clearing all cache");

        // This would need cache service injection or repository access
        Logger.LogWarning("Clear all cache not implemented - requires cache service reference");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<Core.Interfaces.Services.MaintenanceResult> RunDatabaseMaintenanceAsync(
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        Logger.LogInformation("Running database maintenance");

        try
        {
            // Execute database maintenance commands (reindex, update statistics, etc.)
            // This is database-specific and would use raw SQL
            var sql = @"
                -- Update statistics
                EXEC sp_updatestats;

                -- Rebuild fragmented indexes (SQL Server specific)
                DECLARE @TableName NVARCHAR(255);
                DECLARE TableCursor CURSOR FOR
                SELECT name FROM sys.tables WHERE type = 'U';

                OPEN TableCursor;
                FETCH NEXT FROM TableCursor INTO @TableName;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    EXEC('ALTER INDEX ALL ON ' + @TableName + ' REORGANIZE');
                    FETCH NEXT FROM TableCursor INTO @TableName;
                END;

                CLOSE TableCursor;
                DEALLOCATE TableCursor;
            ";

            await UnitOfWork.ExecuteSqlAsync(sql, null, cancellationToken);

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            Logger.LogInformation("Database maintenance completed in {DurationMs}ms", duration);

            return new Core.Interfaces.Services.MaintenanceResult
            {
                Success = true,
                DurationMs = (long)duration,
                Details = new Dictionary<string, object>
                {
                    ["CompletedAt"] = DateTime.UtcNow,
                    ["Operations"] = new[] { "UpdateStatistics", "ReorganizeIndexes" }
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Database maintenance failed");
            return new Core.Interfaces.Services.MaintenanceResult
            {
                Success = false,
                Error = ex.Message,
                DurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    /// <inheritdoc />
    public async Task<DatabaseSizeInfo> GetDatabaseSizeAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Getting database size information");

        try
        {
            // Query database size (SQL Server specific)
            var sql = @"
                SELECT
                    SUM(CAST(FILEPROPERTY(name, 'SpaceUsed') AS BIGINT) * 8192) AS DataSizeBytes,
                    SUM(size * 8192) AS TotalSizeBytes
                FROM sys.database_files
                WHERE type = 0;

                SELECT
                    SUM(size * 8192) AS LogSizeBytes
                FROM sys.database_files
                WHERE type = 1;
            ";

            var sizeInfo = new DatabaseSizeInfo();

            // For simplicity, return a default implementation
            // In production, this would query the actual database
            sizeInfo.TotalSizeBytes = 0;
            sizeInfo.DataSizeBytes = 0;
            sizeInfo.LogSizeBytes = 0;
            sizeInfo.IndexSizeBytes = 0;

            Logger.LogInformation("Database size: {TotalSizeBytes} bytes", sizeInfo.TotalSizeBytes);
            return sizeInfo;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get database size");
            return new DatabaseSizeInfo();
        }
    }

    /// <inheritdoc />
    public async Task<MaintenanceSummary> RunAllMaintenanceAsync(
        CancellationToken cancellationToken = default)
    {
        var summary = new MaintenanceSummary
        {
            StartedAt = DateTime.UtcNow
        };

        Logger.LogInformation("Starting full maintenance run");

        try
        {
            // Get retention settings
            var logRetention = await UnitOfWork.SystemConfiguration.GetValueAsync("Retention.Logs.Days", 90, cancellationToken);
            var executionRetention = await UnitOfWork.SystemConfiguration.GetValueAsync("Retention.Executions.Days", 365, cancellationToken);
            var alertRetention = await UnitOfWork.SystemConfiguration.GetValueAsync("Retention.Alerts.Days", 180, cancellationToken);

            // Run all maintenance tasks
            summary.LogsDeleted = await PurgeOldLogsAsync(logRetention, cancellationToken);
            summary.ExecutionsDeleted = await PurgeOldExecutionsAsync(executionRetention, 100, cancellationToken);
            summary.AlertsDeleted = await PurgeOldAlertsAsync(alertRetention, cancellationToken);
            summary.TokensDeleted = await PurgeExpiredTokensAsync(cancellationToken);
            summary.AuditLogsDeleted = await PurgeOldAuditLogsAsync(365, cancellationToken);

            // Timeout stuck executions
            await TimeoutStuckExecutionsAsync(cancellationToken);

            // Clear expired cache
            summary.CacheEntriesCleared = await ClearExpiredCacheAsync(cancellationToken);

            // Run database maintenance
            var dbMaintenanceResult = await RunDatabaseMaintenanceAsync(cancellationToken);
            summary.DatabaseMaintenanceSuccess = dbMaintenanceResult.Success;
            if (!dbMaintenanceResult.Success && dbMaintenanceResult.Error != null)
            {
                summary.Errors.Add($"Database maintenance: {dbMaintenanceResult.Error}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during full maintenance run");
            summary.Errors.Add(ex.Message);
        }

        summary.CompletedAt = DateTime.UtcNow;
        summary.TotalDurationMs = (long)(summary.CompletedAt - summary.StartedAt).TotalMilliseconds;

        Logger.LogInformation(
            "Maintenance completed: {LogsDeleted} logs, {ExecutionsDeleted} executions, {AlertsDeleted} alerts deleted",
            summary.LogsDeleted, summary.ExecutionsDeleted, summary.AlertsDeleted);

        return summary;
    }
}

/// <summary>
/// Maintenance operation result.
/// </summary>
public class MaintenanceResult
{
    public string Operation { get; set; } = string.Empty;
    public bool Success { get; set; }
    public long RecordsAffected { get; set; }
    public string Message { get; set; } = string.Empty;
}

