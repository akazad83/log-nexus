using System.Security.Claims;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Interfaces.Services;

/// <summary>
/// Service interface for JWT token operations.
/// </summary>
public interface ITokenService
{
    #region Access Token

    /// <summary>
    /// Generates a JWT access token for a user.
    /// </summary>
    /// <param name="user">User entity.</param>
    /// <returns>JWT access token.</returns>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a JWT access token with custom claims.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="username">Username.</param>
    /// <param name="role">User role.</param>
    /// <param name="additionalClaims">Additional claims.</param>
    /// <returns>JWT access token.</returns>
    string GenerateAccessToken(
        Guid userId,
        string username,
        UserRole role,
        IEnumerable<Claim>? additionalClaims = null);

    /// <summary>
    /// Gets the access token expiration time.
    /// </summary>
    TimeSpan AccessTokenExpiration { get; }

    #endregion

    #region Refresh Token

    /// <summary>
    /// Generates a refresh token.
    /// </summary>
    /// <returns>Random refresh token string.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Gets the refresh token expiration time.
    /// </summary>
    TimeSpan RefreshTokenExpiration { get; }

    /// <summary>
    /// Gets the extended refresh token expiration (remember me).
    /// </summary>
    TimeSpan ExtendedRefreshTokenExpiration { get; }

    #endregion

    #region Validation

    /// <summary>
    /// Validates a JWT access token.
    /// </summary>
    /// <param name="token">JWT token.</param>
    /// <returns>Claims principal if valid, null otherwise.</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Validates a token and extracts claims.
    /// </summary>
    /// <param name="token">JWT token.</param>
    /// <returns>Validation result.</returns>
    TokenValidationResult ValidateAndExtract(string token);

    /// <summary>
    /// Gets the user ID from a token.
    /// </summary>
    /// <param name="token">JWT token.</param>
    /// <returns>User ID or null.</returns>
    Guid? GetUserIdFromToken(string token);

    /// <summary>
    /// Gets the security stamp from a token.
    /// </summary>
    /// <param name="token">JWT token.</param>
    /// <returns>Security stamp or null.</returns>
    string? GetSecurityStampFromToken(string token);

    #endregion

    #region Hashing

    /// <summary>
    /// Hashes a refresh token for storage.
    /// </summary>
    /// <param name="token">Plain token.</param>
    /// <returns>SHA256 hash.</returns>
    string HashToken(string token);

    /// <summary>
    /// Hashes an API key for storage.
    /// </summary>
    /// <param name="apiKey">Plain API key.</param>
    /// <returns>SHA256 hash.</returns>
    string HashApiKey(string apiKey);

    /// <summary>
    /// Generates a new API key.
    /// </summary>
    /// <returns>Random API key string.</returns>
    string GenerateApiKey();

    /// <summary>
    /// Gets the prefix of an API key (for identification).
    /// </summary>
    /// <param name="apiKey">API key.</param>
    /// <returns>First 8 characters.</returns>
    string GetApiKeyPrefix(string apiKey);

    #endregion
}

/// <summary>
/// Service interface for password hashing.
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Hashes a password.
    /// </summary>
    /// <param name="password">Plain text password.</param>
    /// <returns>BCrypt hash.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against a hash.
    /// </summary>
    /// <param name="password">Plain text password.</param>
    /// <param name="hash">BCrypt hash.</param>
    /// <returns>True if matches.</returns>
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// Checks if a password meets complexity requirements.
    /// </summary>
    /// <param name="password">Password to check.</param>
    /// <returns>Validation result.</returns>
    PasswordValidationResult ValidatePassword(string password);
}

/// <summary>
/// Password validation result.
/// </summary>
public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();

    public static PasswordValidationResult Valid() => new() { IsValid = true };
    public static PasswordValidationResult Invalid(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}
