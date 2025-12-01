using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Interfaces.Services;

/// <summary>
/// Service interface for system health checks.
/// </summary>
public interface IHealthService
{
    /// <summary>
    /// Gets overall system health.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>System health response.</returns>
    Task<SystemHealthResponse> GetHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks database connectivity.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health check result.</returns>
    Task<HealthCheckResult> CheckDatabaseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks external service connectivity (email, webhooks).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health check results.</returns>
    Task<IReadOnlyList<HealthCheckResult>> CheckExternalServicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets system resource usage (CPU, memory, disk).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resource usage stats.</returns>
    Task<SystemResourceStats> GetResourceStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets liveness status (basic health).
    /// </summary>
    /// <returns>True if alive.</returns>
    bool IsAlive();

    /// <summary>
    /// Gets readiness status (ready to accept traffic).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if ready.</returns>
    Task<bool> IsReadyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// System resource statistics.
/// </summary>
public class SystemResourceStats
{
    public decimal CpuUsagePercent { get; set; }
    public long MemoryUsedBytes { get; set; }
    public long MemoryTotalBytes { get; set; }
    public decimal MemoryUsagePercent { get; set; }
    public long DiskUsedBytes { get; set; }
    public long DiskTotalBytes { get; set; }
    public decimal DiskUsagePercent { get; set; }
    public int ThreadCount { get; set; }
    public TimeSpan Uptime { get; set; }
}

/// <summary>
/// Service interface for maintenance operations.
/// </summary>
public interface IMaintenanceService
{
    #region Log Maintenance

