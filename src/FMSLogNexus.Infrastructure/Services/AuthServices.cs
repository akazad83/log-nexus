using Microsoft.Extensions.Logging;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Repositories;
using FMSLogNexus.Core.Interfaces.Services;
using FMSLogNexus.Infrastructure.Data.Repositories;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FMSLogNexus.Infrastructure.Services;

/// <summary>
/// Service implementation for authentication operations.
/// </summary>
public class AuthService : ServiceBase, IAuthService
{
    private readonly ITokenService _tokenService;
    private readonly ISystemConfigurationRepository _configRepo;

    public AuthService(
        IUnitOfWork unitOfWork,
        ILogger<AuthService> logger,
        ITokenService tokenService)
        : base(unitOfWork, logger)
    {
        _tokenService = tokenService;
        _configRepo = unitOfWork.SystemConfiguration;
    }

    #region Authentication

    /// <inheritdoc />
    public async Task<AuthResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var user = await UnitOfWork.Users.GetByUsernameAsync(request.Username, cancellationToken);
        if (user == null)
        {
            Logger.LogWarning("Login attempt for non-existent user: {Username}", request.Username);
            return AuthResult<LoginResponse>.Fail(ErrorCodes.InvalidCredentials, "Invalid username or password.");
        }

        // Check if account is locked
        if (user.IsLocked && user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
        {
            Logger.LogWarning("Login attempt for locked account: {Username}", request.Username);
            return AuthResult<LoginResponse>.Fail(ErrorCodes.AccountLocked, "Account is locked. Please try again later.");
        }

        // Check if account is active
        if (!user.IsActive)
        {
            Logger.LogWarning("Login attempt for inactive account: {Username}", request.Username);
            return AuthResult<LoginResponse>.Fail(ErrorCodes.AccountDisabled, "Account is disabled.");
        }

        // Verify password
        if (!VerifyPasswordHash(request.Password, user.PasswordHash))
        {
            await UnitOfWork.Users.RecordFailedLoginAsync(request.Username, cancellationToken: cancellationToken);
            await SaveChangesAsync(cancellationToken);
            Logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
            return AuthResult<LoginResponse>.Fail(ErrorCodes.InvalidCredentials, "Invalid username or password.");
        }

        // Reset failed attempts and record login
        await UnitOfWork.Users.RecordLoginAsync(user.Id, ipAddress, cancellationToken);

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress, userAgent, cancellationToken);

        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("User {Username} logged in successfully", request.Username);

