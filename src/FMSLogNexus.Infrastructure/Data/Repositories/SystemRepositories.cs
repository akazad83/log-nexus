using Microsoft.EntityFrameworkCore;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Interfaces.Repositories;
using System.Text.Json;

namespace FMSLogNexus.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for system configuration operations.
/// </summary>
public class SystemConfigurationRepository : RepositoryBase<SystemConfiguration, string>, ISystemConfigurationRepository
{
    public SystemConfigurationRepository(FMSLogNexusDbContext context) : base(context)
    {
    }

    #region Query Operations

    /// <inheritdoc />
    public async Task<string?> GetValueAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var config = await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == key, cancellationToken);

        return config?.Value;
    }

    /// <inheritdoc />
    public async Task<T?> GetValueAsync<T>(
        string key,
        T? defaultValue = default,
        CancellationToken cancellationToken = default)
    {
        var value = await GetValueAsync(key, cancellationToken);

        if (string.IsNullOrEmpty(value))
            return defaultValue;

        try
        {
            if (typeof(T) == typeof(string))
                return (T)(object)value;

            if (typeof(T) == typeof(int))
                return (T)(object)int.Parse(value);

            if (typeof(T) == typeof(long))
                return (T)(object)long.Parse(value);

            if (typeof(T) == typeof(bool))
                return (T)(object)bool.Parse(value);

            if (typeof(T) == typeof(decimal))
                return (T)(object)decimal.Parse(value);

            if (typeof(T) == typeof(double))
                return (T)(object)double.Parse(value);

            // Try JSON deserialization for complex types
            return JsonSerializer.Deserialize<T>(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemConfiguration>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(c => c.Category == category)
            .OrderBy(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, IReadOnlyList<SystemConfiguration>>> GetAllGroupedAsync(
        CancellationToken cancellationToken = default)
    {
        var allConfigs = await DbSet.AsNoTracking()
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

        return allConfigs
            .GroupBy(c => c.Category ?? "Default")
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<SystemConfiguration>)g.ToList()
            );
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(c => c.Category != null)
            .Select(c => c.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Update Operations

    /// <inheritdoc />
    public async Task SetValueAsync(
        string key,
        object? value,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var config = await DbSet.FirstOrDefaultAsync(c => c.Id == key, cancellationToken);

        if (config == null)
            throw new KeyNotFoundException($"Configuration key '{key}' not found.");

        string stringValue;
        if (value == null)
        {
            stringValue = string.Empty;
        }
        else if (value is string str)
        {
            stringValue = str;
        }
        else if (value is int or long or bool or decimal or double)
        {
            stringValue = value.ToString()!;
        }
        else
        {
            stringValue = JsonSerializer.Serialize(value);
        }

        config.Value = stringValue;
        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedBy = updatedBy;
    }

    /// <inheritdoc />
    public async Task SetValuesAsync(
        Dictionary<string, object?> values,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var kvp in values)
        {
            await SetValueAsync(kvp.Key, kvp.Value, updatedBy, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpsertAsync(
        SystemConfiguration config,
        CancellationToken cancellationToken = default)
    {
        var existing = await DbSet.FirstOrDefaultAsync(c => c.Id == config.Id, cancellationToken);

        if (existing == null)
        {
            config.UpdatedAt = DateTime.UtcNow;
            await DbSet.AddAsync(config, cancellationToken);
            return true; // Created
        }
        else
        {
            existing.Value = config.Value;
            existing.Category = config.Category;
            existing.Description = config.Description;
            existing.DataType = config.DataType;
            existing.IsEncrypted = config.IsEncrypted;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = config.UpdatedBy;
            return false; // Updated
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var config = await DbSet.FirstOrDefaultAsync(c => c.Id == key, cancellationToken);

        if (config == null)
            return false;

        DbSet.Remove(config);
        return true;
    }

    #endregion

    #region Caching Support

    /// <inheritdoc />
    public async Task<Dictionary<string, string?>> GetAllForCacheAsync(
        CancellationToken cancellationToken = default)
    {
        var configs = await DbSet.AsNoTracking().ToListAsync(cancellationToken);
        return configs.ToDictionary(c => c.Id, c => c.Value);
    }

    /// <inheritdoc />
    public async Task<DateTime?> GetLastUpdateTimeAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .MaxAsync(c => (DateTime?)c.UpdatedAt, cancellationToken);
    }

    #endregion

    #region Standard Settings

    /// <inheritdoc />
    public async Task<int> GetLogRetentionDaysAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync("LogRetentionDays", 90, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetExecutionRetentionDaysAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync("ExecutionRetentionDays", 365, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetAlertRetentionDaysAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync("AlertRetentionDays", 180, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetHeartbeatTimeoutSecondsAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync("HeartbeatTimeoutSeconds", 300, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetMaxLoginAttemptsAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync("MaxLoginAttempts", 5, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetLockoutMinutesAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync("LockoutMinutes", 15, cancellationToken);
    }

    #endregion
}

/// <summary>
/// Repository implementation for audit log operations.
/// Standalone implementation that does NOT inherit from RepositoryBase.
/// </summary>
public class AuditLogRepository : IAuditLogRepository
{
    private readonly FMSLogNexusDbContext _context;
    private readonly DbSet<AuditLogEntry> _dbSet;

    public AuditLogRepository(FMSLogNexusDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<AuditLogEntry>();
    }

    #region IRepository<AuditLogEntry, long> Implementation

    /// <inheritdoc />
    public async Task<AuditLogEntry?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuditLogEntry?> GetByIdAsync(long id, string[] includes, CancellationToken cancellationToken = default)
    {
        IQueryable<AuditLogEntry> query = _dbSet;

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLogEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLogEntry>> FindAsync(
        System.Linq.Expressions.Expression<Func<AuditLogEntry, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLogEntry>> FindAsync(
        System.Linq.Expressions.Expression<Func<AuditLogEntry, bool>> predicate,
        string[] includes,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AuditLogEntry> query = _dbSet.AsNoTracking();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.Where(predicate).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuditLogEntry?> FirstOrDefaultAsync(
        System.Linq.Expressions.Expression<Func<AuditLogEntry, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(
        System.Linq.Expressions.Expression<Func<AuditLogEntry, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(
        System.Linq.Expressions.Expression<Func<AuditLogEntry, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        if (predicate == null)
            return await _dbSet.CountAsync(cancellationToken);

        return await _dbSet.CountAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuditLogEntry> AddAsync(AuditLogEntry entity, CancellationToken cancellationToken = default)
    {
        var entry = await _dbSet.AddAsync(entity, cancellationToken);
        return entry.Entity;
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<AuditLogEntry> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(AuditLogEntry entity)
    {
        _dbSet.Update(entity);
    }

    /// <inheritdoc />
    public void UpdateRange(IEnumerable<AuditLogEntry> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    /// <inheritdoc />
    public void Remove(AuditLogEntry entity)
    {
        _dbSet.Remove(entity);
    }

    /// <inheritdoc />
    public void RemoveRange(IEnumerable<AuditLogEntry> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        Remove(entity);
        return true;
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<AuditLogEntry>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await GetPagedAsync<long>(page, pageSize, null, null, false, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<AuditLogEntry>> GetPagedAsync<TOrderKey>(
        int page,
        int pageSize,
        System.Linq.Expressions.Expression<Func<AuditLogEntry, bool>>? predicate,
        System.Linq.Expressions.Expression<Func<AuditLogEntry, TOrderKey>>? orderBy,
        bool descending = false,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 1000) pageSize = 1000;

        IQueryable<AuditLogEntry> query = _dbSet.AsNoTracking();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (orderBy != null)
        {
            query = descending
                ? query.OrderByDescending(orderBy)
                : query.OrderBy(orderBy);
        }
        else
        {
            query = query.OrderByDescending(a => a.Timestamp);
        }

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PaginatedResult<AuditLogEntry>.Create(items, totalCount, page, pageSize);
    }

    /// <inheritdoc />
    public IQueryable<AuditLogEntry> Query()
    {
        return _dbSet;
    }

    /// <inheritdoc />
    public IQueryable<AuditLogEntry> QueryNoTracking()
    {
        return _dbSet.AsNoTracking();
    }

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLogEntry>> GetByEntityAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLogEntry>> GetByUserAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking()
            .Where(a => a.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<AuditLogEntry>> SearchAsync(
        AuditLogSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId.Value);

        if (!string.IsNullOrWhiteSpace(request.Username))
            query = query.Where(a => a.Username != null && a.Username.Contains(request.Username));

        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(a => a.Action == request.Action);

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(a => a.EntityType == request.EntityType);

        if (!string.IsNullOrWhiteSpace(request.EntityId))
            query = query.Where(a => a.EntityId == request.EntityId);

        if (request.StartDate.HasValue)
            query = query.Where(a => a.Timestamp >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(a => a.Timestamp <= request.EndDate.Value);

        if (!string.IsNullOrWhiteSpace(request.IpAddress))
            query = query.Where(a => a.IpAddress == request.IpAddress);

        var totalCount = await query.CountAsync(cancellationToken);

        var page = request.Page;
        var pageSize = request.PageSize;

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PaginatedResult<AuditLogEntry>.Create(items, totalCount, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLogEntry>> GetLoginsAsync(
        DateTime startDate,
        DateTime endDate,
        string? username = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking()
            .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate)
            .Where(a => a.Action == "Login" || a.Action == "LoginFailed" || a.Action == "Logout");

        if (!string.IsNullOrWhiteSpace(username))
            query = query.Where(a => a.Username == username);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLogEntry>> GetRecentAsync(
        int count = 50,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Write Operations

    /// <inheritdoc />
    public async Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        entry.Timestamp = DateTime.UtcNow;
        await _dbSet.AddAsync(entry, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogBatchAsync(
        IEnumerable<AuditLogEntry> entries,
        CancellationToken cancellationToken = default)
    {
        var timestamp = DateTime.UtcNow;
        foreach (var entry in entries)
        {
            if (entry.Timestamp == default)
                entry.Timestamp = timestamp;
        }

        await _dbSet.AddRangeAsync(entries, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogCreateAsync(
        Guid? userId,
        string? username,
        string entityType,
        string entityId,
        object? newValues,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            Username = username,
            Action = "Create",
            EntityType = entityType,
            EntityId = entityId,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            IpAddress = ipAddress
        };

        await _dbSet.AddAsync(entry, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogUpdateAsync(
        Guid? userId,
        string? username,
        string entityType,
        string entityId,
        object? oldValues,
        object? newValues,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            Username = username,
            Action = "Update",
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            IpAddress = ipAddress
        };

        await _dbSet.AddAsync(entry, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogDeleteAsync(
        Guid? userId,
        string? username,
        string entityType,
        string entityId,
        object? oldValues,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            Username = username,
            Action = "Delete",
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            IpAddress = ipAddress
        };

        await _dbSet.AddAsync(entry, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogLoginAsync(
        Guid? userId,
        string username,
        bool success,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            Username = username,
            Action = success ? "Login" : "LoginFailed",
            EntityType = "User",
            EntityId = userId?.ToString(),
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        await _dbSet.AddAsync(entry, cancellationToken);
    }

    #endregion

    #region Maintenance

    /// <inheritdoc />
    public async Task<int> ArchiveOldEntriesAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would move entries to an archive table
        // For now, we'll just count them
        return await _dbSet
            .Where(a => a.Timestamp < olderThan)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> DeleteOldEntriesAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        var oldEntries = await _dbSet
            .Where(a => a.Timestamp < olderThan)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(oldEntries);
        return oldEntries.Count;
    }

    #endregion
}
