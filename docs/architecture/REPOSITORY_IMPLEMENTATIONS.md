# FMS Log Nexus - Repository Implementations

## Overview

This document describes the repository implementations for the FMS Log Nexus data access layer. The repositories follow the Repository pattern and Unit of Work pattern to provide a clean abstraction over Entity Framework Core.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Application Layer                         │
│                          (Services)                              │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                         IUnitOfWork                              │
│  ┌─────────────┬─────────────┬─────────────┬─────────────┐     │
│  │    ILog     │    IJob     │  IServer    │   IAlert    │     │
│  │ Repository  │ Repository  │ Repository  │ Repository  │     │
│  └─────────────┴─────────────┴─────────────┴─────────────┘     │
│  ┌─────────────┬─────────────┬─────────────┬─────────────┐     │
│  │  IJobExec   │   IUser     │  IApiKey    │  IRefresh   │     │
│  │ Repository  │ Repository  │ Repository  │  TokenRepo  │     │
│  └─────────────┴─────────────┴─────────────┴─────────────┘     │
│  ┌─────────────┬─────────────┬─────────────┐                   │
│  │ IAlertInst  │ ISysConfig  │ IAuditLog   │                   │
│  │ Repository  │ Repository  │ Repository  │                   │
│  └─────────────┴─────────────┴─────────────┘                   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    FMSLogNexusDbContext                          │
│                    (Entity Framework Core)                       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       SQL Server Database                        │
└─────────────────────────────────────────────────────────────────┘
```

## File Structure

```
src/FMSLogNexus.Infrastructure/
└── Data/
    └── Repositories/
        ├── RepositoryBase.cs           # Base repository implementations
        ├── LogRepository.cs            # Log entry operations
        ├── JobRepository.cs            # Job operations
        ├── JobExecutionRepository.cs   # Job execution operations
        ├── ServerRepository.cs         # Server operations
        ├── AlertRepositories.cs        # Alert and AlertInstance operations
        ├── AuthRepositories.cs         # User, ApiKey, RefreshToken operations
        ├── SystemRepositories.cs       # Configuration and AuditLog operations
        ├── UnitOfWork.cs               # Unit of Work implementation
        └── RepositoryServiceExtensions.cs  # DI registration
```

## Repository Implementations

### Base Repository (RepositoryBase.cs)

The base repository provides common CRUD operations for all entities:

```csharp
public abstract class RepositoryBase<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    // Common operations
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    Task<PaginatedResult<TEntity>> GetPagedAsync(int page, int pageSize, CancellationToken ct);
    IQueryable<TEntity> Query();
    IQueryable<TEntity> QueryNoTracking();
}
```

**Key Features:**
- Generic implementation for any entity type
- Support for composite and string primary keys
- No-tracking queries by default for read operations
- Pagination support with validation
- Include support for eager loading

### Log Repository (LogRepository.cs)

Optimized for high-volume log ingestion and querying.

**Key Methods:**

| Method | Description |
|--------|-------------|
| `BulkInsertAsync` | High-performance batch insert (batches of 1000) |
| `StreamInsertAsync` | Streaming insert for very large batches |
| `SearchAsync` | Comprehensive filtering with pagination |
| `FullTextSearchAsync` | Message text search |
| `GetByCorrelationIdAsync` | Distributed tracing support |
| `GetByTraceIdAsync` | OpenTelemetry trace support |
| `GetByJobExecutionAsync` | Logs for specific execution |
| `GetRecentErrorsAsync` | Quick access to recent errors |
| `GetCountsByLevelAsync` | Aggregation by log level |
| `GetTrendDataAsync` | Time-series trend data |
| `GetTopExceptionsAsync` | Most frequent exceptions |
| `DeleteOldLogsAsync` | Batch deletion for retention |

**Example Usage:**

```csharp
// Search logs with filters
var results = await _unitOfWork.Logs.SearchAsync(new LogSearchRequest
{
    StartDate = DateTime.UtcNow.AddHours(-24),
    MinLevel = LogLevel.Warning,
    ServerName = "PROD-SERVER-01",
    Page = 1,
    PageSize = 100
});

