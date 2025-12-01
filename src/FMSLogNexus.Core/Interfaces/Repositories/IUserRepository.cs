using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for user operations.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    #region Query Operations

    /// <summary>
    /// Gets a user by username.
    /// </summary>
    /// <param name="username">Username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User or null.</returns>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email.
    /// </summary>
    /// <param name="email">Email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User or null.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username with their API keys.
    /// </summary>
    /// <param name="username">Username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User with API keys.</returns>
    Task<User?> GetWithApiKeysAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username with their refresh tokens.
    /// </summary>
    /// <param name="username">Username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User with refresh tokens.</returns>
    Task<User?> GetWithRefreshTokensAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active users.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active users.</returns>
    Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users by role.
    /// </summary>
    /// <param name="role">User role.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Users with the role.</returns>
    Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated users with search.
    /// </summary>
    /// <param name="search">Search text.</param>
    /// <param name="role">Role filter.</param>
    /// <param name="isActive">Active filter.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated users.</returns>
    Task<PaginatedResult<User>> SearchAsync(
        string? search,
        UserRole? role,
        bool? isActive,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    #endregion

    #region Validation Operations

    /// <summary>
    /// Checks if a username exists.
    /// </summary>
    /// <param name="username">Username to check.</param>
    /// <param name="excludeUserId">User ID to exclude (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists.</returns>
    Task<bool> UsernameExistsAsync(
        string username,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email exists.
    /// </summary>
    /// <param name="email">Email to check.</param>
    /// <param name="excludeUserId">User ID to exclude (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists.</returns>
    Task<bool> EmailExistsAsync(
        string email,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Authentication Operations

    /// <summary>
    /// Records a successful login.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="ipAddress">Client IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordLoginAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a failed login attempt.
    /// </summary>
    /// <param name="username">Username attempted.</param>
    /// <param name="maxAttempts">Max attempts before lockout.</param>
    /// <param name="lockoutMinutes">Lockout duration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if account is now locked.</returns>
    Task<bool> RecordFailedLoginAsync(
        string username,
        int maxAttempts = 5,
        int lockoutMinutes = 15,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks a user account.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UnlockAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user's password hash.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="passwordHash">New password hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdatePasswordAsync(
        Guid userId,
        string passwordHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user's security stamp (invalidates tokens).
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New security stamp.</returns>
    Task<string> UpdateSecurityStampAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Preferences

    /// <summary>
    /// Updates user preferences.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="preferences">Preferences JSON.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdatePreferencesAsync(
        Guid userId,
        string preferences,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user preferences.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Preferences JSON.</returns>
    Task<string?> GetPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Activation

    /// <summary>
    /// Activates a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if found and activated.</returns>
    Task<bool> ActivateAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if found and deactivated.</returns>
    Task<bool> DeactivateAsync(Guid userId, CancellationToken cancellationToken = default);

    #endregion
}
