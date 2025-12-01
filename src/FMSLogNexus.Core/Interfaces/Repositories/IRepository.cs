using System.Linq.Expressions;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.Entities;

namespace FMSLogNexus.Core.Interfaces.Repositories;

/// <summary>
/// Base repository interface for entities with Guid primary key.
/// </summary>
/// <typeparam name="TEntity">Entity type.</typeparam>
public interface IRepository<TEntity> : IRepository<TEntity, Guid>
    where TEntity : class, IEntity<Guid>
{
}

/// <summary>
/// Base repository interface for all entity types.
/// </summary>
/// <typeparam name="TEntity">Entity type.</typeparam>
/// <typeparam name="TKey">Primary key type.</typeparam>
public interface IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    #region Query Operations

    /// <summary>
    /// Gets an entity by its primary key.
    /// </summary>
    /// <param name="id">Primary key value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity or null if not found.</returns>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an entity by its primary key with related entities.
    /// </summary>
    /// <param name="id">Primary key value.</param>
    /// <param name="includes">Navigation properties to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity or null if not found.</returns>
    Task<TEntity?> GetByIdAsync(TKey id, string[] includes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All entities.</returns>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities matching a predicate.
    /// </summary>
    /// <param name="predicate">Filter expression.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching entities.</returns>
    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities matching a predicate with includes.
    /// </summary>
    /// <param name="predicate">Filter expression.</param>
    /// <param name="includes">Navigation properties to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching entities.</returns>
    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        string[] includes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity matching a predicate.
    /// </summary>
    /// <param name="predicate">Filter expression.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The first matching entity or null.</returns>
    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the predicate.
    /// </summary>
    /// <param name="predicate">Filter expression.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if any entity matches.</returns>
    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching a predicate.
    /// </summary>
    /// <param name="predicate">Filter expression (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of matching entities.</returns>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Command Operations

    /// <summary>
    /// Adds a new entity.
    /// </summary>
    /// <param name="entity">Entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added entity.</returns>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities.
    /// </summary>
    /// <param name="entities">Entities to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">Entity to update.</param>
    void Update(TEntity entity);

    /// <summary>
    /// Updates multiple entities.
    /// </summary>
    /// <param name="entities">Entities to update.</param>
    void UpdateRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Removes an entity.
    /// </summary>
    /// <param name="entity">Entity to remove.</param>
    void Remove(TEntity entity);

    /// <summary>
    /// Removes multiple entities.
    /// </summary>
    /// <param name="entities">Entities to remove.</param>
    void RemoveRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Removes an entity by its primary key.
    /// </summary>
    /// <param name="id">Primary key value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if entity was found and removed.</returns>
    Task<bool> RemoveByIdAsync(TKey id, CancellationToken cancellationToken = default);

    #endregion

    #region Pagination

    /// <summary>
    /// Gets a paginated list of entities.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated result.</returns>
    Task<PaginatedResult<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list with filtering and sorting.
    /// </summary>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="predicate">Filter expression.</param>
    /// <param name="orderBy">Order by expression.</param>
    /// <param name="descending">Sort descending.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated result.</returns>
    Task<PaginatedResult<TEntity>> GetPagedAsync<TOrderKey>(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate,
        Expression<Func<TEntity, TOrderKey>>? orderBy,
        bool descending = false,
        CancellationToken cancellationToken = default);

    #endregion

    #region Queryable Access

    /// <summary>
    /// Gets a queryable for custom queries (use sparingly).
    /// </summary>
    IQueryable<TEntity> Query();

    /// <summary>
    /// Gets a no-tracking queryable for read-only queries.
    /// </summary>
    IQueryable<TEntity> QueryNoTracking();

    #endregion
}

/// <summary>
/// Repository interface for soft-deletable entities.
/// </summary>
/// <typeparam name="TEntity">Entity type.</typeparam>
/// <typeparam name="TKey">Primary key type.</typeparam>
public interface ISoftDeleteRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, ISoftDeletable
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Soft deletes an entity.
    /// </summary>
    /// <param name="entity">Entity to soft delete.</param>
    /// <param name="deletedBy">User performing the deletion.</param>
    void SoftDelete(TEntity entity, string? deletedBy = null);

    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    /// <param name="entity">Entity to restore.</param>
    void Restore(TEntity entity);

    /// <summary>
    /// Gets all entities including soft-deleted ones.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All entities including deleted.</returns>
    Task<IReadOnlyList<TEntity>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only soft-deleted entities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deleted entities.</returns>
    Task<IReadOnlyList<TEntity>> GetDeletedAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only repository interface for query-only scenarios.
/// </summary>
/// <typeparam name="TEntity">Entity type.</typeparam>
/// <typeparam name="TKey">Primary key type.</typeparam>
public interface IReadOnlyRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
    IQueryable<TEntity> QueryNoTracking();
}