// Get trend data for dashboard
var trends = await _unitOfWork.Logs.GetTrendDataAsync(
    DateTime.UtcNow.AddDays(-7),
    DateTime.UtcNow,
    TrendInterval.Hour,
    serverName: "PROD-SERVER-01"
);
```

### Job Repository (JobRepository.cs)

Manages job definitions and their statistics.

**Key Methods:**

| Method | Description |
|--------|-------------|
| `SearchAsync` | Search with filtering and sorting |
| `GetWithRecentExecutionsAsync` | Job with latest executions |
| `GetByServerAsync` | Jobs on a specific server |
| `GetCriticalJobsAsync` | All critical jobs |
| `GetFailedJobsAsync` | Jobs with failed last run |
| `GetStaleJobsAsync` | Jobs not running as scheduled |
| `GetJobHealthAsync` | Health scores for all jobs |
| `CalculateHealthScoreAsync` | Health score for single job |
| `RegisterOrUpdateAsync` | Upsert job definition |
| `UpdateStatisticsAsync` | Update after execution |

**Health Score Calculation:**
- Starts at 100 points
- Deductions for:
  - Failed last status: -40
  - Timeout last status: -30
  - Low success rate (<50%): -30
  - No recent executions: -20
  - High failure count: -10

### Job Execution Repository (JobExecutionRepository.cs)

Tracks individual job runs.

**Key Methods:**

| Method | Description |
|--------|-------------|
| `SearchAsync` | Search executions with filters |
| `GetWithJobAsync` | Execution with job details |
| `GetByJobAsync` | Paginated executions for a job |
| `GetRunningAsync` | All currently running |
| `GetOverdueAsync` | Executions exceeding timeout |
| `IsJobRunningAsync` | Check if job is running |
| `GetRecentFailuresAsync` | Recent failed executions |
| `GetConsecutiveFailureCountAsync` | Count of consecutive failures |
| `StartAsync` | Begin a new execution |
| `CompleteAsync` | Complete an execution |
| `IncrementLogCountAsync` | Update log counts |
| `TimeoutStuckExecutionsAsync` | Auto-timeout stuck jobs |

### Server Repository (ServerRepository.cs)

Manages server registrations and heartbeats.

**Key Methods:**

| Method | Description |
|--------|-------------|
| `GetActiveServersAsync` | All active servers |
| `GetByStatusAsync` | Servers by status |
| `GetWithJobsAsync` | Server with its jobs |
| `ProcessHeartbeatAsync` | Handle heartbeat (upsert) |
| `MarkOfflineServersAsync` | Mark unresponsive as offline |
| `GetUnresponsiveAsync` | Servers not sending heartbeats |
| `GetStatusSummaryAsync` | Status distribution summary |
| `SetMaintenanceModeAsync` | Toggle maintenance mode |

### Alert Repositories (AlertRepositories.cs)

#### AlertRepository
Manages alert rule definitions.

| Method | Description |
|--------|-------------|
| `GetActiveAlertsAsync` | All enabled alerts |
| `GetByTypeAsync` | Alerts by type |
| `GetByJobAsync` | Alerts for specific job |
| `GetAlertsNeedingEvaluationAsync` | Alerts past throttle period |
| `RecordTriggerAsync` | Update trigger timestamp |

#### AlertInstanceRepository
Manages triggered alert instances.

| Method | Description |
|--------|-------------|
| `SearchAsync` | Search with filters |
| `GetActiveInstancesAsync` | Unresolved instances |
| `GetUnacknowledgedAsync` | New instances only |
| `AcknowledgeAsync` | Acknowledge single instance |
| `ResolveAsync` | Resolve single instance |
| `BulkAcknowledgeAsync` | Bulk acknowledge |
| `BulkResolveAsync` | Bulk resolve |
| `GetSummaryAsync` | Status and severity counts |

### Authentication Repositories (AuthRepositories.cs)

#### UserRepository
User account management.

| Method | Description |
|--------|-------------|
| `GetByUsernameAsync` | Find by username |
| `GetByEmailAsync` | Find by email |
| `GetByRoleAsync` | Users with specific role |
| `SearchAsync` | Search users |
| `IsUsernameAvailableAsync` | Check username availability |
| `RecordLoginAsync` | Track login attempts |
| `UnlockAsync` | Unlock locked account |

#### ApiKeyRepository
API key management.

| Method | Description |
|--------|-------------|
| `GetByKeyHashAsync` | Find by hash |
| `GetByPrefixAsync` | Find by prefix |
| `GetByUserAsync` | User's API keys |
| `GetExpiringAsync` | Keys expiring soon |
| `RevokeAsync` | Revoke single key |
| `RecordUsageAsync` | Track key usage |
| `IsValidAsync` | Validate key |

#### RefreshTokenRepository
JWT refresh token management.

| Method | Description |
|--------|-------------|
| `GetByTokenHashAsync` | Find by hash |
| `GetActiveByUserAsync` | User's valid tokens |
| `RevokeAsync` | Revoke token |
| `RotateAsync` | Rotate token (revoke old, create new) |
| `DeleteExpiredAsync` | Clean up expired tokens |

### System Repositories (SystemRepositories.cs)

#### SystemConfigurationRepository
Application settings management.

| Method | Description |
|--------|-------------|
| `GetValueAsync<T>` | Get typed config value |
| `SetValueAsync<T>` | Set config value |
| `GetByCategoryAsync` | Get by category |
| `CreateOrUpdateAsync` | Upsert config |
| `GetLogRetentionDaysAsync` | Log retention setting |
| `GetHeartbeatTimeoutSecondsAsync` | Heartbeat timeout |

#### AuditLogRepository
Audit trail management.

| Method | Description |
|--------|-------------|
| `SearchAsync` | Search audit entries |
| `GetByUserAsync` | User's audit trail |
| `GetByEntityAsync` | Entity's history |
| `GetLoginHistoryAsync` | Login audit trail |
| `LogAsync` | Create audit entry |
| `LogCreateAsync<T>` | Log entity creation |
| `LogUpdateAsync<T>` | Log entity update |
| `LogDeleteAsync<T>` | Log entity deletion |

### Unit of Work (UnitOfWork.cs)

Coordinates multiple repositories in a single transaction.

```csharp
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    // Repository access
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

    // Transaction management
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<int> SaveChangesWithAuditAsync(Guid? userId, string? username, CancellationToken ct);
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken ct);
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct);
    
    // Raw SQL support
    Task<int> ExecuteSqlAsync(string sql, object[]? parameters, CancellationToken ct);
    Task<IReadOnlyList<T>> QuerySqlAsync<T>(string sql, object? parameters, CancellationToken ct);
}
```

**Example Usage:**

```csharp
// Using multiple repositories in a transaction
await _unitOfWork.ExecuteInTransactionAsync(async () =>
{
    // Create job execution
    var execution = await _unitOfWork.JobExecutions.StartAsync(new JobExecution
    {
        JobId = "daily-report",
        ServerName = "PROD-SERVER-01"
    });

    // Update job statistics
    await _unitOfWork.Jobs.UpdateStatisticsAsync("daily-report", execution);

    // Log audit entry
    await _unitOfWork.AuditLog.LogCreateAsync(execution, userId, username);
});
```

## Dependency Injection

### Registration Options

```csharp
// Option 1: Full data access layer
services.AddDataAccessLayer(
    connectionString: "Server=...;Database=FMSLogNexus;...",
    enableSensitiveDataLogging: isDevelopment,
    enableDetailedErrors: isDevelopment
);