        // Audit log
        await UnitOfWork.AuditLog.LogLoginAsync(user.Id, user.Username, true, ipAddress, userAgent, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        return AuthResult<LoginResponse>.Ok(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = (int)_tokenService.AccessTokenExpiration.TotalSeconds,
            TokenType = "Bearer",
            User = MapUserToResponse(user)
        });
    }

    /// <inheritdoc />
    public async Task<AuthResult<RefreshTokenResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var token = await UnitOfWork.RefreshTokens.GetByHashAsync(tokenHash, cancellationToken);

        if (token == null || token.IsRevoked || token.ExpiresAt <= DateTime.UtcNow)
        {
            Logger.LogWarning("Invalid or expired refresh token used");
            return AuthResult<RefreshTokenResponse>.Fail(ErrorCodes.TokenInvalid, "Invalid or expired refresh token.");
        }

        var user = token.User;
        if (user == null || !user.IsActive)
        {
            return AuthResult<RefreshTokenResponse>.Fail(ErrorCodes.AccountDisabled, "Account is disabled.");
        }

        // Generate new tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = await RotateRefreshTokenAsync(token, user.Id, ipAddress, userAgent, cancellationToken);

        await SaveChangesAsync(cancellationToken);

        Logger.LogDebug("Refreshed tokens for user {Username}", user.Username);

        return AuthResult<RefreshTokenResponse>.Ok(new RefreshTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = (int)_tokenService.AccessTokenExpiration.TotalSeconds
        });
    }

    /// <inheritdoc />
    public async Task LogoutAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var result = await UnitOfWork.RefreshTokens.RevokeAsync(tokenHash, "self", "User logout", cancellationToken);
        if (result)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogDebug("User logged out");
        }
    }

    /// <inheritdoc />
    public async Task LogoutAllSessionsAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var count = await UnitOfWork.RefreshTokens.RevokeAllForUserAsync(userId, ipAddress, "Logout all sessions", cancellationToken);
        if (count > 0)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Logged out all {Count} sessions for user {UserId}", count, userId);
        }
    }

    #endregion

    #region Password Management

    /// <inheritdoc />
    public async Task<bool> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await UnitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return false;

        // Verify current password
        if (!VerifyPasswordHash(request.CurrentPassword, user.PasswordHash))
        {
            Logger.LogWarning("Password change failed for user {UserId}: incorrect current password", userId);
            return false;
        }

        user.PasswordHash = HashPassword(request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.UpdatedAt = DateTime.UtcNow;

        // Revoke all refresh tokens for security
        await UnitOfWork.RefreshTokens.RevokeAllForUserAsync(userId, "system", "Password changed", cancellationToken);

        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Password changed for user {UserId}", userId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ResetPasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await UnitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return false;

        user.PasswordHash = HashPassword(newPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.UpdatedAt = DateTime.UtcNow;

        // Revoke all refresh tokens and API keys
        await UnitOfWork.RefreshTokens.RevokeAllForUserAsync(userId, "admin", "Password reset", cancellationToken);
        await UnitOfWork.ApiKeys.RevokeAllForUserAsync(userId, "admin", "Password reset", cancellationToken);

        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Password reset for user {UserId}", userId);

        return true;
    }

    #endregion

    #region API Key Authentication

    /// <inheritdoc />
    public async Task<ApiKeyAuthResult?> AuthenticateApiKeyAsync(
        string apiKey,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        // API key format: prefix.secret (e.g., "fms_abc123.secretpart")
        var keyHash = HashApiKey(apiKey);
        var key = await UnitOfWork.ApiKeys.GetByHashAsync(keyHash, cancellationToken);

        if (key == null)
        {
            Logger.LogWarning("Invalid API key used from {IpAddress}", ipAddress);
            return null;
        }

        if (key.IsRevoked)
        {
            Logger.LogWarning("Revoked API key used: {KeyId}", key.Id);
            return null;
        }

        if (key.ExpiresAt.HasValue && key.ExpiresAt <= DateTime.UtcNow)
        {
            Logger.LogWarning("Expired API key used: {KeyId}", key.Id);
            return null;
        }

        var user = key.User;
        if (user == null || !user.IsActive)
        {
            Logger.LogWarning("API key for inactive user: {KeyId}", key.Id);
            return null;
        }

        // Record usage
        await UnitOfWork.ApiKeys.RecordUsageAsync(key.Id, ipAddress, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        return new ApiKeyAuthResult
        {
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role,
            ApiKeyId = key.Id,
            ApiKeyName = key.Name,
            ServerName = key.ServerName,
            Scopes = !string.IsNullOrEmpty(key.Scopes)
                ? key.Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                : Array.Empty<string>()
        };
    }

    #endregion

    #region Validation

    /// <inheritdoc />
    public async Task<TokenValidationResult> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var result = _tokenService.ValidateAndExtract(token);
        if (!result.IsValid || !result.UserId.HasValue)
            return result;

        // Verify user still exists and is active
        var user = await UnitOfWork.Users.GetByIdAsync(result.UserId.Value, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return TokenValidationResult.Invalid(ErrorCodes.AccountDisabled, "User account is not valid.");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<UserResponse?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await UnitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        return user == null ? null : MapUserToResponse(user);
    }

    #endregion

    #region Private Methods

    private async Task<string> CreateRefreshTokenAsync(
        Guid userId,
        string? ipAddress,
        string? deviceInfo,
        CancellationToken cancellationToken)
    {
        var token = GenerateSecureToken();
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(token),
            CreatedByIp = ipAddress,
            CreatedByUserAgent = deviceInfo,
            ExpiresAt = DateTime.UtcNow.Add(_tokenService.RefreshTokenExpiration)
        };

        await UnitOfWork.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        return token;
    }

    private async Task<string> RotateRefreshTokenAsync(
        RefreshToken oldToken,
        Guid userId,
        string? ipAddress,
        string? deviceInfo,
        CancellationToken cancellationToken)
    {
        var token = GenerateSecureToken();
        var newToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(token),
            CreatedByIp = ipAddress,
            CreatedByUserAgent = deviceInfo,
            ExpiresAt = DateTime.UtcNow.Add(_tokenService.RefreshTokenExpiration)
        };

        await UnitOfWork.RefreshTokens.RotateAsync(oldToken.TokenHash, newToken, ipAddress, cancellationToken);
        return token;
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPasswordHash(string password, string hash)
    {
        return HashPassword(password) == hash;
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private static string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(bytes);
    }

    private static UserResponse MapUserToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };
    }

    #endregion
}

