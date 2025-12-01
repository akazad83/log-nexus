namespace FMSLogNexus.Core.Interfaces.Repositories;

/// <summary>
/// Unit of Work pattern for coordinating repository operations
/// and managing database transactions.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    #region Repositories

    /// <summary>
    /// Log entry repository.
    /// </summary>
    ILogRepository Logs { get; }

    /// <summary>
    /// Job repository.
    /// </summary>
    IJobRepository Jobs { get; }

    /// <summary>
    /// Job execution repository.
    /// </summary>
    IJobExecutionRepository JobExecutions { get; }

    /// <summary>
    /// Server repository.
    /// </summary>
    IServerRepository Servers { get; }

    /// <summary>
    /// Alert repository.
    /// </summary>
    IAlertRepository Alerts { get; }

    /// <summary>
    /// Alert instance repository.
    /// </summary>
    IAlertInstanceRepository AlertInstances { get; }

    /// <summary>
    /// User repository.
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// API key repository.
    /// </summary>
    IApiKeyRepository ApiKeys { get; }

    /// <summary>
    /// Refresh token repository.
    /// </summary>
    IRefreshTokenRepository RefreshTokens { get; }

    /// <summary>
    /// System configuration repository.
    /// </summary>
    ISystemConfigurationRepository SystemConfiguration { get; }

    /// <summary>
    /// Audit log repository.
    /// </summary>
    IAuditLogRepository AuditLog { get; }

    #endregion

    #region Transaction Management

    /// <summary>
    /// Saves all changes made in this unit of work to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of entities written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes with automatic audit logging.
    /// </summary>
    /// <param name="userId">User ID performing the operation.</param>
    /// <param name="username">Username performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of entities written to the database.</returns>
    Task<int> SaveChangesWithAuditAsync(
        Guid? userId,
        string? username,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins an explicit database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transaction scope for explicit control.</returns>
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an action within a transaction.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteInTransactionAsync(
        Func<Task> action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an action within a transaction and returns a result.
    /// </summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="action">Action to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the action.</returns>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken = default);

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Executes a raw SQL command.
    /// </summary>
    /// <param name="sql">SQL command.</param>
    /// <param name="parameters">SQL parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of rows affected.</returns>
    Task<int> ExecuteSqlAsync(
        string sql,
        object[]? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a raw SQL query and returns results.
    /// </summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="sql">SQL query.</param>
    /// <param name="parameters">SQL parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Query results.</returns>
    Task<IReadOnlyList<T>> QuerySqlAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region State Management

    /// <summary>
    /// Detaches all tracked entities from the context.
    /// </summary>
    void ClearChangeTracker();

    /// <summary>
    /// Checks if there are any pending changes.
    /// </summary>
    bool HasChanges { get; }

    #endregion
}

/// <summary>
/// Database transaction abstraction.
/// </summary>
public interface IDbTransaction : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Transaction ID.
    /// </summary>
    Guid TransactionId { get; }

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
