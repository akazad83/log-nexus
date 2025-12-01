# FMS Log Nexus - Repository Interfaces Reference

## Overview

This document describes the repository interfaces that define data access contracts for the FMS Log Nexus application. The repository pattern provides:

- **Abstraction**: Decouples business logic from data access implementation
- **Testability**: Enables mocking for unit tests
- **Flexibility**: Allows swapping data access technologies
- **Consistency**: Standardized data access patterns across the application

---

## Interface Hierarchy

```
IRepository<TEntity, TKey>
├── IRepository<TEntity> (Guid key shortcut)
├── ISoftDeleteRepository<TEntity, TKey>
└── IReadOnlyRepository<TEntity, TKey>

IUnitOfWork
├── Repositories (all domain repositories)
├── Transaction Management
└── Bulk Operations
```

---

## Base Interfaces

### IRepository<TEntity, TKey>

Generic repository with full CRUD operations.

| Method | Description |
|--------|-------------|
| `GetByIdAsync(id)` | Get entity by primary key |
| `GetByIdAsync(id, includes)` | Get with navigation properties |
| `GetAllAsync()` | Get all entities |
| `FindAsync(predicate)` | Find by expression |
| `FirstOrDefaultAsync(predicate)` | Get first match |
| `AnyAsync(predicate)` | Check if any match |
| `CountAsync(predicate)` | Count matches |
| `AddAsync(entity)` | Add new entity |
| `AddRangeAsync(entities)` | Add multiple entities |
| `Update(entity)` | Update entity |
| `UpdateRange(entities)` | Update multiple |
| `Remove(entity)` | Remove entity |
| `RemoveRange(entities)` | Remove multiple |
| `RemoveByIdAsync(id)` | Remove by ID |
| `GetPagedAsync(page, pageSize)` | Paginated query |
| `Query()` | Get IQueryable |
| `QueryNoTracking()` | Get read-only IQueryable |

### IUnitOfWork

Coordinates repository operations and manages transactions.

```csharp
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    // Repositories
    ILogRepository Logs { get; }
    IJobRepository Jobs { get; }
    IJobExecutionRepository JobExecutions { get; }
    IServerRepository Servers { get; }
    IAlertRepository Alerts { get; }
    IAlertInstanceRepository AlertInstances { get; }
    IUserRepository Users { get; }
    IApiKeyRepository ApiKeys { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    ISystemConfigurationRepository SystemConfiguration { get; }
    IAuditLogRepository AuditLog { get; }

    // Operations
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<int> SaveChangesWithAuditAsync(Guid? userId, string? username, CancellationToken ct = default);
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken ct = default);
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);
}
```

---

## Domain Repositories

### ILogRepository

High-performance log ingestion and querying.

| Category | Methods |
|----------|---------|
| **Ingestion** | `BulkInsertAsync`, `StreamInsertAsync` |
| **Search** | `SearchAsync`, `FullTextSearchAsync`, `GetByCorrelationIdAsync`, `GetByTraceIdAsync`, `GetByJobExecutionAsync` |
| **Statistics** | `GetCountsByLevelAsync`, `GetTrendDataAsync`, `GetTopExceptionsAsync`, `GetSummaryAsync` |
| **Lookups** | `GetDistinctServersAsync`, `GetDistinctJobsAsync`, `GetDistinctCategoriesAsync`, `GetDistinctTagsAsync` |
| **Maintenance** | `DeleteOldLogsAsync`, `ArchiveLogsAsync`, `GetPartitionInfoAsync` |
| **Export** | `ExportAsync` (IAsyncEnumerable) |

### IJobRepository

Job registration and health tracking.

| Category | Methods |
|----------|---------|
| **Query** | `SearchAsync`, `GetWithRecentExecutionsAsync`, `GetByServerAsync`, `GetByCategoryAsync` |
| **Health** | `GetJobHealthAsync`, `CalculateHealthScoreAsync`, `GetStatisticsSummaryAsync`, `GetUnhealthyJobsAsync` |
| **Status** | `GetCriticalJobsAsync`, `GetFailedJobsAsync`, `GetStaleJobsAsync` |
| **Registration** | `RegisterOrUpdateAsync`, `UpdateStatisticsAsync`, `ActivateAsync`, `DeactivateAsync` |

