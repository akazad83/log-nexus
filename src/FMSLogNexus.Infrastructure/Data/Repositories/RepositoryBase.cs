using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Interfaces.Repositories;

namespace FMSLogNexus.Infrastructure.Data.Repositories;

/// <summary>
/// Base repository implementation for entities with Guid primary key.
/// </summary>
/// <typeparam name="TEntity">Entity type.</typeparam>
public abstract class RepositoryBase<TEntity> : RepositoryBase<TEntity, Guid>, IRepository<TEntity>
    where TEntity : class, IEntity<Guid>
{
    protected RepositoryBase(FMSLogNexusDbContext context) : base(context)
    {
    }
}

/// <summary>
/// Base repository implementation for all entity types.
/// </summary>
/// <typeparam name="TEntity">Entity type.</typeparam>
/// <typeparam name="TKey">Primary key type.</typeparam>
public abstract class RepositoryBase<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    protected readonly FMSLogNexusDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    protected RepositoryBase(FMSLogNexusDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        DbSet = context.Set<TEntity>();
    }

    #region Query Operations

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetByIdAsync(TKey id, string[] includes, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = DbSet;

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        string[] includes,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = DbSet.AsNoTracking();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.Where(predicate).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        if (predicate == null)
            return await DbSet.CountAsync(cancellationToken);

        return await DbSet.CountAsync(predicate, cancellationToken);
    }

    #endregion

    #region Command Operations

    /// <inheritdoc />
    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(entity, cancellationToken);
        return entry.Entity;
    }

    /// <inheritdoc />
    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(entities, cancellationToken);
    }

    /// <inheritdoc />
    public virtual void Update(TEntity entity)
    {
        DbSet.Update(entity);
    }

    /// <inheritdoc />
    public virtual void UpdateRange(IEnumerable<TEntity> entities)
    {
        DbSet.UpdateRange(entities);
    }

    /// <inheritdoc />
    public virtual void Remove(TEntity entity)
    {
        DbSet.Remove(entity);
    }

    /// <inheritdoc />
    public virtual void RemoveRange(IEnumerable<TEntity> entities)
    {
        DbSet.RemoveRange(entities);
    }

    /// <inheritdoc />
    public virtual async Task<bool> RemoveByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        Remove(entity);
        return true;
    }

    #endregion

    #region Pagination

    /// <inheritdoc />
    public virtual async Task<PaginatedResult<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await GetPagedAsync<TKey>(page, pageSize, null, null, false, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<PaginatedResult<TEntity>> GetPagedAsync<TOrderKey>(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate,
        Expression<Func<TEntity, TOrderKey>>? orderBy,
        bool descending = false,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 1000) pageSize = 1000;

        IQueryable<TEntity> query = DbSet.AsNoTracking();

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

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PaginatedResult<TEntity>.Create(items, totalCount, page, pageSize);
    }

    #endregion

    #region Queryable Access

    /// <inheritdoc />
    public virtual IQueryable<TEntity> Query()
    {
        return DbSet;
    }

    /// <inheritdoc />
    public virtual IQueryable<TEntity> QueryNoTracking()
    {
        return DbSet.AsNoTracking();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Applies includes to a query.
    /// </summary>
    protected IQueryable<TEntity> ApplyIncludes(IQueryable<TEntity> query, params Expression<Func<TEntity, object>>[] includes)
    {
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        return query;
    }

    /// <summary>
    /// Applies string-based includes to a query.
    /// </summary>
    protected IQueryable<TEntity> ApplyIncludes(IQueryable<TEntity> query, IEnumerable<string> includes)
    {
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        return query;
    }

    /// <summary>
    /// Applies pagination to a query.
    /// </summary>
    protected IQueryable<TEntity> ApplyPaging(IQueryable<TEntity> query, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 1000) pageSize = 1000;

        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }

    /// <summary>
    /// Creates a paginated result from query and count.
    /// </summary>
    protected async Task<PaginatedResult<T>> CreatePaginatedResultAsync<T>(
        IQueryable<T> query,
        int totalCount,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var items = await query.ToListAsync(cancellationToken);
        return PaginatedResult<T>.Create(items, totalCount, page, pageSize);
    }

    #endregion
}

/// <summary>
/// Base repository for soft-deletable entities.
/// </summary>
/// <typeparam name="TEntity">Entity type.</typeparam>
/// <typeparam name="TKey">Primary key type.</typeparam>
public abstract class SoftDeleteRepositoryBase<TEntity, TKey> : RepositoryBase<TEntity, TKey>, ISoftDeleteRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, ISoftDeletable
    where TKey : IEquatable<TKey>
{
    protected SoftDeleteRepositoryBase(FMSLogNexusDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public override async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(e => e.Id.Equals(id) && !e.IsDeleted, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(e => !e.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(e => !e.IsDeleted)
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual void SoftDelete(TEntity entity, string? deletedBy = null)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = deletedBy;
        Update(entity);
    }

    /// <inheritdoc />
    public virtual void Restore(TEntity entity)
    {
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
        Update(entity);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> GetDeletedAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(e => e.IsDeleted)
            .ToListAsync(cancellationToken);
    }
}
