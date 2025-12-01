using Microsoft.EntityFrameworkCore;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Repositories;

namespace FMSLogNexus.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for user operations.
/// </summary>
public class UserRepository : RepositoryBase<User>, IUserRepository
{
    public UserRepository(FMSLogNexusDbContext context) : base(context)
    {
    }

    #region Query Operations

    /// <inheritdoc />
    public async Task<User?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(
            u => u.Username.ToLower() == username.ToLower(),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(
            u => u.Email.ToLower() == email.ToLower(),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(u => u.Role == role && u.IsActive)
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<User>> SearchAsync(
        string? search,
        UserRole? role,
        bool? isActive,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern = $"%{search}%";
            query = query.Where(u =>
                EF.Functions.Like(u.Username, searchPattern) ||
                EF.Functions.Like(u.Email, searchPattern) ||
                (u.DisplayName != null && EF.Functions.Like(u.DisplayName, searchPattern)));
        }

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PaginatedResult<User>.Create(items, totalCount, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<User?> GetWithApiKeysAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.ApiKeys.Where(k => !k.IsRevoked))
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetWithApiKeysAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.ApiKeys.Where(k => !k.IsRevoked))
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetWithRefreshTokensAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.RefreshTokens.Where(t => !t.IsRevoked))
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Validation

    /// <inheritdoc />
    public async Task<bool> IsUsernameAvailableAsync(
        string username,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(u => u.Username.ToLower() == username.ToLower());

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return !await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsEmailAvailableAsync(
        string email,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(u => u.Email.ToLower() == email.ToLower());

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return !await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> UsernameExistsAsync(
        string username,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(u => u.Username.ToLower() == username.ToLower());

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(
        string email,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(u => u.Email.ToLower() == email.ToLower());

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    #endregion

    #region Actions

    /// <inheritdoc />
    public async Task<bool> ActivateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await DbSet.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
            return false;

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await DbSet.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
            return false;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <inheritdoc />
    public async Task UnlockAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await DbSet.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user != null)
        {
            user.Unlock();
        }
    }

    /// <inheritdoc />
    public async Task RecordLoginAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await DbSet.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user != null)
        {
            user.RecordSuccessfulLogin(ipAddress);
        }
    }

    /// <inheritdoc />
    public async Task<bool> RecordFailedLoginAsync(
        string username,
        int maxAttempts = 5,
        int lockoutMinutes = 15,
        CancellationToken cancellationToken = default)
    {
        var user = await DbSet.FirstOrDefaultAsync(
            u => u.Username.ToLower() == username.ToLower(),
            cancellationToken);

        if (user == null)
            return false;

        user.RecordFailedLogin(maxAttempts, lockoutMinutes);

        return user.IsLocked;
    }

    /// <inheritdoc />
    public async Task UpdatePasswordAsync(
        Guid userId,
        string passwordHash,
        CancellationToken cancellationToken = default)
    {
        var user = await DbSet.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user != null)
        {
            user.PasswordHash = passwordHash;
            user.UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <inheritdoc />
    public async Task<string> UpdateSecurityStampAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await DbSet.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user != null)
        {
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.UpdatedAt = DateTime.UtcNow;
            return user.SecurityStamp;
        }

        return string.Empty;
    }

    /// <inheritdoc />
    public async Task UpdatePreferencesAsync(
        Guid userId,
        string preferences,
        CancellationToken cancellationToken = default)
    {
        var user = await DbSet.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user != null)
        {
            user.Preferences = preferences;
            user.UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user?.Preferences;
    }

    #endregion

    #region Statistics

    /// <inheritdoc />
    public async Task<UserStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var users = await DbSet.AsNoTracking().ToListAsync(cancellationToken);

        return new UserStatistics
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.IsActive),
            LockedUsers = users.Count(u => u.IsLocked),
            AdminCount = users.Count(u => u.Role == UserRole.Administrator),
            OperatorCount = users.Count(u => u.Role == UserRole.Operator),
            ViewerCount = users.Count(u => u.Role == UserRole.Viewer),
            ApiUserCount = users.Count(u => u.Role == UserRole.ApiUser)
        };
    }

    #endregion
}