### IJobExecutionRepository

Execution lifecycle management.

| Category | Methods |
|----------|---------|
| **Query** | `SearchAsync`, `GetWithJobAsync`, `GetByJobAsync`, `GetByCorrelationIdAsync` |
| **Running** | `GetRunningAsync`, `GetRunningByJobAsync`, `GetOverdueAsync`, `IsJobRunningAsync` |
| **Failures** | `GetRecentFailuresAsync`, `GetConsecutiveFailureCountAsync` |
| **Statistics** | `GetCountsByStatusAsync`, `GetStatisticsAsync`, `GetTrendDataAsync` |
| **Lifecycle** | `StartAsync`, `CompleteAsync`, `IncrementLogCountAsync`, `TimeoutStuckExecutionsAsync` |

### IServerRepository

Server registration and heartbeat management.

| Category | Methods |
|----------|---------|
| **Query** | `GetActiveAsync`, `GetByStatusAsync`, `GetWithJobsAsync`, `GetUnresponsiveAsync` |
| **Status** | `GetStatusSummaryAsync`, `GetAllStatusesAsync`, `CalculateStatusAsync` |
| **Heartbeat** | `UpdateHeartbeatAsync`, `MarkOfflineAsync` |
| **Registration** | `RegisterOrUpdateAsync`, `SetMaintenanceModeAsync` |

### IAlertRepository

Alert rule definitions.

| Category | Methods |
|----------|---------|
| **Query** | `GetActiveAsync`, `GetByTypeAsync`, `GetByJobAsync`, `GetByServerAsync`, `GetWithInstancesAsync` |
| **Trigger** | `GetTriggerableAsync`, `RecordTriggerAsync`, `CanTriggerAsync` |
| **Activation** | `ActivateAsync`, `DeactivateAsync` |

### IAlertInstanceRepository

Alert instance lifecycle.

| Category | Methods |
|----------|---------|
| **Query** | `SearchAsync`, `GetActiveAsync`, `GetUnacknowledgedAsync`, `GetByAlertAsync`, `GetRecentBySeverityAsync` |
| **Summary** | `GetCountsByStatusAsync`, `GetCountsBySeverityAsync`, `GetSummaryAsync` |
| **Lifecycle** | `CreateAsync`, `AcknowledgeAsync`, `ResolveAsync`, `SuppressAsync` |
| **Bulk** | `BulkAcknowledgeAsync`, `BulkResolveAsync` |
| **Notifications** | `RecordNotificationAsync` |

### IUserRepository

User management and authentication.

| Category | Methods |
|----------|---------|
| **Query** | `GetByUsernameAsync`, `GetByEmailAsync`, `GetWithApiKeysAsync`, `SearchAsync` |
| **Validation** | `UsernameExistsAsync`, `EmailExistsAsync` |
| **Auth** | `RecordLoginAsync`, `RecordFailedLoginAsync`, `UnlockAsync`, `UpdatePasswordAsync`, `UpdateSecurityStampAsync` |
| **Preferences** | `UpdatePreferencesAsync`, `GetPreferencesAsync` |

### IApiKeyRepository

API key management.

| Category | Methods |
|----------|---------|
| **Query** | `GetByHashAsync`, `GetByPrefixAsync`, `GetByUserAsync`, `GetByServerAsync`, `GetExpiringAsync` |
| **Validation** | `ValidateKeyAsync`, `NameExistsAsync` |
| **Usage** | `RecordUsageAsync`, `GetUsageStatsAsync` |
| **Lifecycle** | `RevokeAsync`, `RevokeAllForUserAsync`, `ActivateAsync`, `DeactivateAsync` |

### IRefreshTokenRepository

JWT refresh token management.

| Category | Methods |
|----------|---------|
| **Query** | `GetByHashAsync`, `GetWithUserAsync`, `GetActiveByUserAsync`, `GetTokenChainAsync` |
| **Validation** | `ValidateTokenAsync`, `IsValidAsync` |
| **Lifecycle** | `CreateAsync`, `RotateAsync`, `RevokeAsync`, `RevokeAllForUserAsync`, `RevokeChainAsync` |
| **Sessions** | `GetActiveCountForUserAsync`, `EnforceMaxSessionsAsync` |

