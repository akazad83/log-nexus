namespace FMSLogNexus.Core.Interfaces.Services;

/// <summary>
/// Service interface for caching operations.
/// Provides unified caching abstraction.
/// </summary>
public interface ICacheService
{
    #region Basic Operations

    /// <summary>
    /// Gets a cached value.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached value or default.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a cached value.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Value to cache.</param>
    /// <param name="expiration">Optional expiration time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or sets a cached value.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Factory to create value if not cached.</param>
    /// <param name="expiration">Optional expiration time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached or newly created value.</returns>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a cached value.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes cached values matching a pattern.
    /// </summary>
    /// <param name="pattern">Key pattern (supports *).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in cache.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists.</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Gets multiple cached values.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="keys">Cache keys.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of key-value pairs.</returns>
    Task<Dictionary<string, T?>> GetManyAsync<T>(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets multiple cached values.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="items">Key-value pairs.</param>
    /// <param name="expiration">Optional expiration time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetManyAsync<T>(
        Dictionary<string, T> items,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Cache Management

    /// <summary>
    /// Clears all cached values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cache statistics.</returns>
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Typed Cache Keys

    /// <summary>
    /// Dashboard summary cache key.
    /// </summary>
    string DashboardSummaryKey { get; }

    /// <summary>
    /// Gets job lookup cache key.
    /// </summary>
    string JobLookupKey { get; }

    /// <summary>
    /// Gets server lookup cache key.
    /// </summary>
    string ServerLookupKey { get; }

    /// <summary>
    /// Gets configuration cache key.
    /// </summary>
    string ConfigurationKey { get; }

    #endregion
}

/// <summary>
/// Cache statistics.
/// </summary>
public class CacheStatistics
{
    public long ItemCount { get; set; }
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public decimal HitRatio => HitCount + MissCount > 0
        ? (decimal)HitCount / (HitCount + MissCount) * 100
        : 0;
    public long MemoryUsageBytes { get; set; }
}
