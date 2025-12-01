using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.Entities;

namespace FMSLogNexus.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for system configuration operations.
/// </summary>
public interface ISystemConfigurationRepository : IRepository<SystemConfiguration, string>
{
    #region Query Operations

    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    /// <param name="key">Configuration key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration value or null.</returns>
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed configuration value.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="key">Configuration key.</param>
    /// <param name="defaultValue">Default value if not found.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration value.</returns>
    Task<T?> GetValueAsync<T>(
        string key,
        T? defaultValue = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configuration values by category.
    /// </summary>
    /// <param name="category">Category name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration entries in the category.</returns>
    Task<IReadOnlyList<SystemConfiguration>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configuration values grouped by category.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configurations grouped by category.</returns>
    Task<Dictionary<string, IReadOnlyList<SystemConfiguration>>> GetAllGroupedAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets distinct categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Category names.</returns>
    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Update Operations

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    /// <param name="key">Configuration key.</param>
    /// <param name="value">Value to set.</param>
    /// <param name="updatedBy">User making the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetValueAsync(
        string key,
        object? value,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets multiple configuration values.
    /// </summary>
    /// <param name="values">Key-value pairs.</param>
    /// <param name="updatedBy">User making the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetValuesAsync(
        Dictionary<string, object?> values,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a configuration entry.
    /// </summary>
    /// <param name="config">Configuration entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if created, false if updated.</returns>
    Task<bool> UpsertAsync(
        SystemConfiguration config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a configuration entry.
    /// </summary>
    /// <param name="key">Configuration key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted.</returns>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

    #endregion

    #region Caching Support

    /// <summary>
    /// Gets all configuration values for caching.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All configuration key-value pairs.</returns>
    Task<Dictionary<string, string?>> GetAllForCacheAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last update timestamp for cache invalidation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Last update timestamp.</returns>
    Task<DateTime?> GetLastUpdateTimeAsync(CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Repository interface for audit log operations.
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLogEntry, long>
{
    #region Query Operations

    /// <summary>
    /// Gets audit logs for an entity.
    /// </summary>
    /// <param name="entityType">Entity type name.</param>
    /// <param name="entityId">Entity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audit log entries.</returns>
    Task<IReadOnlyList<AuditLogEntry>> GetByEntityAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="startDate">Start date filter.</param>
    /// <param name="endDate">End date filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audit log entries.</returns>
    Task<IReadOnlyList<AuditLogEntry>> GetByUserAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches audit logs with filtering and pagination.
    /// </summary>
    /// <param name="request">Search request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated audit logs.</returns>
    Task<PaginatedResult<AuditLogEntry>> SearchAsync(
        AuditLogSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent audit logs.
    /// </summary>
    /// <param name="count">Number of entries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent audit log entries.</returns>
    Task<IReadOnlyList<AuditLogEntry>> GetRecentAsync(
        int count = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets login audit logs.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="username">Optional username filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login audit entries.</returns>
    Task<IReadOnlyList<AuditLogEntry>> GetLoginsAsync(
        DateTime startDate,
        DateTime endDate,
        string? username = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Write Operations

    /// <summary>
    /// Logs an audit entry.
    /// </summary>
    /// <param name="entry">Audit log entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs multiple audit entries.
    /// </summary>
    /// <param name="entries">Audit log entries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogBatchAsync(
        IEnumerable<AuditLogEntry> entries,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a create action.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="username">Username.</param>
    /// <param name="entityType">Entity type.</param>
    /// <param name="entityId">Entity ID.</param>
    /// <param name="newValues">New values.</param>
    /// <param name="ipAddress">Client IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogCreateAsync(
        Guid? userId,
        string? username,
        string entityType,
        string entityId,
        object? newValues,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an update action.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="username">Username.</param>
    /// <param name="entityType">Entity type.</param>
    /// <param name="entityId">Entity ID.</param>
    /// <param name="oldValues">Old values.</param>
    /// <param name="newValues">New values.</param>
    /// <param name="ipAddress">Client IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogUpdateAsync(
        Guid? userId,
        string? username,
        string entityType,
        string entityId,
        object? oldValues,
        object? newValues,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a delete action.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="username">Username.</param>
    /// <param name="entityType">Entity type.</param>
    /// <param name="entityId">Entity ID.</param>
    /// <param name="oldValues">Old values.</param>
    /// <param name="ipAddress">Client IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogDeleteAsync(
        Guid? userId,
        string? username,
        string entityType,
        string entityId,
        object? oldValues,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a login action.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="username">Username.</param>
    /// <param name="success">Whether login was successful.</param>
    /// <param name="ipAddress">Client IP address.</param>
    /// <param name="userAgent">Client user agent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogLoginAsync(
        Guid? userId,
        string username,
        bool success,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Maintenance

    /// <summary>
    /// Deletes old audit log entries.
    /// </summary>
    /// <param name="olderThan">Delete entries before this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of entries deleted.</returns>
    Task<int> DeleteOldEntriesAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives old audit log entries.
    /// </summary>
    /// <param name="olderThan">Archive entries before this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of entries archived.</returns>
    Task<int> ArchiveOldEntriesAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Audit log search request.
/// </summary>
public class AuditLogSearchRequest : PaginationRequest
{
    /// <summary>
    /// Filter by user ID.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Filter by username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Filter by action (Create, Update, Delete, Login).
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Filter by entity type.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Filter by entity ID.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Start date filter.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date filter.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by IP address.
    /// </summary>
    public string? IpAddress { get; set; }
}

/// <summary>
/// Repository interface for dashboard cache operations.
/// </summary>
public interface IDashboardCacheRepository : IRepository<DashboardCacheEntry, string>
{
    /// <summary>
    /// Gets a cached value.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached value or null if expired/missing.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sets a cached value.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Value to cache.</param>
    /// <param name="ttlSeconds">Time to live in seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync<T>(
        string key,
        T value,
        int ttlSeconds = 60,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Gets a cached value or computes and caches it.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Factory to compute value if not cached.</param>
    /// <param name="ttlSeconds">Time to live in seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached or computed value.</returns>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        int ttlSeconds = 60,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Invalidates a cached value.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cached values matching a pattern.
    /// </summary>
    /// <param name="pattern">Key pattern (SQL LIKE syntax).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cached values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired cache entries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of entries deleted.</returns>
    Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default);
}