/// <summary>
/// Service implementation for user management.
/// </summary>
public class UserService : ServiceBase, IUserService
{
    public UserService(
        IUnitOfWork unitOfWork,
        ILogger<UserService> logger)
        : base(unitOfWork, logger)
    {
    }

    #region User Management

    /// <inheritdoc />
    public async Task<UserResponse> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        // Check username availability
        if (await UnitOfWork.Users.UsernameExistsAsync(request.Username, null, cancellationToken))
        {
            throw new InvalidOperationException($"Username '{request.Username}' is already taken.");
        }

        // Check email availability
        if (await UnitOfWork.Users.EmailExistsAsync(request.Email, null, cancellationToken))
        {
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            DisplayName = request.DisplayName,
            PasswordHash = HashPassword(request.Password),
            Role = request.Role,
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        await UnitOfWork.Users.AddAsync(user, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Created user {Username} ({UserId})", user.Username, user.Id);

        return MapUserToResponse(user);
    }

    /// <inheritdoc />
    public async Task<UserResponse?> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await UnitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        return user == null ? null : MapUserToResponse(user);
    }

    /// <inheritdoc />
    public async Task<UserResponse?> GetUserByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        var user = await UnitOfWork.Users.GetByUsernameAsync(username, cancellationToken);
        return user == null ? null : MapUserToResponse(user);
    }

    /// <inheritdoc />
    public async Task<UserResponse?> UpdateUserAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await UnitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return null;

        if (request.Email != null)
        {
            if (await UnitOfWork.Users.EmailExistsAsync(request.Email, userId, cancellationToken))
            {
                throw new InvalidOperationException($"Email '{request.Email}' is already registered.");
            }
            user.Email = request.Email;
        }

        if (request.DisplayName != null) user.DisplayName = request.DisplayName;
        if (request.Role.HasValue) user.Role = request.Role.Value;

        user.UpdatedAt = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Updated user {Username} ({UserId})", user.Username, userId);

        return MapUserToResponse(user);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.Users.DeactivateAsync(userId, cancellationToken);
        if (result)
        {
            // Revoke all tokens and keys
            await UnitOfWork.RefreshTokens.RevokeAllForUserAsync(userId, "system", "User deleted", cancellationToken);
            await UnitOfWork.ApiKeys.RevokeAllForUserAsync(userId, "system", "User deleted", cancellationToken);
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Deactivated user {UserId}", userId);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<UserResponse>> SearchUsersAsync(
        string? search,
        UserRole? role,
        bool? isActive,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.Users.SearchAsync(search, role, isActive, page, pageSize, cancellationToken);

        return PaginatedResult<UserResponse>.Create(
            result.Items.Select(MapUserToResponse),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }

    #endregion

    #region User Actions

    /// <inheritdoc />
    public async Task<bool> ActivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.Users.ActivateAsync(userId, cancellationToken);
        if (result)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Activated user {UserId}", userId);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.Users.DeactivateAsync(userId, cancellationToken);
        if (result)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Deactivated user {UserId}", userId);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> UnlockUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await UnitOfWork.Users.UnlockAsync(userId, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        Logger.LogInformation("Unlocked user {UserId}", userId);
        return true;
    }

    #endregion

    #region Preferences

    /// <inheritdoc />
    public async Task<UserPreferencesResponse?> GetPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await UnitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return null;

        return !string.IsNullOrEmpty(user.Preferences)
            ? JsonSerializer.Deserialize<UserPreferencesResponse>(user.Preferences)
            : new UserPreferencesResponse();
    }

    /// <inheritdoc />
    public async Task<UserPreferencesResponse?> UpdatePreferencesAsync(
        Guid userId,
        UpdatePreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await UnitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return null;

        var preferences = !string.IsNullOrEmpty(user.Preferences)
            ? JsonSerializer.Deserialize<UserPreferencesResponse>(user.Preferences)
            : new UserPreferencesResponse();

        if (preferences == null)
            preferences = new UserPreferencesResponse();

        if (request.Theme != null) preferences.Theme = request.Theme;
        if (request.DefaultPageSize.HasValue) preferences.DefaultPageSize = request.DefaultPageSize.Value;
        if (request.Timezone != null) preferences.Timezone = request.Timezone;
        if (request.RefreshInterval.HasValue) preferences.RefreshInterval = request.RefreshInterval.Value;
        if (request.DefaultLogLevel.HasValue) preferences.DefaultLogLevel = request.DefaultLogLevel.Value.ToString();
        if (request.EmailNotifications.HasValue) preferences.EmailNotifications = request.EmailNotifications.Value;

        user.Preferences = JsonSerializer.Serialize(preferences);
        user.UpdatedAt = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken);

        return preferences;
    }

    #endregion

    #region Validation

    /// <inheritdoc />
    public async Task<bool> IsUsernameAvailableAsync(
        string username,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        return !await UnitOfWork.Users.UsernameExistsAsync(username, excludeUserId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsEmailAvailableAsync(
        string email,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        return !await UnitOfWork.Users.EmailExistsAsync(email, excludeUserId, cancellationToken);
    }

    #endregion

    #region Private Methods

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static UserResponse MapUserToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };
    }

    #endregion
}

