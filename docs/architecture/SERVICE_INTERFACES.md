# FMS Log Nexus - Service Interfaces Reference

## Overview

This document describes the service interfaces that define business logic contracts for the FMS Log Nexus application. Services sit between API controllers and repositories, implementing business rules, validation, and orchestration.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     API Controllers                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Services Layer                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ Domain      │  │ Auth        │  │ Infrastructure      │  │
│  │ Services    │  │ Services    │  │ Services            │  │
│  │             │  │             │  │                     │  │
│  │ • LogService│  │ • AuthSvc   │  │ • NotificationSvc   │  │
│  │ • JobService│  │ • UserSvc   │  │ • CacheService      │  │
│  │ • AlertSvc  │  │ • ApiKeySvc │  │ • HealthService     │  │
│  │ • ServerSvc │  │ • TokenSvc  │  │ • MaintenanceSvc    │  │
│  │ • Dashboard │  │             │  │ • ConfigService     │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Repository Layer                          │
└─────────────────────────────────────────────────────────────┘
```

---

## Domain Services

### ILogService

Handles log ingestion, searching, and statistics.

| Category | Methods |
|----------|---------|
| **Ingestion** | `CreateLogAsync`, `CreateBatchAsync`, `IngestStreamAsync` |
| **Query** | `GetByIdAsync`, `GetDetailAsync`, `SearchAsync`, `GetByCorrelationIdAsync`, `GetByTraceIdAsync`, `GetByJobExecutionAsync`, `GetRecentErrorsAsync` |
| **Statistics** | `GetStatisticsAsync`, `GetTrendAsync`, `GetCountsByLevelAsync` |
| **Lookups** | `GetDistinctServersAsync`, `GetDistinctJobsAsync`, `GetDistinctCategoriesAsync`, `GetDistinctTagsAsync` |
| **Export** | `ExportAsync`, `ExportToStreamAsync` |

### IJobService

Handles job registration, execution tracking, and health monitoring.

| Category | Methods |
|----------|---------|
| **Job CRUD** | `CreateJobAsync`, `GetJobAsync`, `UpdateJobAsync`, `DeleteJobAsync`, `SearchJobsAsync` |
| **Job Actions** | `ActivateJobAsync`, `DeactivateJobAsync` |
| **Executions** | `StartExecutionAsync`, `CompleteExecutionAsync`, `GetExecutionAsync`, `SearchExecutionsAsync`, `GetJobExecutionsAsync` |
| **Running** | `GetRunningExecutionsAsync`, `GetRecentFailuresAsync` |
| **Health** | `GetHealthOverviewAsync`, `CalculateHealthScoreAsync`, `GetJobStatisticsAsync`, `GetUnhealthyJobsAsync` |
| **Lookups** | `GetJobLookupAsync`, `GetCategoriesAsync`, `GetTagsAsync`, `JobExistsAsync` |

### IServerService

Handles server registration, heartbeats, and status monitoring.

| Category | Methods |
|----------|---------|
| **Server CRUD** | `CreateServerAsync`, `GetServerAsync`, `UpdateServerAsync`, `DeleteServerAsync`, `GetAllServersAsync` |
| **Heartbeat** | `ProcessHeartbeatAsync`, `CheckAndMarkOfflineServersAsync` |
| **Status** | `GetStatusSummaryAsync`, `GetServersByStatusAsync`, `GetUnresponsiveServersAsync` |
| **Actions** | `ActivateServerAsync`, `DeactivateServerAsync`, `SetMaintenanceModeAsync` |
| **Lookups** | `GetServerLookupAsync`, `ServerExistsAsync` |

### IAlertService

Handles alert rules, instances, and evaluation.

| Category | Methods |
|----------|---------|
| **Alert Rules** | `CreateAlertAsync`, `GetAlertAsync`, `UpdateAlertAsync`, `DeleteAlertAsync`, `GetAlertsAsync` |
| **Rule Actions** | `ActivateAlertAsync`, `DeactivateAlertAsync` |
| **Instances** | `GetInstanceAsync`, `SearchInstancesAsync`, `GetActiveInstancesAsync`, `GetUnacknowledgedInstancesAsync` |
| **Instance Lifecycle** | `AcknowledgeInstanceAsync`, `ResolveInstanceAsync`, `SuppressInstanceAsync` |
| **Bulk** | `BulkAcknowledgeAsync`, `BulkResolveAsync` |
| **Evaluation** | `EvaluateAlertsAsync`, `TriggerAlertAsync` |
| **Statistics** | `GetSummaryAsync`, `GetCountsByStatusAsync`, `GetCountsBySeverityAsync` |

### IDashboardService

Provides aggregated data for the UI dashboard.

| Category | Methods |
|----------|---------|
| **Summary** | `GetSummaryAsync`, `GetLogStatsAsync`, `GetJobStatsAsync`, `GetServerStatsAsync`, `GetAlertStatsAsync` |
| **Trends** | `GetLogTrendsAsync` |
| **Lists** | `GetRecentErrorsAsync`, `GetRunningJobsAsync`, `GetRecentFailuresAsync`, `GetActiveAlertsAsync`, `GetServerStatusesAsync`, `GetUnhealthyJobsAsync` |
| **Health** | `GetSystemHealthAsync` |
| **Real-time** | `GetUpdatesAsync` |

---

## Authentication Services

### IAuthService

Handles authentication and session management.

| Method | Description |
|--------|-------------|
| `LoginAsync` | Authenticate with username/password |
| `RefreshTokenAsync` | Refresh access token |
| `LogoutAsync` | Revoke refresh token |
| `LogoutAllSessionsAsync` | Revoke all user sessions |
| `ChangePasswordAsync` | Change user password |
| `ResetPasswordAsync` | Reset password (admin) |
| `AuthenticateApiKeyAsync` | Authenticate with API key |
| `ValidateTokenAsync` | Validate JWT token |
| `GetCurrentUserAsync` | Get current user from token |

### IUserService

Handles user management.

| Category | Methods |
|----------|---------|
| **CRUD** | `CreateUserAsync`, `GetUserAsync`, `GetUserByUsernameAsync`, `UpdateUserAsync`, `DeleteUserAsync`, `SearchUsersAsync` |
| **Actions** | `ActivateUserAsync`, `DeactivateUserAsync`, `UnlockUserAsync` |
| **Preferences** | `GetPreferencesAsync`, `UpdatePreferencesAsync` |
| **Validation** | `IsUsernameAvailableAsync`, `IsEmailAvailableAsync` |

### IApiKeyService

Handles API key management.

| Method | Description |
|--------|-------------|
| `CreateApiKeyAsync` | Create new API key |
| `GetApiKeyAsync` | Get API key details |
| `GetUserApiKeysAsync` | Get user's API keys |
| `RevokeApiKeyAsync` | Revoke API key |
| `RevokeAllUserApiKeysAsync` | Revoke all user's keys |
| `GetExpiringApiKeysAsync` | Get expiring keys |

### ITokenService

Handles JWT token operations.

| Category | Methods |
|----------|---------|
| **Generation** | `GenerateAccessToken`, `GenerateRefreshToken`, `GenerateApiKey` |
| **Validation** | `ValidateToken`, `ValidateAndExtract`, `GetUserIdFromToken`, `GetSecurityStampFromToken` |
| **Hashing** | `HashToken`, `HashApiKey`, `GetApiKeyPrefix` |

### IPasswordService

Handles password operations.

| Method | Description |
|--------|-------------|
| `HashPassword` | BCrypt hash password |
| `VerifyPassword` | Verify password against hash |
| `ValidatePassword` | Check password complexity |

---

## Infrastructure Services

### INotificationService

Handles sending notifications.

| Category | Methods |
|----------|---------|
| **Alert** | `SendAlertNotificationAsync` |
| **Email** | `SendEmailAsync`, `SendTemplatedEmailAsync` |
| **Webhook** | `SendWebhookAsync`, `SendWebhooksAsync` |
| **Dashboard** | `PushDashboardNotificationAsync` |

### ICacheService

Provides caching abstraction.

| Category | Methods |
|----------|---------|
| **Basic** | `GetAsync`, `SetAsync`, `GetOrSetAsync`, `RemoveAsync`, `RemoveByPatternAsync`, `ExistsAsync` |
| **Bulk** | `GetManyAsync`, `SetManyAsync` |
| **Management** | `ClearAsync`, `GetStatisticsAsync` |

### IHealthService

Handles system health checks.

| Method | Description |
|--------|-------------|
| `GetHealthAsync` | Get overall system health |
| `CheckDatabaseAsync` | Check database connectivity |
| `CheckExternalServicesAsync` | Check external services |
| `GetResourceStatsAsync` | Get resource usage |
| `IsAlive` | Liveness check |
| `IsReadyAsync` | Readiness check |

### IMaintenanceService

Handles maintenance operations.

| Category | Methods |
|----------|---------|
| **Logs** | `PurgeOldLogsAsync`, `ArchiveOldLogsAsync` |
| **Executions** | `PurgeOldExecutionsAsync`, `TimeoutStuckExecutionsAsync` |
| **Alerts** | `PurgeOldAlertsAsync` |
| **Tokens** | `PurgeExpiredTokensAsync`, `PurgeOldApiKeysAsync` |
| **Audit** | `PurgeOldAuditLogsAsync` |
| **Cache** | `ClearExpiredCacheAsync`, `ClearAllCacheAsync` |
| **Database** | `RunDatabaseMaintenanceAsync`, `GetDatabaseSizeAsync` |
| **Full** | `RunAllMaintenanceAsync` |

### IConfigurationService

Handles system configuration.

| Category | Methods |
|----------|---------|
| **Get** | `GetValueAsync`, `GetValueAsync<T>`, `GetAllConfigurationAsync`, `GetByCategoryAsync` |
| **Set** | `SetValueAsync`, `UpdateConfigurationAsync` |
| **Standard** | `GetLogRetentionDaysAsync`, `GetExecutionRetentionDaysAsync`, `GetAlertRetentionDaysAsync`, etc. |
| **Cache** | `RefreshCacheAsync` |

### IAuditService

Handles audit logging.

| Method | Description |
|--------|-------------|
| `LogCreateAsync` | Log entity creation |
| `LogUpdateAsync` | Log entity update |
| `LogDeleteAsync` | Log entity deletion |
| `LogLoginAsync` | Log login attempt |
| `LogActionAsync` | Log custom action |

### IRealTimeService

Handles SignalR real-time notifications.

| Category | Methods |
|----------|---------|
| **Dashboard** | `BroadcastDashboardStatsAsync`, `BroadcastLogStatsAsync` |
| **Logs** | `BroadcastNewLogAsync`, `BroadcastNewErrorAsync`, `SendJobLogAsync`, `SendExecutionLogAsync` |
| **Jobs** | `BroadcastExecutionStartedAsync`, `BroadcastExecutionCompletedAsync`, `BroadcastExecutionProgressAsync`, `BroadcastRunningJobsAsync` |
| **Alerts** | `BroadcastNewAlertAsync`, `BroadcastAlertAcknowledgedAsync`, `BroadcastAlertResolvedAsync` |
| **Servers** | `BroadcastServerStatusChangeAsync`, `BroadcastServerHeartbeatAsync` |
| **Notifications** | `SendUserNotificationAsync`, `BroadcastNotificationAsync` |

---

## Result Types

### AuthResult<T>

Authentication operation result wrapper.

```csharp
public class AuthResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### TokenValidationResult

