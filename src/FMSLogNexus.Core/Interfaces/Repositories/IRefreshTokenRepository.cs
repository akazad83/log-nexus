using FMSLogNexus.Core.Entities;

namespace FMSLogNexus.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for refresh token operations.
/// </summary>
public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    #region Query Operations

    /// <summary>
    /// Gets a refresh token by its hash.
    /// </summary>
    /// <param name="tokenHash">SHA256 hash of the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Refresh token or null.</returns>
    Task<RefreshToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a refresh token with its user.
    /// </summary>
    /// <param name="tokenHash">SHA256 hash of the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Refresh token with user.</returns>
    Task<RefreshToken?> GetWithUserAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active refresh tokens for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active refresh tokens.</returns>
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all refresh tokens for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="includeRevoked">Include revoked tokens.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User's refresh tokens.</returns>
    Task<IReadOnlyList<RefreshToken>> GetByUserAsync(
        Guid userId,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the token chain (token and its replacements).
    /// </summary>
    /// <param name="tokenId">Starting token ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token chain.</returns>
    Task<IReadOnlyList<RefreshToken>> GetTokenChainAsync(
        Guid tokenId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Validation Operations

    /// <summary>
    /// Validates a refresh token and returns the user.
    /// </summary>
    /// <param name="tokenHash">SHA256 hash of the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User if token is valid, null otherwise.</returns>
    Task<User?> ValidateTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a token is valid (not expired, not revoked).
    /// </summary>
    /// <param name="tokenHash">SHA256 hash of the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if valid.</returns>
    Task<bool> IsValidAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    #endregion

    #region Token Operations

    /// <summary>
    /// Creates a new refresh token.
    /// </summary>
    /// <param name="token">Refresh token to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created token.</returns>
    Task<RefreshToken> CreateAsync(
        RefreshToken token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates a refresh token (revokes old, creates new).
    /// </summary>
    /// <param name="oldTokenHash">Hash of the old token.</param>
    /// <param name="newToken">New token to create.</param>
    /// <param name="revokedByIp">IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New refresh token.</returns>
    Task<RefreshToken?> RotateAsync(
        string oldTokenHash,
        RefreshToken newToken,
        string? revokedByIp,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token.
    /// </summary>
    /// <param name="tokenHash">SHA256 hash of the token.</param>
    /// <param name="revokedByIp">IP address.</param>
    /// <param name="reason">Revocation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if found and revoked.</returns>
    Task<bool> RevokeAsync(
        string tokenHash,
        string? revokedByIp,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="revokedByIp">IP address.</param>
    /// <param name="reason">Revocation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of tokens revoked.</returns>
    Task<int> RevokeAllForUserAsync(
        Guid userId,
        string? revokedByIp,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes token chain (token and all descendants).
    /// </summary>
    /// <param name="tokenId">Starting token ID.</param>
    /// <param name="revokedByIp">IP address.</param>
    /// <param name="reason">Revocation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of tokens revoked.</returns>
    Task<int> RevokeChainAsync(
        Guid tokenId,
        string? revokedByIp,
        string? reason = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Maintenance

    /// <summary>
    /// Deletes expired tokens.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of tokens deleted.</returns>
    Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old revoked tokens.
    /// </summary>
    /// <param name="revokedBefore">Delete tokens revoked before this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of tokens deleted.</returns>
    Task<int> DeleteOldRevokedAsync(
        DateTime revokedBefore,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of active tokens for a user (for session limiting).
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active token count.</returns>
    Task<int> GetActiveCountForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enforces maximum sessions per user by revoking oldest tokens.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="maxSessions">Maximum allowed sessions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of tokens revoked.</returns>
    Task<int> EnforceMaxSessionsAsync(
        Guid userId,
        int maxSessions,
        CancellationToken cancellationToken = default);

    #endregion
}