/// <summary>
/// Service implementation for API key management.
/// </summary>
public class ApiKeyService : ServiceBase, IApiKeyService
{
    public ApiKeyService(
        IUnitOfWork unitOfWork,
        ILogger<ApiKeyService> logger)
        : base(unitOfWork, logger)
    {
    }

    /// <inheritdoc />
    public async Task<ApiKeyResponse> CreateApiKeyAsync(
        Guid userId,
        CreateApiKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        // Generate API key
        var (key, prefix) = GenerateApiKey();
        var keyHash = HashApiKey(key);

        var apiKey = new UserApiKey
        {
            UserId = userId,
            Name = request.Name,
            KeyHash = keyHash,
            KeyPrefix = prefix,
            ServerName = request.ServerName,
            Scopes = request.Scopes != null ? string.Join(",", request.Scopes) : null,
            ExpiresAt = request.ExpirationDays.HasValue ? DateTime.UtcNow.AddDays(request.ExpirationDays.Value) : null
        };

        await UnitOfWork.ApiKeys.AddAsync(apiKey, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Created API key {KeyName} ({KeyId}) for user {UserId}", request.Name, apiKey.Id, userId);

        return new ApiKeyResponse
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            ApiKey = key, // Only returned once
            KeyPrefix = prefix,
            ServerName = apiKey.ServerName,
            Scopes = apiKey.Scopes?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>(),
            ExpiresAt = apiKey.ExpiresAt,
            CreatedAt = apiKey.CreatedAt
        };
    }

    /// <inheritdoc />
    public async Task<ApiKeyListItem?> GetApiKeyAsync(
        Guid keyId,
        CancellationToken cancellationToken = default)
    {
        var key = await UnitOfWork.ApiKeys.GetByIdAsync(keyId, cancellationToken);
        return key == null ? null : MapToListItem(key);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApiKeyListItem>> GetUserApiKeysAsync(
        Guid userId,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default)
    {
        var keys = await UnitOfWork.ApiKeys.GetByUserAsync(userId, includeRevoked, cancellationToken);
        return keys.Select(MapToListItem).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> RevokeApiKeyAsync(
        Guid keyId,
        Guid userId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        // Verify ownership
        var key = await UnitOfWork.ApiKeys.GetByIdAsync(keyId, cancellationToken);
        if (key == null || key.UserId != userId)
            return false;

        var result = await UnitOfWork.ApiKeys.RevokeAsync(keyId, userId.ToString(), reason, cancellationToken);
        if (result)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Revoked API key {KeyId}", keyId);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<int> RevokeAllUserApiKeysAsync(
        Guid userId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var count = await UnitOfWork.ApiKeys.RevokeAllForUserAsync(userId, "admin", reason, cancellationToken);
        if (count > 0)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Revoked {Count} API keys for user {UserId}", count, userId);
        }
        return count;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApiKeyListItem>> GetExpiringApiKeysAsync(
        int withinDays = 30,
        CancellationToken cancellationToken = default)
    {
        var keys = await UnitOfWork.ApiKeys.GetExpiringAsync(withinDays, cancellationToken);
        return keys.Select(MapToListItem).ToList();
    }

    #region Private Methods

    private static (string Key, string Prefix) GenerateApiKey()
    {
        var prefix = "fms_" + GenerateRandomString(8);
        var secret = GenerateRandomString(32);
        return ($"{prefix}.{secret}", prefix);
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    private static string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(bytes);
    }

    private static ApiKeyListItem MapToListItem(UserApiKey key)
    {
        return new ApiKeyListItem
        {
            Id = key.Id,
            Name = key.Name,
            KeyPrefix = key.KeyPrefix,
            ServerName = key.ServerName,
            Scopes = key.Scopes?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>(),
            IsActive = !key.IsRevoked && (!key.ExpiresAt.HasValue || key.ExpiresAt > DateTime.UtcNow),
            ExpiresAt = key.ExpiresAt,
            LastUsedAt = key.LastUsedAt,
            UsageCount = key.UsageCount,
            CreatedAt = key.CreatedAt
        };
    }

    #endregion
}
