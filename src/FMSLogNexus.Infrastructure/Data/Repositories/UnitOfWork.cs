using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.SqlClient;
using FMSLogNexus.Core.Interfaces.Repositories;
using Dapper;

namespace FMSLogNexus.Infrastructure.Data.Repositories;

/// <summary>
/// Unit of Work implementation for coordinating repository operations
/// and managing database transactions.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly FMSLogNexusDbContext _context;
    private bool _disposed;

    // Lazy-loaded repositories
    private ILogRepository? _logs;
    private IJobRepository? _jobs;
    private IJobExecutionRepository? _jobExecutions;
    private IServerRepository? _servers;
    private IAlertRepository? _alerts;
    private IAlertInstanceRepository? _alertInstances;
    private IUserRepository? _users;
    private IApiKeyRepository? _apiKeys;
    private IRefreshTokenRepository? _refreshTokens;
    private ISystemConfigurationRepository? _systemConfiguration;
    private IAuditLogRepository? _auditLog;

    public UnitOfWork(FMSLogNexusDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Repositories

    /// <inheritdoc />
    public ILogRepository Logs => _logs ??= new LogRepository(_context);

    /// <inheritdoc />
    public IJobRepository Jobs => _jobs ??= new JobRepository(_context);

    /// <inheritdoc />
    public IJobExecutionRepository JobExecutions => _jobExecutions ??= new JobExecutionRepository(_context);

    /// <inheritdoc />
    public IServerRepository Servers => _servers ??= new ServerRepository(_context);

    /// <inheritdoc />
    public IAlertRepository Alerts => _alerts ??= new AlertRepository(_context);

    /// <inheritdoc />
    public IAlertInstanceRepository AlertInstances => _alertInstances ??= new AlertInstanceRepository(_context);

    /// <inheritdoc />
    public IUserRepository Users => _users ??= new UserRepository(_context);

    /// <inheritdoc />
    public IApiKeyRepository ApiKeys => _apiKeys ??= new ApiKeyRepository(_context);

    /// <inheritdoc />
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_context);

    /// <inheritdoc />
    public ISystemConfigurationRepository SystemConfiguration => _systemConfiguration ??= new SystemConfigurationRepository(_context);

    /// <inheritdoc />
    public IAuditLogRepository AuditLog => _auditLog ??= new AuditLogRepository(_context);

    #endregion

    #region Transaction Management

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesWithAuditAsync(
        Guid? userId,
        string? username,
        CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesWithAuditAsync(userId, username, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new DbTransactionWrapper(transaction);
    }

    /// <inheritdoc />
    public async Task ExecuteInTransactionAsync(
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    /// <inheritdoc />
    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action();
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    #endregion

    #region Bulk Operations

    /// <inheritdoc />
    public async Task<int> ExecuteSqlAsync(
        string sql,
        object[]? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters == null || parameters.Length == 0)
        {
            return await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }

        return await _context.Database.ExecuteSqlRawAsync(sql, parameters);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> QuerySqlAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
        }

        var result = await connection.QueryAsync<T>(sql, parameters);
        return result.AsList();
    }

    #endregion

    #region State Management

    /// <inheritdoc />
    public void ClearChangeTracker()
    {
        _context.ChangeTracker.Clear();
    }

    /// <inheritdoc />
    public bool HasChanges => _context.ChangeTracker.HasChanges();

    #endregion

    #region Disposal

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _context.DisposeAsync();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    #endregion
}

/// <summary>
/// Wrapper for EF Core database transaction.
/// </summary>
internal class DbTransactionWrapper : IDbTransaction
{
    private readonly IDbContextTransaction _transaction;
    private bool _disposed;

    public DbTransactionWrapper(IDbContextTransaction transaction)
    {
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        TransactionId = Guid.NewGuid();
    }

    /// <inheritdoc />
    public Guid TransactionId { get; }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.RollbackAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _transaction.Dispose();
            _disposed = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _transaction.DisposeAsync();
            _disposed = true;
        }
    }
}
