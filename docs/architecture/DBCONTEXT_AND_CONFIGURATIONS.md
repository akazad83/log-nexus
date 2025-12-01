# Step 12: DbContext and Entity Configurations

## Overview

This document describes the Entity Framework Core infrastructure for FMS Log Nexus, including the DbContext, entity configurations, and helper extensions.

## File Structure

```
src/FMSLogNexus.Infrastructure/
├── Data/
│   ├── FMSLogNexusDbContext.cs          # Main DbContext (320 lines)
│   ├── FMSLogNexusDbContextFactory.cs   # Design-time factory (46 lines)
│   ├── DbContextExtensions.cs           # DI registration helpers (134 lines)
│   ├── QueryExtensions.cs               # Query helper methods (266 lines)
│   └── Configurations/
│       ├── LogEntryConfiguration.cs           # 142 lines
│       ├── JobConfiguration.cs                # 135 lines
│       ├── JobExecutionConfiguration.cs       # 114 lines
│       ├── ServerConfiguration.cs             # 113 lines
│       ├── AlertConfiguration.cs              # 118 lines
│       ├── AlertInstanceConfiguration.cs      # 101 lines
│       ├── UserConfiguration.cs               # 115 lines
│       ├── UserApiKeyConfiguration.cs         # 99 lines
│       ├── RefreshTokenConfiguration.cs       # 86 lines
│       ├── SystemConfigurationConfiguration.cs # 56 lines
│       ├── AuditLogEntryConfiguration.cs      # 82 lines
│       ├── DashboardCacheEntryConfiguration.cs # 51 lines
│       └── SchemaVersionConfiguration.cs      # 65 lines

Total: 17 files, 2,043 lines of code
```

## DbContext

### FMSLogNexusDbContext

Main database context with the following features:

#### DbSets

| DbSet | Entity | Schema.Table |
|-------|--------|--------------|
| `LogEntries` | LogEntry | [log].[LogEntries] |
| `Jobs` | Job | [job].[Jobs] |
| `JobExecutions` | JobExecution | [job].[JobExecutions] |
| `Servers` | Server | [job].[Servers] |
| `Alerts` | Alert | [alert].[Alerts] |
| `AlertInstances` | AlertInstance | [alert].[AlertInstances] |
| `Users` | User | [auth].[Users] |
| `UserApiKeys` | UserApiKey | [auth].[UserApiKeys] |
| `RefreshTokens` | RefreshToken | [auth].[RefreshTokens] |
| `SystemConfiguration` | SystemConfiguration | [system].[Configuration] |
| `AuditLog` | AuditLogEntry | [system].[AuditLog] |
| `DashboardCache` | DashboardCacheEntry | [system].[DashboardCache] |
| `SchemaVersions` | SchemaVersion | [system].[SchemaVersions] |

#### Automatic Audit Fields

The DbContext automatically updates audit fields on save:
- `CreatedAt` - Set to UTC now on insert
- `UpdatedAt` - Set to UTC now on insert and update
- `CreatedBy`/`UpdatedBy` - Set via `SaveChangesWithAuditAsync()`

```csharp
// Standard save (auto-sets timestamps)
await context.SaveChangesAsync();

// Save with audit user info
await context.SaveChangesWithAuditAsync(userId, username);
```

#### Enum Conversions

All enums are stored as integers:
- LogLevel (0-5)
- JobStatus (0-5)
- JobType (0-4)
- ServerStatus (0-3)
- AlertSeverity (0-3)
- AlertType (0-4)
- AlertInstanceStatus (0-3)
- UserRole (0-3)

## Entity Configurations

### Schema Mapping

| Schema | Tables | Purpose |
|--------|--------|---------|
| `log` | LogEntries | Log storage |
| `job` | Jobs, JobExecutions, Servers | Job management |
| `alert` | Alerts, AlertInstances | Alert management |
| `auth` | Users, UserApiKeys, RefreshTokens | Authentication |
| `system` | Configuration, AuditLog, DashboardCache, SchemaVersions | System management |

### Primary Keys

