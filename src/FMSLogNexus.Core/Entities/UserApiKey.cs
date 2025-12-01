namespace FMSLogNexus.Core.Entities;

/// <summary>
/// Represents an API key for service authentication.
/// Maps to [auth].[UserApiKeys] table.
/// </summary>
public class UserApiKey : AuditableEntity, IActivatable
{
    /// <summary>
    /// The user who owns this API key.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The server this API key is associated with (optional).
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// Friendly name for the API key.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the key is used for.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Hashed API key value (SHA256).
    /// </summary>
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>
    /// First 8 characters of the key for identification.
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of scopes/permissions.
    /// </summary>
    public string? Scopes { get; set; }

    /// <summary>
    /// Whether the key is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// UTC timestamp when the key expires (null = never).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// UTC timestamp when the key was last used.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// IP address from which the key was last used.
    /// </summary>
    public string? LastUsedIp { get; set; }

    /// <summary>
    /// Total number of times the key has been used.
    /// </summary>
    public long UsageCount { get; set; }

    /// <summary>
    /// UTC timestamp when the key was revoked.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Username who revoked the key.
    /// </summary>
    public string? RevokedBy { get; set; }

    /// <summary>
    /// Reason for revocation.
    /// </summary>
    public string? RevocationReason { get; set; }

    // -------------------------------------------------------------------------
    // Navigation Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// The user who owns this API key.
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// The server this key is associated with.
    /// </summary>
    public virtual Server? Server { get; set; }

    // -------------------------------------------------------------------------
    // Computed Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// Indicates if the key is expired.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

    /// <summary>
    /// Indicates if the key has been revoked.
    /// </summary>
    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>
    /// Indicates if the key is valid (active, not expired, not revoked).
    /// </summary>
    public bool IsValid => IsActive && !IsExpired && !IsRevoked;

    /// <summary>
    /// Days until expiration (null if no expiration or already expired).
    /// </summary>
    public int? DaysUntilExpiration
    {
        get
        {
            if (!ExpiresAt.HasValue || IsExpired)
                return null;

            return (int)(ExpiresAt.Value - DateTime.UtcNow).TotalDays;
        }
    }

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Records usage of the API key.
    /// </summary>
    public void RecordUsage(string? ipAddress = null)
    {
        LastUsedAt = DateTime.UtcNow;
        LastUsedIp = ipAddress;
        UsageCount++;
    }

    /// <summary>
    /// Revokes the API key.
    /// </summary>
    public void Revoke(string revokedBy, string? reason = null)
    {
        IsActive = false;
        RevokedAt = DateTime.UtcNow;
        RevokedBy = revokedBy;
        RevocationReason = reason;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = revokedBy;
    }

    /// <summary>
    /// Gets the scopes as a list.
    /// </summary>
    public List<string> GetScopesList()
    {
        if (string.IsNullOrEmpty(Scopes))
            return new List<string>();

        return Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .ToList();
    }

    /// <summary>
    /// Checks if the key has a specific scope.
    /// </summary>
    public bool HasScope(string scope)
    {
        var scopes = GetScopesList();
        return scopes.Contains("*") || scopes.Contains(scope, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sets the scopes from a list.
    /// </summary>
    public void SetScopes(IEnumerable<string> scopes)
    {
        Scopes = string.Join(",", scopes);
    }

    /// <summary>
    /// Generates a masked display version of the key.
    /// </summary>
    public string GetMaskedKey()
    {
        return $"{KeyPrefix}...";
    }
}