/// <summary>
/// Repository implementation for API key operations.
/// </summary>
public class ApiKeyRepository : RepositoryBase<UserApiKey>, IApiKeyRepository
{
    public ApiKeyRepository(FMSLogNexusDbContext context) : base(context)
    {
    }

    #region Query Operations

    /// <inheritdoc />
    public async Task<UserApiKey?> GetByKeyHashAsync(
        string keyHash,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && !k.IsRevoked, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserApiKey?> GetByHashAsync(
        string keyHash,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && !k.IsRevoked, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserApiKey>> GetByPrefixAsync(
        string keyPrefix,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(k => k.User)
            .Where(k => k.KeyPrefix == keyPrefix && !k.IsRevoked)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserApiKey>> GetByUserAsync(
        Guid userId,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Where(k => k.UserId == userId);

        if (!includeRevoked)
            query = query.Where(k => !k.IsRevoked);

        return await query
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserApiKey>> GetByServerAsync(
        string serverName,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Where(k => k.ServerName == serverName);

        if (!includeRevoked)
            query = query.Where(k => !k.IsRevoked);

        return await query
            .Include(k => k.User)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserApiKey?> GetWithUserAsync(
        Guid keyId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.Id == keyId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserApiKey>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(k => !k.IsRevoked)
            .Where(k => !k.ExpiresAt.HasValue || k.ExpiresAt > DateTime.UtcNow)
            .Include(k => k.User)
            .OrderBy(k => k.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserApiKey>> GetExpiringAsync(
        int withinDays = 30,
        CancellationToken cancellationToken = default)
    {
        var expiryThreshold = DateTime.UtcNow.AddDays(withinDays);

        return await DbSet.AsNoTracking()
            .Where(k => !k.IsRevoked)
            .Where(k => k.ExpiresAt.HasValue && k.ExpiresAt <= expiryThreshold)
            .Include(k => k.User)
            .OrderBy(k => k.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Lifecycle

    /// <inheritdoc />
    public async Task<bool> RevokeAsync(
        Guid keyId,
        string revokedBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var key = await DbSet.FirstOrDefaultAsync(k => k.Id == keyId, cancellationToken);
        if (key == null || key.IsRevoked)
            return false;

        key.RevokedAt = DateTime.UtcNow;
        key.RevokedBy = revokedBy;
        key.RevocationReason = reason;
        return true;
    }

    /// <inheritdoc />
    public async Task<int> RevokeAllForUserAsync(
        Guid userId,
        string revokedBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var keys = await DbSet
            .Where(k => k.UserId == userId && !k.IsRevoked)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var key in keys)
        {
            key.RevokedAt = now;
            key.RevokedBy = revokedBy;
            key.RevocationReason = reason;
        }

        return keys.Count;
    }

    /// <inheritdoc />
    public async Task<bool> ActivateAsync(Guid keyId, CancellationToken cancellationToken = default)
    {
        var key = await DbSet.FirstOrDefaultAsync(k => k.Id == keyId, cancellationToken);
        if (key == null)
            return false;

        // Note: Activation might need additional logic based on your domain model
        // For now, we'll ensure it's not revoked
        if (key.IsRevoked)
            return false;

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateAsync(Guid keyId, CancellationToken cancellationToken = default)
    {
        var key = await DbSet.FirstOrDefaultAsync(k => k.Id == keyId, cancellationToken);
        if (key == null)
            return false;

        // Deactivate by revoking
        if (!key.IsRevoked)
        {
            key.RevokedAt = DateTime.UtcNow;
            key.RevocationReason = "Deactivated";
        }

        return true;
    }

    #endregion

    #region Usage Tracking

    /// <inheritdoc />
    public async Task RecordUsageAsync(
        Guid keyId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var key = await DbSet.FirstOrDefaultAsync(k => k.Id == keyId, cancellationToken);
        if (key == null)
            return;

        key.LastUsedAt = DateTime.UtcNow;
        key.LastUsedIp = ipAddress;
    }

    /// <inheritdoc />
    public async Task<ApiKeyUsageStats> GetUsageStatsAsync(
        Guid keyId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var key = await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == keyId, cancellationToken);

        if (key == null)
        {
            return new ApiKeyUsageStats
            {
                KeyId = keyId,
                TotalUsage = 0,
                FirstUsed = null,
                LastUsed = null,
                UniqueIps = Array.Empty<string>(),
                DailyUsage = Array.Empty<DailyUsage>()
            };
        }

        // Note: This is a basic implementation. For production, you would typically
        // have a separate usage tracking table/entity to store detailed usage data.
        // This implementation uses the limited data available on the UserApiKey entity.
        return new ApiKeyUsageStats
        {
            KeyId = keyId,
            TotalUsage = key.LastUsedAt.HasValue ? 1 : 0,
            FirstUsed = key.CreatedAt,
            LastUsed = key.LastUsedAt,
            UniqueIps = key.LastUsedIp != null ? new[] { key.LastUsedIp } : Array.Empty<string>(),
            DailyUsage = Array.Empty<DailyUsage>()
        };
    }

    #endregion

    #region Maintenance

    /// <inheritdoc />
    public async Task<int> DeleteOldRevokedAsync(
        DateTime revokedBefore,
        CancellationToken cancellationToken = default)
    {
        var oldRevoked = await DbSet
            .Where(k => k.IsRevoked && k.RevokedAt.HasValue && k.RevokedAt < revokedBefore)
            .ToListAsync(cancellationToken);

        DbSet.RemoveRange(oldRevoked);
        return oldRevoked.Count;
    }

    #endregion

    #region Validation

    /// <inheritdoc />
    public async Task<User?> ValidateKeyAsync(
        string keyHash,
        CancellationToken cancellationToken = default)
    {
        var key = await DbSet.AsNoTracking()
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash, cancellationToken);

        if (key == null || key.IsRevoked)
            return null;

        if (key.ExpiresAt.HasValue && key.ExpiresAt <= DateTime.UtcNow)
            return null;

        return key.User;
    }

    /// <inheritdoc />
    public async Task<bool> NameExistsAsync(
        Guid userId,
        string name,
        Guid? excludeKeyId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Where(k => k.UserId == userId && k.Name == name);

        if (excludeKeyId.HasValue)
            query = query.Where(k => k.Id != excludeKeyId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsValidAsync(
        string keyHash,
        CancellationToken cancellationToken = default)
    {
        var key = await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash, cancellationToken);

        if (key == null || key.IsRevoked)
            return false;

        if (key.ExpiresAt.HasValue && key.ExpiresAt <= DateTime.UtcNow)
            return false;

        return true;
    }

    #endregion
}

/// <summary>
/// Repository implementation for refresh token operations.
/// </summary>
public class RefreshTokenRepository : RepositoryBase<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(FMSLogNexusDbContext context) : base(context)
    {
    }

    #region Query Operations

    /// <inheritdoc />
    public async Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> GetWithUserAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RefreshToken>> GetByUserAsync(
        Guid userId,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Where(t => t.UserId == userId);

        if (!includeRevoked)
            query = query.Where(t => !t.IsRevoked);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(t => t.UserId == userId)
            .Where(t => !t.IsRevoked)
            .Where(t => t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RefreshToken>> GetTokenChainAsync(
        Guid tokenId,
        CancellationToken cancellationToken = default)
    {
        var tokens = new List<RefreshToken>();
        var currentToken = await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tokenId, cancellationToken);

        if (currentToken == null)
            return tokens;

        tokens.Add(currentToken);

        // Follow the chain through ReplacedByTokenId
        while (currentToken.ReplacedByTokenId.HasValue)
        {
            currentToken = await DbSet.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == currentToken.ReplacedByTokenId.Value, cancellationToken);

            if (currentToken == null)
                break;

            tokens.Add(currentToken);
        }

        return tokens;
    }

    #endregion

    #region Validation

    /// <inheritdoc />
    public async Task<User?> ValidateTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var token = await DbSet.AsNoTracking()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token == null || token.IsRevoked)
            return null;

        if (token.ExpiresAt <= DateTime.UtcNow)
            return null;

        return token.User;
    }

    /// <inheritdoc />
    public async Task<bool> IsValidAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var token = await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token == null || token.IsRevoked)
            return false;

        if (token.ExpiresAt <= DateTime.UtcNow)
            return false;

        return true;
    }

    #endregion

    #region Token Operations

    /// <inheritdoc />
    public async Task<RefreshToken> CreateAsync(
        RefreshToken token,
        CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(token, cancellationToken);
        return token;
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> RotateAsync(
        string oldTokenHash,
        RefreshToken newToken,
        string? revokedByIp,
        CancellationToken cancellationToken = default)
    {
        var oldToken = await DbSet.FirstOrDefaultAsync(t => t.TokenHash == oldTokenHash, cancellationToken);
        if (oldToken != null)
        {
            oldToken.RevokedAt = DateTime.UtcNow;
            oldToken.RevokedByIp = revokedByIp;
            oldToken.RevokedReason = "Token rotation";
            oldToken.ReplacedByTokenId = newToken.Id;
        }

        await DbSet.AddAsync(newToken, cancellationToken);
        return newToken;
    }

    /// <inheritdoc />
    public async Task<bool> RevokeAsync(
        string tokenHash,
        string? revokedByIp,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var token = await DbSet.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        if (token == null || token.IsRevoked)
            return false;

        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = revokedByIp;
        token.RevokedReason = reason;
        return true;
    }

    /// <inheritdoc />
    public async Task<int> RevokeAllForUserAsync(
        Guid userId,
        string? revokedByIp,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var tokens = await DbSet
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.RevokedAt = now;
            token.RevokedByIp = revokedByIp;
            token.RevokedReason = reason;
        }

        return tokens.Count;
    }

    /// <inheritdoc />
    public async Task<int> RevokeChainAsync(
        Guid tokenId,
        string? revokedByIp,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var chain = await GetTokenChainAsync(tokenId);
        var now = DateTime.UtcNow;
        var revokedCount = 0;

        foreach (var token in chain)
        {
            var tokenToRevoke = await DbSet.FirstOrDefaultAsync(t => t.Id == token.Id);
            if (tokenToRevoke != null && !tokenToRevoke.IsRevoked)
            {
                tokenToRevoke.RevokedAt = now;
                tokenToRevoke.RevokedByIp = revokedByIp;
                tokenToRevoke.RevokedReason = reason ?? "Chain revocation";
                revokedCount++;
            }
        }

        return revokedCount;
    }

    #endregion

    #region Maintenance

    /// <inheritdoc />
    public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expired = await DbSet
            .Where(t => t.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        DbSet.RemoveRange(expired);
        return expired.Count;
    }

    /// <inheritdoc />
    public async Task<int> DeleteOldRevokedAsync(
        DateTime revokedBefore,
        CancellationToken cancellationToken = default)
    {
        var oldRevoked = await DbSet
            .Where(t => t.IsRevoked && t.RevokedAt.HasValue && t.RevokedAt < revokedBefore)
            .ToListAsync(cancellationToken);

        DbSet.RemoveRange(oldRevoked);
        return oldRevoked.Count;
    }

    /// <inheritdoc />
    public async Task<int> GetActiveCountForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(t => t.UserId == userId)
            .Where(t => !t.IsRevoked)
            .Where(t => t.ExpiresAt > DateTime.UtcNow)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> EnforceMaxSessionsAsync(
        Guid userId,
        int maxSessions,
        CancellationToken cancellationToken = default)
    {
        var activeTokens = await DbSet
            .Where(t => t.UserId == userId)
            .Where(t => !t.IsRevoked)
            .Where(t => t.ExpiresAt > DateTime.UtcNow)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var tokensToRevoke = activeTokens.Take(Math.Max(0, activeTokens.Count - maxSessions)).ToList();
        var now = DateTime.UtcNow;

        foreach (var token in tokensToRevoke)
        {
            token.RevokedAt = now;
            token.RevokedReason = "Max sessions enforced";
        }

        return tokensToRevoke.Count;
    }

    #endregion
}

/// <summary>
/// User statistics.
/// </summary>
public class UserStatistics
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int LockedUsers { get; set; }
    public int AdminCount { get; set; }
    public int OperatorCount { get; set; }
    public int ViewerCount { get; set; }
    public int ApiUserCount { get; set; }
}
