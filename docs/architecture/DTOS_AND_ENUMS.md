# FMS Log Nexus - DTOs and Enums Reference

## Overview

This document describes all Data Transfer Objects (DTOs) and Enumeration types used in the FMS Log Nexus API.

---

## Enumeration Types

### LogLevel
Log severity levels matching industry conventions.

| Value | Name | Description |
|-------|------|-------------|
| 0 | Trace | Most detailed logging |
| 1 | Debug | Debugging information |
| 2 | Information | General operational info |
| 3 | Warning | Potential problems |
| 4 | Error | Errors that allow continuation |
| 5 | Critical | Critical errors requiring attention |

### JobStatus
Job execution status values.

| Value | Name | Description |
|-------|------|-------------|
| 0 | Pending | Queued but not started |
| 1 | Running | Currently executing |
| 2 | Completed | Finished successfully |
| 3 | Failed | Failed with error |
| 4 | Cancelled | Manually cancelled |
| 5 | Timeout | Exceeded max duration |
| 6 | Warning | Completed with warnings |

### JobType
Types of jobs in the system.

| Value | Name | Description |
|-------|------|-------------|
| 0 | Unknown | Not specified |
| 1 | Executable | Windows .exe |
| 2 | PowerShell | PowerShell .ps1 script |
| 3 | VBScript | VBScript .vbs |
| 4 | BatchFile | .bat/.cmd file |
| 5 | SqlAgent | SQL Server Agent job |
| 6 | ScheduledTask | Windows Task Scheduler |
| 7 | WindowsService | Windows Service |
| 8 | DotNet | .NET application |
| 9 | SSIS | SSIS package |
| 99 | Other | Custom/other type |

### ServerStatus
Server health status values.

| Value | Name | Description |
|-------|------|-------------|
| 0 | Unknown | No heartbeat received |
| 1 | Online | Healthy and responding |
| 2 | Offline | Heartbeat timeout exceeded |
| 3 | Degraded | Heartbeat delayed |
| 4 | Maintenance | In maintenance mode |

### AlertSeverity
Alert priority levels.