### ISystemConfigurationRepository

System settings management.

| Category | Methods |
|----------|---------|
| **Query** | `GetValueAsync`, `GetValueAsync<T>`, `GetByCategoryAsync`, `GetAllGroupedAsync` |
| **Update** | `SetValueAsync`, `SetValuesAsync`, `UpsertAsync`, `DeleteAsync` |
| **Cache** | `GetAllForCacheAsync`, `GetLastUpdateTimeAsync` |

### IAuditLogRepository

Audit trail management.

| Category | Methods |
|----------|---------|
| **Query** | `GetByEntityAsync`, `GetByUserAsync`, `SearchAsync`, `GetRecentAsync`, `GetLoginsAsync` |
| **Write** | `LogAsync`, `LogBatchAsync`, `LogCreateAsync`, `LogUpdateAsync`, `LogDeleteAsync`, `LogLoginAsync` |
| **Maintenance** | `DeleteOldEntriesAsync`, `ArchiveOldEntriesAsync` |

### IDashboardCacheRepository

Dashboard data caching.

| Method | Description |
|--------|-------------|
| `GetAsync<T>` | Get cached value |
| `SetAsync<T>` | Set cached value with TTL |
| `GetOrSetAsync<T>` | Get or compute and cache |
| `InvalidateAsync` | Invalidate specific key |
| `InvalidatePatternAsync` | Invalidate by pattern |
| `ClearAllAsync` | Clear all cache |
| `CleanupExpiredAsync` | Remove expired entries |

---

## Usage Examples

### Basic Repository Operations

```csharp
// Dependency Injection
public class LogService : ILogService
{
    private readonly IUnitOfWork _unitOfWork;

    public LogService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LogEntry?> GetLogAsync(Guid id)
    {
        return await _unitOfWork.Logs.GetByIdAsync(id);
    }

    public async Task CreateLogAsync(LogEntry log)
    {
        await _unitOfWork.Logs.AddAsync(log);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

### Transaction Management

```csharp
public async Task CreateJobWithExecutionAsync(Job job, JobExecution execution)
{
    await _unitOfWork.ExecuteInTransactionAsync(async () =>
    {
        await _unitOfWork.Jobs.AddAsync(job);
        await _unitOfWork.JobExecutions.AddAsync(execution);
        await _unitOfWork.SaveChangesAsync();
    });
}
```

### Paginated Queries

```csharp
public async Task<PaginatedResult<LogEntry>> SearchLogsAsync(LogSearchRequest request)
{
    return await _unitOfWork.Logs.SearchAsync(request);
}
```

### Bulk Operations

```csharp
public async Task<int> IngestLogsAsync(IEnumerable<LogEntry> logs)
{
    return await _unitOfWork.Logs.BulkInsertAsync(logs);
}
```

---

## File Structure

```
src/FMSLogNexus.Core/Interfaces/Repositories/
├── IRepository.cs              # Base repository interfaces
├── IUnitOfWork.cs              # Unit of Work pattern
├── ILogRepository.cs           # Log operations
├── IJobRepository.cs           # Job operations
├── IJobExecutionRepository.cs  # Execution operations
├── IServerRepository.cs        # Server operations
├── IAlertRepository.cs         # Alert operations (definitions + instances)
├── IUserRepository.cs          # User operations
├── IApiKeyRepository.cs        # API key operations
├── IRefreshTokenRepository.cs  # Refresh token operations
└── ISystemRepository.cs        # System config, audit log, cache
```

---

## Implementation Notes

### Thread Safety
- All repository methods are designed to be thread-safe
- Unit of Work should be scoped per request (not singleton)

### Async Patterns
- All I/O operations use `async/await`
- Support `CancellationToken` for cancellation
- Use `IAsyncEnumerable` for streaming large datasets

### Performance
- Use `QueryNoTracking()` for read-only queries
- Use bulk methods for high-volume operations
- Leverage database-specific optimizations

### Error Handling
- Repositories throw domain exceptions, not database exceptions
- Wrap in try-catch at service layer
- Use transactions for multi-step operations
