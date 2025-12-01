using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.Entities;

namespace FMSLogNexus.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for API key operations.
/// </summary>
public interface IApiKeyRepository : IRepository<UserApiKey>
{
    #region Query Operations

    /// <summary>
    /// Gets an API key by its hash.
    /// </summary>
    /// <param name="keyHash">SHA256 hash of the key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>API key or null.</returns>
    Task<UserApiKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an API key by its prefix.
    /// </summary>
    /// <param name="keyPrefix">First 8 characters of the key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>API keys matching prefix.</returns>
    Task<IReadOnlyList<UserApiKey>> GetByPrefixAsync(
        string keyPrefix,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets API keys for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="includeRevoked">Include revoked keys.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User's API keys.</returns>
    Task<IReadOnlyList<UserApiKey>> GetByUserAsync(
        Guid userId,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets API keys for a server.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="includeRevoked">Include revoked keys.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server's API keys.</returns>
    Task<IReadOnlyList<UserApiKey>> GetByServerAsync(
        string serverName,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an API key with its user.
    /// </summary>
    /// <param name="keyId">API key ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>API key with user.</returns>
    Task<UserApiKey?> GetWithUserAsync(
        Guid keyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active (valid) API keys.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active API keys.</returns>
    Task<IReadOnlyList<UserApiKey>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expiring API keys.
    /// </summary>
    /// <param name="withinDays">Days until expiration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Expiring API keys.</returns>
    Task<IReadOnlyList<UserApiKey>> GetExpiringAsync(
        int withinDays = 30,
        CancellationToken cancellationToken = default);

    #endregion

    #region Validation Operations

    /// <summary>
    /// Validates an API key and returns the associated user.
    /// </summary>
    /// <param name="keyHash">SHA256 hash of the key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User if key is valid, null otherwise.</returns>
    Task<User?> ValidateKeyAsync(string keyHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key name exists for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="name">Key name.</param>
    /// <param name="excludeKeyId">Key ID to exclude (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if name exists.</returns>
    Task<bool> NameExistsAsync(
        Guid userId,
        string name,
        Guid? excludeKeyId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Usage Tracking

    /// <summary>
    /// Records API key usage.
    /// </summary>
    /// <param name="keyId">API key ID.</param>
    /// <param name="ipAddress">Client IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordUsageAsync(
        Guid keyId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets usage statistics for an API key.
    /// </summary>
    /// <param name="keyId">API key ID.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Usage statistics.</returns>
    Task<ApiKeyUsageStats> GetUsageStatsAsync(
        Guid keyId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    #endregion

    #region Lifecycle Operations

    /// <summary>
    /// Revokes an API key.
    /// </summary>
    /// <param name="keyId">API key ID.</param>
    /// <param name="revokedBy">User revoking the key.</param>
    /// <param name="reason">Revocation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if found and revoked.</returns>
    Task<bool> RevokeAsync(
        Guid keyId,
        string revokedBy,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all API keys for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="revokedBy">User revoking the keys.</param>
    /// <param name="reason">Revocation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of keys revoked.</returns>
    Task<int> RevokeAllForUserAsync(
        Guid userId,
        string revokedBy,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates an API key.
    /// </summary>
    /// <param name="keyId">API key ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if found and activated.</returns>
    Task<bool> ActivateAsync(Guid keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates an API key.
    /// </summary>
    /// <param name="keyId">API key ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if found and deactivated.</returns>
    Task<bool> DeactivateAsync(Guid keyId, CancellationToken cancellationToken = default);

    #endregion

    #region Maintenance

    /// <summary>
    /// Deletes expired revoked keys.
    /// </summary>
    /// <param name="revokedBefore">Delete keys revoked before this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of keys deleted.</returns>
    Task<int> DeleteOldRevokedAsync(
        DateTime revokedBefore,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// API key usage statistics.
/// </summary>
public class ApiKeyUsageStats
{
    public Guid KeyId { get; set; }
    public long TotalUsage { get; set; }
    public DateTime? FirstUsed { get; set; }
    public DateTime? LastUsed { get; set; }
    public IReadOnlyList<string> UniqueIps { get; set; } = Array.Empty<string>();
    public IReadOnlyList<DailyUsage> DailyUsage { get; set; } = Array.Empty<DailyUsage>();
}

/// <summary>
/// Daily usage data point.
/// </summary>
public class DailyUsage
{
    public DateTime Date { get; set; }
    public long Count { get; set; }
}