    /// <summary>
    /// Deletes old log entries.
    /// </summary>
    /// <param name="retentionDays">Keep logs newer than this many days.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of logs deleted.</returns>
    Task<long> PurgeOldLogsAsync(
        int retentionDays,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives old log entries.
    /// </summary>
    /// <param name="retentionDays">Archive logs older than this many days.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of logs archived.</returns>
    Task<long> ArchiveOldLogsAsync(
        int retentionDays,
        CancellationToken cancellationToken = default);

    #endregion

    #region Execution Maintenance

    /// <summary>
    /// Deletes old job executions.
    /// </summary>
    /// <param name="retentionDays">Keep executions newer than this many days.</param>
    /// <param name="keepMinimum">Minimum executions to keep per job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of executions deleted.</returns>
    Task<int> PurgeOldExecutionsAsync(
        int retentionDays,
        int keepMinimum = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Times out stuck executions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of executions timed out.</returns>
    Task<int> TimeoutStuckExecutionsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Alert Maintenance

    /// <summary>
    /// Deletes old resolved alert instances.
    /// </summary>
    /// <param name="retentionDays">Keep alerts newer than this many days.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of alerts deleted.</returns>
    Task<int> PurgeOldAlertsAsync(
        int retentionDays,
        CancellationToken cancellationToken = default);

    #endregion

    #region Token Maintenance

    /// <summary>
    /// Deletes expired refresh tokens.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of tokens deleted.</returns>
    Task<int> PurgeExpiredTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old revoked API keys.
    /// </summary>
    /// <param name="retentionDays">Keep keys revoked within this many days.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of keys deleted.</returns>
    Task<int> PurgeOldApiKeysAsync(
        int retentionDays,
        CancellationToken cancellationToken = default);

    #endregion

    #region Audit Log Maintenance

    /// <summary>
    /// Deletes old audit log entries.
    /// </summary>
    /// <param name="retentionDays">Keep entries newer than this many days.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of entries deleted.</returns>
    Task<int> PurgeOldAuditLogsAsync(
        int retentionDays,
        CancellationToken cancellationToken = default);

    #endregion

    #region Cache Maintenance

    /// <summary>
    /// Clears expired cache entries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of entries cleared.</returns>
    Task<int> ClearExpiredCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cache.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearAllCacheAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Database Maintenance

    /// <summary>
    /// Runs database maintenance (reindex, update stats).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Maintenance result.</returns>
    Task<MaintenanceResult> RunDatabaseMaintenanceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets database size information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Database size info.</returns>
    Task<DatabaseSizeInfo> GetDatabaseSizeAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Full Maintenance

    /// <summary>
    /// Runs all maintenance tasks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Maintenance summary.</returns>
    Task<MaintenanceSummary> RunAllMaintenanceAsync(CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Maintenance operation result.
/// </summary>
public class MaintenanceResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public long DurationMs { get; set; }
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// Database size information.
/// </summary>
public class DatabaseSizeInfo
{
    public long TotalSizeBytes { get; set; }
    public long DataSizeBytes { get; set; }
    public long LogSizeBytes { get; set; }
    public long IndexSizeBytes { get; set; }
    public Dictionary<string, long> TableSizes { get; set; } = new();
}

/// <summary>
/// Maintenance summary.
/// </summary>
public class MaintenanceSummary
{
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public long TotalDurationMs { get; set; }
    public long LogsDeleted { get; set; }
    public int ExecutionsDeleted { get; set; }
    public int AlertsDeleted { get; set; }
    public int TokensDeleted { get; set; }
    public int AuditLogsDeleted { get; set; }
    public int CacheEntriesCleared { get; set; }
    public bool DatabaseMaintenanceSuccess { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Service interface for system configuration.
/// </summary>
public interface IConfigurationService
{
    #region Get Configuration

    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    /// <param name="key">Configuration key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration value.</returns>
    Task<string?> GetValueAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed configuration value.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="key">Configuration key.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration value.</returns>
    Task<T?> GetValueAsync<T>(
        string key,
        T? defaultValue = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configuration values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration response.</returns>
    Task<ConfigurationResponse> GetAllConfigurationAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configuration values by category.
    /// </summary>
    /// <param name="category">Category name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Category configuration.</returns>
    Task<Dictionary<string, ConfigurationItem>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default);

    #endregion

    #region Set Configuration

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    /// <param name="key">Configuration key.</param>
    /// <param name="value">Value to set.</param>
    /// <param name="username">User making the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetValueAsync(
        string key,
        object? value,
        string? username = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a configuration value (overload with userId).
    /// </summary>
    /// <param name="key">Configuration key.</param>
    /// <param name="value">Value to set.</param>
    /// <param name="userId">User ID making the change.</param>
    /// <param name="username">Username making the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, false if key not found.</returns>
    Task<bool> SetValueAsync(
        string key,
        string value,
        Guid? userId = null,
        string? username = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets multiple configuration values.
    /// </summary>
    /// <param name="request">Update configuration request.</param>
    /// <param name="username">User making the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateConfigurationAsync(
        UpdateConfigurationRequest request,
        string? username = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Extended Operations

    /// <summary>
    /// Gets all configuration items.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All configuration items.</returns>
    Task<IReadOnlyList<ConfigurationItemResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets distinct configuration categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Category names.</returns>
    Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a configuration item.
    /// </summary>
    /// <param name="request">Configuration request.</param>
    /// <param name="userId">User ID making the change.</param>
    /// <param name="username">Username making the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration item response.</returns>
    Task<ConfigurationItemResponse> CreateOrUpdateAsync(
        CreateConfigurationRequest request,
        Guid? userId = null,
        string? username = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Standard Settings

    /// <summary>
    /// Gets log retention days.
    /// </summary>
    Task<int> GetLogRetentionDaysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets execution retention days.
    /// </summary>
    Task<int> GetExecutionRetentionDaysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alert retention days.
    /// </summary>
    Task<int> GetAlertRetentionDaysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets server heartbeat timeout seconds.
    /// </summary>
    Task<int> GetHeartbeatTimeoutSecondsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets maximum login attempts before lockout.
    /// </summary>
    Task<int> GetMaxLoginAttemptsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets lockout duration in minutes.
    /// </summary>
    Task<int> GetLockoutMinutesAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Cache

    /// <summary>
    /// Refreshes configuration cache.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshCacheAsync(CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Service interface for audit logging.
/// </summary>
public interface IAuditService
{
    #region Query Operations

    /// <summary>
    /// Searches audit logs with filtering and pagination.
    /// </summary>
    Task<PaginatedResult<AuditLogResponse>> SearchAsync(
        AuditSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a user.
    /// </summary>
    Task<IReadOnlyList<AuditLogResponse>> GetByUserAsync(
        Guid userId,
        int count = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for an entity.
    /// </summary>
    Task<IReadOnlyList<AuditLogResponse>> GetByEntityAsync(
        string entityType,
        string entityId,
        int count = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent audit logs.
    /// </summary>
    Task<IReadOnlyList<AuditLogResponse>> GetRecentAsync(
        int count = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets login history.
    /// </summary>
    Task<IReadOnlyList<AuditLogResponse>> GetLoginHistoryAsync(
        Guid? userId = null,
        int count = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets distinct actions.
    /// </summary>
    Task<IReadOnlyList<string>> GetDistinctActionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets distinct entity types.
    /// </summary>
    Task<IReadOnlyList<string>> GetDistinctEntityTypesAsync(
        CancellationToken cancellationToken = default);

    #endregion

    #region Logging Operations

    /// <summary>
    /// Logs a create action.
    /// </summary>
    Task LogCreateAsync<T>(
        Guid? userId,
        string? username,
        T entity,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an update action.
    /// </summary>
    Task LogUpdateAsync<T>(
        Guid? userId,
        string? username,
        T oldEntity,
        T newEntity,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a delete action.
    /// </summary>
    Task LogDeleteAsync<T>(
        Guid? userId,
        string? username,
        T entity,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a login attempt.
    /// </summary>
    Task LogLoginAsync(
        Guid? userId,
        string username,
        bool success,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a custom action.
    /// </summary>
    Task LogActionAsync(
        Guid? userId,
        string? username,
        string action,
        string entityType,
        string entityId,
        object? details = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    #endregion
}
