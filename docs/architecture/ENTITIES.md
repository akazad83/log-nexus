# FMS Log Nexus - Domain Entities

## Entity Overview

| Entity | Namespace | Database Table | Description |
|--------|-----------|----------------|-------------|
| `LogEntry` | FMSLogNexus.Core.Entities | [log].[LogEntries] | Individual log entries |
| `Job` | FMSLogNexus.Core.Entities | [job].[Jobs] | Job definitions |
| `JobExecution` | FMSLogNexus.Core.Entities | [job].[JobExecutions] | Job execution instances |
| `Server` | FMSLogNexus.Core.Entities | [job].[Servers] | Server/machine registrations |
| `Alert` | FMSLogNexus.Core.Entities | [alert].[Alerts] | Alert rule definitions |
| `AlertInstance` | FMSLogNexus.Core.Entities | [alert].[AlertInstances] | Triggered alert instances |
| `User` | FMSLogNexus.Core.Entities | [auth].[Users] | System users |
| `UserApiKey` | FMSLogNexus.Core.Entities | [auth].[UserApiKeys] | API keys for services |
| `RefreshToken` | FMSLogNexus.Core.Entities | [auth].[RefreshTokens] | JWT refresh tokens |
| `SystemConfiguration` | FMSLogNexus.Core.Entities | [system].[Configuration] | Key-value settings |
| `AuditLogEntry` | FMSLogNexus.Core.Entities | [system].[AuditLog] | Audit trail entries |
| `DashboardCacheEntry` | FMSLogNexus.Core.Entities | [system].[DashboardCache] | Cached statistics |
| `SchemaVersion` | FMSLogNexus.Core.Entities | [system].[SchemaVersion] | Migration tracking |

---

## Entity Relationships

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              ENTITY RELATIONSHIPS                            │
└─────────────────────────────────────────────────────────────────────────────┘

                                    ┌───────────┐
                                    │   User    │
                                    └─────┬─────┘
                          ┌───────────────┼───────────────┐
                          ▼               ▼               ▼
                   ┌───────────┐   ┌───────────────┐ ┌───────────────┐
                   │ ApiKey    │   │ RefreshToken  │ │  AuditLog     │
                   └───────────┘   └───────────────┘ └───────────────┘
                          │
                          ▼
                   ┌───────────┐           ┌───────────┐
                   │  Server   │◄──────────│   Job     │
                   └─────┬─────┘           └─────┬─────┘
                         │                       │
         ┌───────────────┼───────────────────────┼───────────────┐
         ▼               ▼                       ▼               ▼
  ┌───────────┐   ┌─────────────┐         ┌───────────────┐ ┌─────────┐
  │  Alert    │   │JobExecution │         │   LogEntry    │ │  Alert  │
  └─────┬─────┘   └──────┬──────┘         └───────────────┘ └─────────┘
        │                │                        ▲
        ▼                └────────────────────────┘
  ┌─────────────┐
  │AlertInstance│
  └─────────────┘
