using System.ComponentModel.DataAnnotations;

namespace FMSLogNexus.Core.DTOs.Requests;

/// <summary>
/// Request to login with username and password.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Username.
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Whether to remember the session (extends token lifetime).
    /// </summary>
    public bool RememberMe { get; set; }
}

/// <summary>
/// Request to refresh an access token.
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// The refresh token.
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Request to change password.
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Current password.
    /// </summary>
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password.
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirm new password.
    /// </summary>
    [Required(ErrorMessage = "Confirm password is required")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a new user.
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// Username.
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Username can only contain letters, numbers, dots, underscores, and hyphens")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    [StringLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Initial password.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// User role.
    /// </summary>
    [Required]
    public Enums.UserRole Role { get; set; } = Enums.UserRole.Viewer;
}

/// <summary>
/// Request to update a user.
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// Email address.
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(256)]
    public string? Email { get; set; }

    /// <summary>
    /// Display name.
    /// </summary>
    [StringLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// User role.
    /// </summary>
    public Enums.UserRole? Role { get; set; }

    /// <summary>
    /// Whether the user is active.
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Request to create an API key.
/// </summary>
public class CreateApiKeyRequest
{
    /// <summary>
    /// Name for the API key.
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the key is used for.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Server this key is associated with (optional).
    /// </summary>
    [StringLength(100)]
    public string? ServerName { get; set; }

    /// <summary>
    /// Scopes/permissions for the key.
    /// </summary>
    public List<string>? Scopes { get; set; }

    /// <summary>
    /// Number of days until the key expires (null = never).
    /// </summary>
    [Range(1, 365, ErrorMessage = "Expiration must be between 1 and 365 days")]
    public int? ExpirationDays { get; set; }
}
