using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Entities;

/// <summary>
/// Represents a system user.
/// Maps to [auth].[Users] table.
/// </summary>
public class User : AuditableEntity, IActivatable
{
    /// <summary>
    /// Username for login.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Normalized username for case-insensitive comparison.
    /// </summary>
    public string NormalizedUsername => Username.ToUpperInvariant();

    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Normalized email for case-insensitive comparison.
    /// </summary>
    public string NormalizedEmail => Email.ToUpperInvariant();

    /// <summary>
    /// Display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Hashed password (BCrypt).
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Security stamp (changes when credentials change).
    /// </summary>
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User's role in the system.
    /// </summary>
    public UserRole Role { get; set; } = UserRole.Viewer;

    /// <summary>
    /// Whether the user is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the account is locked.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// UTC timestamp when lockout ends.
    /// </summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// Number of failed login attempts.
    /// </summary>
    public int FailedLoginCount { get; set; }

    /// <summary>
    /// UTC timestamp of the last successful login.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// IP address of the last login.
    /// </summary>
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// User preferences as JSON.
    /// </summary>
    public string? Preferences { get; set; }

    // -------------------------------------------------------------------------
    // Navigation Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// API keys owned by this user.
    /// </summary>
    public virtual ICollection<UserApiKey> ApiKeys { get; set; } = new List<UserApiKey>();

    /// <summary>
    /// Refresh tokens for this user.
    /// </summary>
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    // -------------------------------------------------------------------------
    // Computed Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// Indicates if the account is currently locked out.
    /// </summary>
    public bool IsLockedOut => IsLocked && LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;

    /// <summary>
    /// Indicates if the user can log in.
    /// </summary>
    public bool CanLogin => IsActive && !IsLockedOut;

    /// <summary>
    /// Gets the effective display name.
    /// </summary>
    public string EffectiveDisplayName => DisplayName ?? Username;

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Records a successful login.
    /// </summary>
    public void RecordSuccessfulLogin(string? ipAddress = null)
    {
        LastLoginAt = DateTime.UtcNow;
        LastLoginIp = ipAddress;
        FailedLoginCount = 0;
        IsLocked = false;
        LockoutEnd = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a failed login attempt.
    /// </summary>
    /// <param name="maxAttempts">Maximum failed attempts before lockout.</param>
    /// <param name="lockoutMinutes">Duration of lockout in minutes.</param>
    public void RecordFailedLogin(int maxAttempts = 5, int lockoutMinutes = 15)
    {
        FailedLoginCount++;
        UpdatedAt = DateTime.UtcNow;

        if (FailedLoginCount >= maxAttempts)
        {
            IsLocked = true;
            LockoutEnd = DateTime.UtcNow.AddMinutes(lockoutMinutes);
        }
    }

    /// <summary>
    /// Unlocks the account.
    /// </summary>
    public void Unlock()
    {
        IsLocked = false;
        LockoutEnd = null;
        FailedLoginCount = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Changes the password and regenerates security stamp.
    /// </summary>
    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        SecurityStamp = Guid.NewGuid().ToString();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets user preferences as a typed object.
    /// </summary>
    public UserPreferences GetPreferences()
    {
        if (string.IsNullOrEmpty(Preferences))
            return new UserPreferences();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<UserPreferences>(Preferences)
                ?? new UserPreferences();
        }
        catch
        {
            return new UserPreferences();
        }
    }

    /// <summary>
    /// Sets user preferences.
    /// </summary>
    public void SetPreferences(UserPreferences preferences)
    {
        Preferences = System.Text.Json.JsonSerializer.Serialize(preferences);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if user has the required role or higher.
    /// </summary>
    public bool HasRole(UserRole requiredRole)
    {
        return Role >= requiredRole;
    }

    /// <summary>
    /// Checks if user is an administrator.
    /// </summary>
    public bool IsAdmin => Role == UserRole.Administrator;
}

/// <summary>
/// User preferences.
/// </summary>
public class UserPreferences
{
    /// <summary>
    /// UI theme (light/dark).
    /// </summary>
    public string Theme { get; set; } = "light";

    /// <summary>
    /// User's timezone.
    /// </summary>
    public string Timezone { get; set; } = "UTC";

    /// <summary>
    /// Dashboard auto-refresh interval in seconds.
    /// </summary>
    public int RefreshInterval { get; set; } = 30;

    /// <summary>
    /// Default page size for lists.
    /// </summary>
    public int DefaultPageSize { get; set; } = 50;

    /// <summary>
    /// Default log level filter.
    /// </summary>
    public string? DefaultLogLevel { get; set; }

    /// <summary>
    /// Email notification preferences.
    /// </summary>
    public bool EmailNotifications { get; set; } = true;
}