```

---

## Base Classes & Interfaces

### EntityBase
```csharp
public abstract class EntityBase : IEntity<Guid>
{
    public Guid Id { get; set; }
}
```

### AuditableEntity
```csharp
public abstract class AuditableEntity : EntityBase, IModificationAuditable
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
```

### Interfaces
- `IEntity` - Marker interface for all entities
- `IEntity<TKey>` - Entity with typed primary key
- `ICreationAuditable` - Tracks CreatedAt/CreatedBy
- `IModificationAuditable` - Tracks UpdatedAt/UpdatedBy
- `ISoftDeletable` - Supports soft delete
- `IActivatable` - Supports IsActive flag

---

## Entity Details

### LogEntry
Primary storage for all log messages.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | Guid | Primary key |
| `Timestamp` | DateTime | When log was generated |
| `Level` | LogLevel | Trace/Debug/Info/Warning/Error/Critical |
| `Message` | string | Log message text |
| `JobId` | string? | Associated job ID |
| `JobExecutionId` | Guid? | Associated execution ID |
| `ServerName` | string | Origin server |
| `ExceptionType` | string? | Exception type if any |
| `Properties` | string? | JSON structured data |

**Key Methods:**
- `SetException(Exception)` - Populates exception fields
- `GetPropertiesAsDictionary()` - Parses JSON properties
- `GetTagsList()` - Parses comma-separated tags

---

### Job
Job definition with execution statistics.

| Property | Type | Description |
|----------|------|-------------|
| `Id` / `JobId` | string | Primary key |
| `DisplayName` | string | Human-readable name |
| `JobType` | JobType | Executable/PowerShell/VBScript/etc |
| `ServerName` | string? | Default server |
| `Schedule` | string? | Cron expression |
| `TotalExecutions` | int | Total run count |
| `SuccessCount` | int | Successful runs |
| `FailureCount` | int | Failed runs |
| `HealthScore` | int | Calculated 0-100 |

**Key Methods:**
- `UpdateStatistics(JobExecution)` - Updates stats after execution

---

### JobExecution
Individual job run instance.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | Guid | Primary key |
| `JobId` | string | Parent job |
| `StartedAt` | DateTime | Start time |
| `CompletedAt` | DateTime? | End time |
| `Status` | JobStatus | Pending/Running/Completed/Failed |
| `DurationMs` | long? | Calculated duration |
| `ErrorCount` | int | Error log count |

**Key Methods:**
- `Start()` - Sets status to Running
- `Complete(resultSummary)` - Marks successful
- `Fail(message, category)` - Marks failed
- `IncrementLogCount(level)` - Updates log counts

---

### Server
Registered server/machine.

| Property | Type | Description |
|----------|------|-------------|
| `Id` / `ServerName` | string | Primary key |
| `DisplayName` | string | Human-readable name |
| `Status` | ServerStatus | Online/Offline/Degraded |
| `LastHeartbeat` | DateTime? | Last heartbeat time |
| `HeartbeatIntervalSeconds` | int | Expected interval |

**Key Methods:**
- `UpdateHeartbeat()` - Records heartbeat
- `CalculatedStatus` - Status based on heartbeat timing

---

### Alert
Alert rule definition.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | Guid | Primary key |
| `Name` | string | Alert name |
| `AlertType` | AlertType | ErrorThreshold/JobFailure/ServerOffline |
| `Severity` | AlertSeverity | Low/Medium/High/Critical |
| `Condition` | string | JSON condition config |
| `ThrottleMinutes` | int | Minimum time between triggers |

**Key Methods:**
- `Trigger(message, context)` - Creates AlertInstance
- `CanTrigger` - Checks throttle status

---

### AlertInstance
Triggered alert.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | Guid | Primary key |
| `AlertId` | Guid | Parent alert |
| `TriggeredAt` | DateTime | Trigger time |
| `Status` | AlertInstanceStatus | New/Acknowledged/Resolved |
| `AcknowledgedAt` | DateTime? | When acknowledged |
| `ResolvedAt` | DateTime? | When resolved |

**Key Methods:**
- `Acknowledge(username, note)` - Marks acknowledged
- `Resolve(username, note)` - Marks resolved

---

### User
System user account.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | Guid | Primary key |
| `Username` | string | Login name |
| `Email` | string | Email address |
| `PasswordHash` | string | BCrypt hash |
| `Role` | UserRole | Viewer/Operator/Admin |
| `IsLocked` | bool | Account locked |

**Key Methods:**
- `RecordSuccessfulLogin()` - Updates login info
- `RecordFailedLogin()` - Increments failures, may lock
- `ChangePassword(hash)` - Updates password and security stamp

---

### UserApiKey
API key for service authentication.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | Guid | Primary key |
| `UserId` | Guid | Owner user |
| `KeyHash` | string | SHA256 hash |
| `KeyPrefix` | string | First 8 chars |
| `Scopes` | string? | Comma-separated permissions |
| `ExpiresAt` | DateTime? | Expiration time |

**Key Methods:**
- `RecordUsage(ip)` - Updates usage tracking
- `Revoke(by, reason)` - Revokes the key
- `HasScope(scope)` - Checks permissions

---

## File List

| File | Size | Description |
|------|------|-------------|
| `EntityBase.cs` | 3.4 KB | Base classes and interfaces |
| `LogEntry.cs` | 6.7 KB | Log entry entity |
| `Job.cs` | 7.2 KB | Job definition entity |
| `JobExecution.cs` | 9.2 KB | Job execution entity |
| `Server.cs` | 6.4 KB | Server entity |
| `Alert.cs` | 7.1 KB | Alert definition entity |
| `AlertInstance.cs` | 8.5 KB | Alert instance entity |
| `User.cs` | 6.9 KB | User entity |
| `UserApiKey.cs` | 5.4 KB | API key entity |
| `RefreshToken.cs` | 4.7 KB | Refresh token entity |
| `SystemEntities.cs` | 12.1 KB | System configuration entities |

---

## Usage Examples

### Creating a Log Entry
```csharp
var log = new LogEntry
{
    Level = LogLevel.Information,
    Message = "Processing completed successfully",
    JobId = "IMPORT-001",
    JobExecutionId = executionId,
    ServerName = Environment.MachineName,
    Properties = JsonSerializer.Serialize(new { RecordCount = 150 })
};
```

### Starting a Job Execution
```csharp
var execution = new JobExecution
{
    JobId = "IMPORT-001",
    ServerName = "JOBSRV01",
    TriggerType = "Scheduled",
    CorrelationId = Guid.NewGuid().ToString("N")[..12]
};
execution.Start();
```

### Completing a Job Execution
```csharp
if (success)
{
    execution.Complete(new { RecordsProcessed = 1500 });
}
else
{
    execution.Fail("Database connection failed", "DatabaseError");
}

job.UpdateStatistics(execution);
```

### Triggering an Alert
```csharp
if (alert.CanTrigger)
{
    var instance = alert.Trigger(
        $"Error threshold exceeded: {errorCount} errors in last 60 minutes",
        new Dictionary<string, object>
        {
            ["errorCount"] = errorCount,
            ["threshold"] = 10
        });
    
    // Save instance and send notifications
}
```
