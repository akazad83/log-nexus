using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.DTOs.Responses;

/// <summary>
/// Response after successful login.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration in seconds.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Token type (always "Bearer").
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// User information.
    /// </summary>
    public UserResponse User { get; set; } = new();
}

/// <summary>
/// Response after token refresh.
/// </summary>
public class RefreshTokenResponse
{
    /// <summary>
    /// New JWT access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// New refresh token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration in seconds.
    /// </summary>
    public int ExpiresIn { get; set; }
}

/// <summary>
/// User information response.
/// </summary>
public class UserResponse
{
    /// <summary>
    /// User ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Effective display name (DisplayName or Username).
    /// </summary>
    public string EffectiveDisplayName => DisplayName ?? Username;

    /// <summary>
    /// User role.
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Role name.
    /// </summary>
    public string RoleName => Role.ToString();

    /// <summary>
    /// Whether the user is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Last login timestamp.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// User preferences.
    /// </summary>
    public UserPreferencesResponse? Preferences { get; set; }

    /// <summary>
    /// Account creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// User preferences response.
/// </summary>
public class UserPreferencesResponse
{
    /// <summary>
    /// UI theme.
    /// </summary>
    public string Theme { get; set; } = "light";

    /// <summary>
    /// Timezone.
    /// </summary>
    public string Timezone { get; set; } = "UTC";

    /// <summary>
    /// Refresh interval in seconds.
    /// </summary>
    public int RefreshInterval { get; set; } = 30;

    /// <summary>
    /// Default page size.
    /// </summary>
    public int DefaultPageSize { get; set; } = 50;

    /// <summary>
    /// Default log level filter.
    /// </summary>
    public string? DefaultLogLevel { get; set; }

    /// <summary>
    /// Email notifications enabled.
    /// </summary>
    public bool EmailNotifications { get; set; } = true;
}

/// <summary>
/// API key response (after creation).
/// </summary>
public class ApiKeyResponse
{
    /// <summary>
    /// API key ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Key name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Key prefix for identification.
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Full API key (only returned on creation, store it securely).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Associated server name.
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// Scopes/permissions.
    /// </summary>
    public List<string> Scopes { get; set; } = new();

    /// <summary>
    /// Whether the key is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Expiration date.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Last used date.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Usage count.
    /// </summary>
    public long UsageCount { get; set; }

    /// <summary>
    /// Creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// API key list item (without full key).
/// </summary>
public class ApiKeyListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string? ServerName { get; set; }
    public List<string> Scopes { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public long UsageCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