JWT token validation result.

```csharp
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public UserRole? Role { get; set; }
    public string? ErrorCode { get; set; }
}
```

### ApiKeyAuthResult

API key authentication result.

```csharp
public class ApiKeyAuthResult
{
    public Guid UserId { get; set; }
    public string Username { get; set; }
    public UserRole Role { get; set; }
    public Guid ApiKeyId { get; set; }
    public IReadOnlyList<string> Scopes { get; set; }
}
```

---

## Usage Examples

### Log Ingestion

```csharp
public class LogsController : ControllerBase
{
    private readonly ILogService _logService;

    [HttpPost]
    public async Task<IActionResult> CreateLog([FromBody] CreateLogRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _logService.CreateLogAsync(request, clientIp);
        return Ok(result);
    }

    [HttpPost("batch")]
    public async Task<IActionResult> CreateBatch([FromBody] BatchLogRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _logService.CreateBatchAsync(request, clientIp);
        return Ok(result);
    }
}
```

### Authentication

```csharp
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();
        
        var result = await _authService.LoginAsync(request, ip, userAgent);
        
        if (!result.Success)
            return Unauthorized(new { result.ErrorCode, result.ErrorMessage });
        
        return Ok(result.Data);
    }
}
```

### Dashboard with Caching

```csharp
public class DashboardService : IDashboardService
{
    private readonly ICacheService _cache;
    private readonly ILogService _logService;

    public async Task<DashboardSummaryResponse> GetSummaryAsync(
        DashboardRequest request,
        CancellationToken ct)
    {
        var cacheKey = $"dashboard:summary:{request.Period}";
        
        return await _cache.GetOrSetAsync(
            cacheKey,
            async _ => await BuildSummaryAsync(request, ct),
            TimeSpan.FromSeconds(30),
            ct);
    }
}
```

---

## File Structure

```
src/FMSLogNexus.Core/Interfaces/Services/
├── ILogService.cs          # Log operations
├── IJobService.cs          # Job and execution operations
├── IServerService.cs       # Server management
├── IAlertService.cs        # Alert management
├── IDashboardService.cs    # Dashboard aggregation
├── IAuthService.cs         # Auth, User, ApiKey services
├── ITokenService.cs        # Token, Password services
├── INotificationService.cs # Notifications
├── ICacheService.cs        # Caching
├── ISystemServices.cs      # Health, Maintenance, Config, Audit
└── IRealTimeService.cs     # SignalR real-time
```

---

## Design Principles

| Principle | Implementation |
|-----------|----------------|
| **Single Responsibility** | Each service handles one domain area |
| **Dependency Inversion** | Services depend on abstractions (interfaces) |
| **Async First** | All I/O operations are asynchronous |
| **Cancellation Support** | All operations accept CancellationToken |
| **Result Types** | Operations return typed results, not exceptions |
| **Testability** | Interfaces enable easy mocking |