// Option 2: With connection pooling for high-performance
services.AddDataAccessLayerWithPooling(
    connectionString: "Server=...;Database=FMSLogNexus;...",
    poolSize: 1024
);

// Option 3: Unit of Work only
services.AddFMSLogNexusDbContext(connectionString);
services.AddUnitOfWork();

// Option 4: Individual repositories
services.AddFMSLogNexusDbContext(connectionString);
services.AddRepositories();
```

### Using in Services

```csharp
public class LogService : ILogService
{
    private readonly IUnitOfWork _unitOfWork;

    public LogService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginatedResult<LogEntry>> SearchLogsAsync(LogSearchRequest request)
    {
        return await _unitOfWork.Logs.SearchAsync(request);
    }
}
```

## Performance Considerations

### 1. No-Tracking Queries
All read operations use `AsNoTracking()` by default:
```csharp
return await DbSet.AsNoTracking()
    .Where(predicate)
    .ToListAsync(cancellationToken);
```

### 2. Batch Operations
Large operations are batched to prevent memory issues:
```csharp
// Bulk insert logs in batches of 1000
const int batchSize = 1000;
foreach (var batch in logList.Chunk(batchSize))
{
    await DbSet.AddRangeAsync(batch, cancellationToken);
    await Context.SaveChangesAsync(cancellationToken);
    Context.ChangeTracker.Clear(); // Free memory
}
```

### 3. Streaming for Large Exports
```csharp
public async IAsyncEnumerable<LogEntry> ExportAsync(LogExportRequest request, ...)
{
    await foreach (var log in query.AsAsyncEnumerable())
    {
        yield return log;
    }
}
```

### 4. Filtered Indexes
Repository methods leverage filtered indexes defined in entity configurations:
```csharp
// Uses IX_LogEntries_Level_Timestamp filtered index (Level >= 4)
query.Where(l => l.Level >= LogLevel.Error)
     .Where(l => l.Timestamp >= startDate);
```

### 5. Dapper for Complex Queries
Unit of Work supports raw SQL via Dapper for complex scenarios:
```csharp
var results = await _unitOfWork.QuerySqlAsync<DashboardMetric>(
    "SELECT * FROM [log].[vw_DashboardMetrics] WHERE ServerName = @Server",
    new { Server = serverName }
);
```

## Best Practices

1. **Use Unit of Work for transactions** - Always use `ExecuteInTransactionAsync` for multi-repository operations
2. **Prefer specific methods over raw queries** - Repository methods are optimized for common operations
3. **Use pagination** - Always paginate large result sets
4. **Clear change tracker** - Call `ClearChangeTracker()` after large batch operations
5. **Handle cancellation** - Always pass `CancellationToken` through the call chain

## Statistics Summary

| Component | Lines of Code | Methods |
|-----------|--------------|---------|
| RepositoryBase | 250 | 20 |
| LogRepository | 550 | 24 |
| JobRepository | 350 | 18 |
| JobExecutionRepository | 450 | 22 |
| ServerRepository | 200 | 14 |
| AlertRepositories | 350 | 22 |
| AuthRepositories | 400 | 28 |
| SystemRepositories | 350 | 22 |
| UnitOfWork | 180 | 12 |
| RepositoryServiceExtensions | 80 | 4 |
| **Total** | **~3,160** | **~186** |