| Entity | Key Type | Column Name | Generation |
|--------|----------|-------------|------------|
| LogEntry | Guid | Id | NEWSEQUENTIALID() |
| Job | string | JobId | User-provided |
| JobExecution | Guid | Id | NEWSEQUENTIALID() |
| Server | string | ServerName | User-provided |
| Alert | Guid | Id | NEWSEQUENTIALID() |
| AlertInstance | Guid | Id | NEWSEQUENTIALID() |
| User | Guid | Id | NEWSEQUENTIALID() |
| UserApiKey | Guid | Id | NEWSEQUENTIALID() |
| RefreshToken | Guid | Id | NEWSEQUENTIALID() |
| SystemConfiguration | string | ConfigKey | User-provided |
| AuditLogEntry | Guid | Id | NEWSEQUENTIALID() |
| DashboardCacheEntry | string | CacheKey | User-provided |
| SchemaVersion | int | Id | Identity |

### Index Summary

#### LogEntries Indexes
- `IX_LogEntries_Timestamp` - Primary time-based queries
- `IX_LogEntries_Timestamp_Level` - Level filtering with time
- `IX_LogEntries_JobId_Timestamp` - Job-specific logs (filtered)
- `IX_LogEntries_JobExecutionId_Timestamp` - Execution logs (filtered)
- `IX_LogEntries_ServerName_Timestamp` - Server logs
- `IX_LogEntries_CorrelationId` - Correlation tracking (filtered)
- `IX_LogEntries_TraceId` - Distributed tracing (filtered)
- `IX_LogEntries_Level_Timestamp` - Error-only queries (filtered, Level >= 4)

#### Jobs Indexes
- `IX_Jobs_ServerName` - Server-based queries (filtered)
- `IX_Jobs_Category` - Category-based queries (filtered)
- `IX_Jobs_IsActive` - Active job filtering
- `IX_Jobs_IsCritical` - Critical job filtering (filtered)
- `IX_Jobs_LastStatus` - Status-based queries (filtered)
- `IX_Jobs_IsActive_LastStatus` - Combined filtering

#### Users Indexes
- `UQ_Users_Username` - Unique username
- `UQ_Users_Email` - Unique email
- `IX_Users_Role` - Role-based queries
- `IX_Users_IsActive` - Active user filtering
- `IX_Users_IsActive_Role` - Combined filtering
- `IX_Users_IsLocked` - Locked account queries (filtered)

### Relationships

```
Job (1) ──────────┬──── (Many) JobExecution
                  ├──── (Many) LogEntry
                  └──── (Many) Alert

Server (1) ───────┬──── (Many) Job
                  ├──── (Many) JobExecution
                  ├──── (Many) LogEntry
                  ├──── (Many) UserApiKey
                  └──── (Many) Alert

JobExecution (1) ─┴──── (Many) LogEntry

User (1) ─────────┬──── (Many) UserApiKey
                  └──── (Many) RefreshToken

Alert (1) ────────┴──── (Many) AlertInstance
```

### Delete Behaviors

| Relationship | Delete Behavior | Reason |
|--------------|-----------------|--------|
| Job → JobExecution | Cascade | Delete job removes all executions |
| Job → LogEntry | SetNull | Keep logs, clear job reference |
| Job → Alert | SetNull | Keep alerts, clear job reference |
| Server → Job | SetNull | Keep jobs, clear server reference |
| Server → JobExecution | Restrict | Prevent delete if executions exist |
| Server → LogEntry | Restrict | Prevent delete if logs exist |
| User → UserApiKey | Cascade | Delete user removes all API keys |
| User → RefreshToken | Cascade | Delete user removes all tokens |
| Alert → AlertInstance | Cascade | Delete alert removes all instances |

## Extension Methods

### DbContextExtensions

Registration helpers for dependency injection:

```csharp
// Basic registration
services.AddFMSLogNexusDbContext(
    connectionString,
    enableSensitiveDataLogging: true,  // Development only
    enableDetailedErrors: true);

// Pooled context for high performance
services.AddFMSLogNexusDbContextPool(connectionString, poolSize: 1024);

// Migration helpers
await serviceProvider.MigrateDatabaseAsync();
await serviceProvider.EnsureDatabaseCreatedAsync();
bool connected = await serviceProvider.TestDatabaseConnectionAsync();
```