| Value | Name | Color |
|-------|------|-------|
| 0 | Low | Gray (#6c757d) |
| 1 | Medium | Yellow (#ffc107) |
| 2 | High | Orange (#fd7e14) |
| 3 | Critical | Red (#dc3545) |

### AlertType
Types of alert rules.

| Value | Name | Description |
|-------|------|-------------|
| 0 | ErrorThreshold | Error count exceeds threshold |
| 1 | JobFailure | Job execution failed |
| 2 | ServerOffline | Server stopped responding |
| 3 | PerformanceWarning | Performance issue detected |
| 4 | CustomQuery | Based on SQL query |
| 5 | PatternMatch | Log message pattern match |
| 6 | DurationExceeded | Job ran too long |
| 7 | MissedSchedule | Job didn't run as expected |

### AlertInstanceStatus
Alert instance lifecycle.

| Value | Name | Description |
|-------|------|-------------|
| 0 | New | Unacknowledged |
| 1 | Acknowledged | Reviewed but not resolved |
| 2 | Resolved | Issue resolved |
| 3 | Suppressed | Intentionally ignored |

### UserRole
Authorization roles.

| Value | Name | Permissions |
|-------|------|-------------|
| 0 | Viewer | Read-only access |
| 1 | Operator | Acknowledge alerts, basic ops |
| 2 | Administrator | Full access |

### Other Enums
- **SortOrder**: Ascending (0), Descending (1)
- **TimePeriod**: LastHour, Last6Hours, Last24Hours, Last7Days, Last30Days, Custom
- **TrendInterval**: Minute, Hour, Day
- **ExportFormat**: Json, Csv, Excel
- **HealthStatus**: Unknown, Healthy, Warning, Critical

---

## Common DTOs

### PaginationRequest
Base class for paginated queries.

```csharp
{
    "page": 1,              // 1-based page number
    "pageSize": 50,         // Items per page (1-1000)
    "sortBy": "timestamp",  // Field to sort by
    "sortOrder": "Descending"
}
```

### PaginatedResult<T>
Standard paginated response wrapper.

```csharp
{
    "items": [...],
    "page": 1,
    "pageSize": 50,
    "totalCount": 1234,
    "totalPages": 25,
    "hasNextPage": true,
    "hasPreviousPage": false
}
```

### ApiError
Error response structure.

```csharp
{
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred",
    "details": [
        { "field": "JobId", "message": "Job ID is required" }
    ],
    "traceId": "abc123"
}
```

---

## Request DTOs

### Authentication

| DTO | Endpoint | Purpose |
|-----|----------|---------|
| `LoginRequest` | POST /api/auth/login | Login with credentials |
| `RefreshTokenRequest` | POST /api/auth/refresh | Refresh access token |
| `ChangePasswordRequest` | POST /api/auth/change-password | Change password |
| `CreateUserRequest` | POST /api/users | Create new user |
| `CreateApiKeyRequest` | POST /api/auth/apikeys | Create API key |

### Logs

| DTO | Endpoint | Purpose |
|-----|----------|---------|
| `CreateLogRequest` | POST /api/logs | Create single log |
| `BatchLogRequest` | POST /api/logs/batch | Create multiple logs (max 1000) |
| `LogSearchRequest` | GET /api/logs | Search/filter logs |
| `LogExportRequest` | GET /api/logs/export | Export logs |
| `LogStatisticsRequest` | GET /api/logs/statistics | Get log statistics |

### Jobs

| DTO | Endpoint | Purpose |
|-----|----------|---------|
| `CreateJobRequest` | POST /api/jobs | Register new job |
| `UpdateJobRequest` | PUT /api/jobs/{id} | Update job |
| `StartExecutionRequest` | POST /api/jobs/{id}/executions | Start execution |
| `CompleteExecutionRequest` | PUT /api/jobs/executions/{id} | Complete execution |
| `JobSearchRequest` | GET /api/jobs | Search jobs |
| `ExecutionSearchRequest` | GET /api/jobs/{id}/executions | Search executions |

### Alerts

| DTO | Endpoint | Purpose |
|-----|----------|---------|
| `CreateAlertRequest` | POST /api/alerts | Create alert rule |
| `UpdateAlertRequest` | PUT /api/alerts/{id} | Update alert |
| `AcknowledgeAlertRequest` | POST /api/alerts/instances/{id}/acknowledge | Acknowledge alert |
| `ResolveAlertRequest` | POST /api/alerts/instances/{id}/resolve | Resolve alert |
| `AlertInstanceSearchRequest` | GET /api/alerts/instances | Search alert instances |

### System

| DTO | Endpoint | Purpose |
|-----|----------|---------|
| `ServerHeartbeatRequest` | POST /api/system/servers/heartbeat | Update heartbeat |
| `CreateServerRequest` | POST /api/system/servers | Register server |
| `UpdateConfigurationRequest` | PUT /api/system/config | Update settings |
| `UpdatePreferencesRequest` | PUT /api/auth/me/preferences | Update user prefs |

---

## Response DTOs

### Authentication Responses

| DTO | Description |
|-----|-------------|
| `LoginResponse` | Access token, refresh token, user info |
| `RefreshTokenResponse` | New access/refresh tokens |
| `UserResponse` | User profile information |
| `ApiKeyResponse` | API key details (key only on creation) |

### Log Responses

| DTO | Description |
|-----|-------------|
| `LogEntryResponse` | Single log entry |
| `LogEntryDetailResponse` | Log with related logs |
| `LogIngestionResult` | ID and received timestamp |
| `BatchLogResult` | Accepted/rejected counts |
| `LogStatisticsResponse` | Summary, trends, exceptions |

### Job Responses

| DTO | Description |
|-----|-------------|
| `JobListItem` | Job summary for lists |
| `JobDetailResponse` | Full job with stats and recent executions |
| `ExecutionListItem` | Execution summary |
| `ExecutionDetailResponse` | Full execution details |
| `StartExecutionResult` | Execution ID and correlation ID |
| `RunningJobInfo` | Currently running job status |

### Alert Responses

| DTO | Description |
|-----|-------------|
| `AlertListItem` | Alert rule summary |
| `AlertDetailResponse` | Full alert with recent instances |
| `AlertInstanceListItem` | Alert instance summary |
| `AlertInstanceDetailResponse` | Full instance with notifications |
| `AlertSummary` | Count breakdown by status |

### Dashboard Responses

| DTO | Description |
|-----|-------------|
| `DashboardSummaryResponse` | Main dashboard statistics |
| `DashboardTrendsResponse` | Log volume trends |
| `JobHealthOverviewResponse` | Job health with failures |
| `ServerStatusResponse` | Server health and metrics |
| `SystemHealthResponse` | System component health |

---

## Usage Examples

### Creating a Log Entry

```json
POST /api/logs
{
    "level": 2,
    "message": "Processing completed: 150 records",
    "jobId": "IMPORT-001",
    "jobExecutionId": "a1b2c3d4-...",
    "serverName": "JOBSRV01",
    "properties": {
        "recordCount": 150,
        "duration": 1234
    },
    "tags": ["import", "customer"]
}
```

### Starting a Job Execution

```json
POST /api/jobs/IMPORT-001/executions
{
    "serverName": "JOBSRV01",
    "triggerType": "Scheduled",
    "triggeredBy": "SQL Agent",
    "parameters": {
        "inputFile": "data.csv"
    }
}
```

Response:
```json
{
    "executionId": "a1b2c3d4-...",
    "correlationId": "abc123def456",
    "startedAt": "2024-01-15T10:30:00Z"
}
```

### Creating an Alert

```json
POST /api/alerts
{
    "name": "High Error Rate",
    "description": "Alert when errors exceed 10 per hour",
    "alertType": 0,
    "severity": 2,
    "condition": {
        "errorThreshold": {
            "threshold": 10,
            "windowMinutes": 60,
            "level": 4
        }
    },
    "throttleMinutes": 30,
    "notificationChannels": {
        "email": ["ops@company.com"],
        "dashboard": true
    }
}
```

---

## File Structure

```
src/FMSLogNexus.Core/
├── DTOs/
│   ├── Common.cs              # Shared DTOs (pagination, errors)
│   ├── Requests/
│   │   ├── AuthRequests.cs    # Authentication requests
│   │   ├── LogRequests.cs     # Log requests
│   │   ├── JobRequests.cs     # Job requests
│   │   ├── AlertRequests.cs   # Alert requests
│   │   └── SystemRequests.cs  # System requests
│   └── Responses/
│       ├── AuthResponses.cs      # Auth responses
│       ├── LogResponses.cs       # Log responses
│       ├── JobResponses.cs       # Job responses
│       ├── AlertResponses.cs     # Alert responses
│       └── DashboardResponses.cs # Dashboard responses
└── Enums/
    └── Enums.cs               # All enumeration types
```
