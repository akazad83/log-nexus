namespace FMSLogNexus.Core.Entities;

/// <summary>
/// Represents a JWT refresh token.
/// Maps to [auth].[RefreshTokens] table.
/// </summary>
public class RefreshToken : EntityBase
{
    /// <summary>
    /// The user this token belongs to.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Hashed token value (SHA256).
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// UTC timestamp when the token was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address from which the token was created.
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// User agent that created the token.
    /// </summary>
    public string? CreatedByUserAgent { get; set; }

    /// <summary>
    /// UTC timestamp when the token was revoked.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// IP address from which the token was revoked.
    /// </summary>
    public string? RevokedByIp { get; set; }

    /// <summary>
    /// Reason for revocation.
    /// </summary>
    public string? RevokedReason { get; set; }

    /// <summary>
    /// ID of the token that replaced this one (when refreshed).
    /// </summary>
    public Guid? ReplacedByTokenId { get; set; }

    // -------------------------------------------------------------------------
    // Navigation Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// The user this token belongs to.
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// The token that replaced this one.
    /// </summary>
    public virtual RefreshToken? ReplacedByToken { get; set; }

    // -------------------------------------------------------------------------
    // Computed Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// Indicates if the token is expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Indicates if the token has been revoked.
    /// </summary>
    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>
    /// Indicates if the token is valid (not expired or revoked).
    /// </summary>
    public bool IsActive => !IsRevoked && !IsExpired;

    /// <summary>
    /// Seconds until expiration (negative if expired).
    /// </summary>
    public int SecondsUntilExpiration => (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds;

    /// <summary>
    /// Age of the token in seconds.
    /// </summary>
    public int AgeInSeconds => (int)(DateTime.UtcNow - CreatedAt).TotalSeconds;

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Revokes the token.
    /// </summary>
    public void Revoke(string? ipAddress = null, string? reason = null, Guid? replacedByTokenId = null)
    {
        RevokedAt = DateTime.UtcNow;
        RevokedByIp = ipAddress;
        RevokedReason = reason;
        ReplacedByTokenId = replacedByTokenId;
    }

    /// <summary>
    /// Creates a new refresh token for the same user.
    /// </summary>
    public RefreshToken CreateReplacement(string tokenHash, int expirationDays, string? ipAddress = null, string? userAgent = null)
    {
        var replacement = new RefreshToken
        {
            UserId = UserId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            CreatedByUserAgent = userAgent
        };

        // Revoke this token
        Revoke(ipAddress, "Replaced by new token", replacement.Id);

        return replacement;
    }

    /// <summary>
    /// Creates a new refresh token.
    /// </summary>
    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        int expirationDays,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            CreatedByUserAgent = userAgent
        };
    }
}
