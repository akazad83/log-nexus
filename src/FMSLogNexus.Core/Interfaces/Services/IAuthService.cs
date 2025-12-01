using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Interfaces.Services;

/// <summary>
/// Service interface for authentication operations.
/// Handles login, token management, and session management.
/// </summary>
public interface IAuthService
{
    #region Authentication

    /// <summary>
    /// Authenticates a user with username and password.
    /// </summary>
    /// <param name="request">Login request.</param>
    /// <param name="ipAddress">Client IP address.</param>
    /// <param name="userAgent">Client user agent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login response with tokens.</returns>
    Task<AuthResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    /// <param name="request">Refresh token request.</param>
    /// <param name="ipAddress">Client IP address.</param>
    /// <param name="userAgent">Client user agent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New tokens.</returns>
    Task<AuthResult<RefreshTokenResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out by revoking the refresh token.
    /// </summary>
    /// <param name="refreshToken">Refresh token to revoke.</param>
    /// <param name="ipAddress">Client IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogoutAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out from all sessions.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="ipAddress">Client IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogoutAllSessionsAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    #endregion

    #region Password Management

    /// <summary>
    /// Changes a user's password.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Change password request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if changed.</returns>
    Task<bool> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a user's password (admin only).
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="newPassword">New password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if reset.</returns>
    Task<bool> ResetPasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken cancellationToken = default);

    #endregion

    #region API Key Authentication

    /// <summary>
    /// Authenticates using an API key.
    /// </summary>
    /// <param name="apiKey">API key.</param>
    /// <param name="ipAddress">Client IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authenticated user or null.</returns>
    Task<ApiKeyAuthResult?> AuthenticateApiKeyAsync(
        string apiKey,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    #endregion

    #region Validation

    /// <summary>
    /// Validates a JWT access token.
    /// </summary>
    /// <param name="token">JWT token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token validation result.</returns>
    Task<TokenValidationResult> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current user from a valid token.
    /// </summary>
    /// <param name="userId">User ID from token claims.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User response.</returns>
    Task<UserResponse?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Service interface for user management.
/// </summary>
public interface IUserService
{
    #region User Management

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="request">User creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created user response.</returns>
    Task<UserResponse> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User response or null.</returns>
    Task<UserResponse?> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username.
    /// </summary>
    /// <param name="username">Username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User response or null.</returns>
    Task<UserResponse?> GetUserByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated user response.</returns>
    Task<UserResponse?> UpdateUserAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user (soft delete).
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted.</returns>
    Task<bool> DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches users with filtering and pagination.
    /// </summary>
    /// <param name="search">Search text.</param>
    /// <param name="role">Role filter.</param>
    /// <param name="isActive">Active filter.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated user list.</returns>
    Task<PaginatedResult<UserResponse>> SearchUsersAsync(
        string? search,
        UserRole? role,
        bool? isActive,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    #endregion

    #region User Actions

    /// <summary>
    /// Activates a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if activated.</returns>
    Task<bool> ActivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deactivated.</returns>
    Task<bool> DeactivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks a user account.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if unlocked.</returns>
    Task<bool> UnlockUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Preferences

    /// <summary>
    /// Gets user preferences.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User preferences.</returns>
    Task<UserPreferencesResponse?> GetPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user preferences.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Update preferences request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated preferences.</returns>
    Task<UserPreferencesResponse?> UpdatePreferencesAsync(
        Guid userId,
        UpdatePreferencesRequest request,
        CancellationToken cancellationToken = default);

    #endregion

    #region Validation

    /// <summary>
    /// Checks if a username is available.
    /// </summary>
    /// <param name="username">Username to check.</param>
    /// <param name="excludeUserId">User ID to exclude.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if available.</returns>
    Task<bool> IsUsernameAvailableAsync(
        string username,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email is available.
    /// </summary>
    /// <param name="email">Email to check.</param>
    /// <param name="excludeUserId">User ID to exclude.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if available.</returns>
    Task<bool> IsEmailAvailableAsync(
        string email,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Service interface for API key management.
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Creates a new API key.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">API key creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created API key (with full key shown once).</returns>
    Task<ApiKeyResponse> CreateApiKeyAsync(
        Guid userId,
        CreateApiKeyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an API key by ID.
    /// </summary>
    /// <param name="keyId">API key ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>API key response (without full key).</returns>
    Task<ApiKeyListItem?> GetApiKeyAsync(
        Guid keyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all API keys for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="includeRevoked">Include revoked keys.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>API key list.</returns>
    Task<IReadOnlyList<ApiKeyListItem>> GetUserApiKeysAsync(
        Guid userId,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes an API key.
    /// </summary>
    /// <param name="keyId">API key ID.</param>
    /// <param name="userId">User ID (for authorization).</param>
    /// <param name="reason">Revocation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if revoked.</returns>
    Task<bool> RevokeApiKeyAsync(
        Guid keyId,
        Guid userId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all API keys for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="reason">Revocation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of keys revoked.</returns>
    Task<int> RevokeAllUserApiKeysAsync(
        Guid userId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expiring API keys for notification.
    /// </summary>
    /// <param name="withinDays">Days until expiration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Expiring API keys.</returns>
    Task<IReadOnlyList<ApiKeyListItem>> GetExpiringApiKeysAsync(
        int withinDays = 30,
        CancellationToken cancellationToken = default);
}

#region Auth Result Types

/// <summary>
/// Authentication result wrapper.
/// </summary>
/// <typeparam name="T">Result type.</typeparam>
public class AuthResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static AuthResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static AuthResult<T> Fail(string code, string message) => new() { Success = false, ErrorCode = code, ErrorMessage = message };
}

/// <summary>
/// API key authentication result.
/// </summary>
public class ApiKeyAuthResult
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid ApiKeyId { get; set; }
    public string ApiKeyName { get; set; } = string.Empty;
    public string? ServerName { get; set; }
    public IReadOnlyList<string> Scopes { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Token validation result.
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public UserRole? Role { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static TokenValidationResult Valid(Guid userId, string username, UserRole role) => new()
    {
        IsValid = true,
        UserId = userId,
        Username = username,
        Role = role
    };

    public static TokenValidationResult Invalid(string code, string message) => new()
    {
        IsValid = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}

#endregion