### QueryExtensions

Common query patterns:

```csharp
// Pagination
var logs = await dbContext.LogEntries
    .OrderByDescending(l => l.Timestamp)
    .Paginate(page: 1, pageSize: 50)
    .ToListAsync();

// Conditional filtering
var jobs = await dbContext.Jobs
    .WhereIf(categoryFilter != null, j => j.Category == categoryFilter)
    .WhereIfNotEmpty(searchTerm, s => j => j.DisplayName.Contains(s))
    .WhereActive(activeOnly: true)
    .ToListAsync();

// Date range filtering
var executions = await dbContext.JobExecutions
    .WhereDateRange(e => e.StartedAt, fromDate, toDate)
    .ToListAsync();

// Combined count and paging
var (totalCount, items) = await dbContext.LogEntries
    .Where(l => l.Level >= LogLevel.Error)
    .ToPagedListAsync(page, pageSize);

// No-tracking for read-only queries
var servers = await dbContext.Servers
    .AsReadOnly()
    .ToListAsync();
```

## Usage Examples

### Basic CRUD Operations

```csharp
// Create
var job = new Job 
{ 
    Id = "daily-report",
    DisplayName = "Daily Report Generator",
    JobType = JobType.Scheduled
};
dbContext.Jobs.Add(job);
await dbContext.SaveChangesAsync();

// Read
var job = await dbContext.Jobs
    .Include(j => j.Executions.OrderByDescending(e => e.StartedAt).Take(5))
    .FirstOrDefaultAsync(j => j.Id == "daily-report");

// Update
job.DisplayName = "Updated Daily Report";
await dbContext.SaveChangesWithAuditAsync(userId, username);

// Delete (soft delete via IsActive)
job.IsActive = false;
await dbContext.SaveChangesAsync();
```

### Complex Queries

```csharp
// Get recent errors with job info
var recentErrors = await dbContext.LogEntries
    .AsReadOnly()
    .Where(l => l.Level >= LogLevel.Error)
    .Where(l => l.Timestamp >= DateTime.UtcNow.AddHours(-24))
    .Include(l => l.Job)
    .Include(l => l.JobExecution)
    .OrderByDescending(l => l.Timestamp)
    .Take(100)
    .ToListAsync();

// Get job health summary
var jobStats = await dbContext.Jobs
    .AsReadOnly()
    .Where(j => j.IsActive)
    .Select(j => new 
    {
        j.Id,
        j.DisplayName,
        j.LastStatus,
        j.SuccessRate,
        RecentExecutions = j.Executions
            .OrderByDescending(e => e.StartedAt)
            .Take(10)
            .ToList()
    })
    .ToListAsync();
```

## SQL Server Configuration

### Connection String

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=FMSLogNexus;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### Recommended Settings

- Command Timeout: 300 seconds (for large queries)
- Retry on Failure: 5 retries with 30-second delay
- Query Splitting: Enabled for complex includes
- Connection Pooling: 1024 connections (for pooled context)

## Migration Commands

```bash
# Add migration
dotnet ef migrations add InitialCreate -p src/FMSLogNexus.Infrastructure -s src/FMSLogNexus.Api

# Update database
dotnet ef database update -p src/FMSLogNexus.Infrastructure -s src/FMSLogNexus.Api

# Generate SQL script
dotnet ef migrations script -p src/FMSLogNexus.Infrastructure -s src/FMSLogNexus.Api -o migrations.sql
```

## Best Practices

1. **Use AsNoTracking()** for read-only queries
2. **Use Split Queries** for complex includes to avoid Cartesian products
3. **Apply Pagination** to all list queries
4. **Use Projections** (Select) instead of loading full entities when possible
5. **Avoid N+1** by using Include() for navigation properties
6. **Use Transactions** for multi-entity operations
7. **Dispose Context** properly (handled by DI container)

## Next Steps

- Step 13: Repository Implementations
- Step 14: Service Implementations
- Step 15: API Controllers
