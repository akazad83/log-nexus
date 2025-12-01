# FMS Log Nexus - API Reference

Complete API documentation for FMS Log Nexus RESTful API.

## Base URL

```
Production: https://api.yourdomain.com
Development: http://localhost:5000
```

## Authentication

All API endpoints (except login) require authentication using JWT Bearer tokens.

### Headers

```http
Authorization: Bearer <your-jwt-token>
Content-Type: application/json
```

---

## Table of Contents

1. [Authentication](#authentication-endpoints)
2. [Logs](#logs-endpoints)
3. [Jobs](#jobs-endpoints)
4. [Executions](#executions-endpoints)
5. [Servers](#servers-endpoints)
6. [Alerts](#alerts-endpoints)
7. [Users](#users-endpoints)
8. [Dashboard](#dashboard-endpoints)
9. [Error Handling](#error-handling)

---

## Authentication Endpoints

### Login

Authenticate user and receive JWT tokens.

```http
POST /api/auth/login
```

**Request Body:**
```json
{
  "username": "string",
  "password": "string"
}
```

**Response:** `200 OK`
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "expiresAt": "2024-01-15T12:00:00Z",
  "user": {
    "id": "guid",
    "username": "string",
    "email": "string",
    "role": "Admin"
  }
}
```

**Error Responses:**
- `400 Bad Request` - Invalid request body
- `401 Unauthorized` - Invalid credentials

---

### Refresh Token

Refresh an expired access token.

```http
POST /api/auth/refresh
```

**Request Body:**
```json
{
  "refreshToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

**Response:** `200 OK`
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "new-refresh-token",
  "expiresAt": "2024-01-15T12:00:00Z"
}
```

---

### Logout

Invalidate refresh token.

```http
POST /api/auth/logout
Authorization: Bearer <token>
```

**Response:** `204 No Content`

---

### Get Current User

Get authenticated user information.

```http
GET /api/auth/me
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "username": "string",
  "email": "string",
  "role": "Admin",
  "isActive": true,
  "lastLoginAt": "2024-01-15T10:00:00Z"
}
```

---

### Change Password

Change current user's password.

```http
POST /api/auth/change-password
Authorization: Bearer <token>
```

**Request Body:**
```json
{
  "currentPassword": "string",
  "newPassword": "string"
}
```

**Response:** `204 No Content`

---

## Logs Endpoints

### Create Log Entry

Create a single log entry.

```http
POST /api/logs
Authorization: Bearer <token> (Agent role)
```

**Request Body:**
```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "level": "Information",
  "message": "Job completed successfully",
  "serverName": "SERVER-01",
  "jobId": "DAILY-BACKUP",
  "executionId": "guid (optional)",
  "category": "string (optional)",
  "correlationId": "string (optional)",
  "exception": "string (optional)",
  "properties": { "key": "value" }
}
```

**Log Levels:** `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`

**Response:** `201 Created`
```json
{
  "id": "guid",
  "timestamp": "2024-01-15T10:30:00Z",
  "level": "Information",
  "message": "Job completed successfully",
  "serverName": "SERVER-01",
  "jobId": "DAILY-BACKUP",
  "createdAt": "2024-01-15T10:30:01Z"
}
```

---

### Create Batch Logs

Create multiple log entries at once.

```http
POST /api/logs/batch
Authorization: Bearer <token> (Agent role)
```

**Request Body:**
```json
[
  {
    "timestamp": "2024-01-15T10:30:00Z",
    "level": "Information",
    "message": "Starting job"
  },
  {
    "timestamp": "2024-01-15T10:30:05Z",
    "level": "Information",
    "message": "Processing data"
  }
]
```

**Response:** `200 OK`
```json
{
  "totalReceived": 2,
  "successCount": 2,
  "failedCount": 0,
  "errors": []
}
```

---

### Get Log by ID

Retrieve a specific log entry.

```http
GET /api/logs/{id}
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "timestamp": "2024-01-15T10:30:00Z",
  "level": "Error",
  "message": "Database connection failed",
  "serverName": "SERVER-01",
  "jobId": "DAILY-BACKUP",
  "executionId": "guid",
  "exception": "SqlException: Connection timeout",
  "properties": { "retryCount": 3 }
}
```

---

### Search Logs

Search and filter log entries with pagination.

```http
GET /api/logs?startTime=2024-01-15T00:00:00Z&endTime=2024-01-15T23:59:59Z&minLevel=Warning&serverName=SERVER-01&page=1&pageSize=50
Authorization: Bearer <token>
```

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| startTime | datetime | Filter from timestamp |
| endTime | datetime | Filter to timestamp |
| minLevel | LogLevel | Minimum log level |
| serverName | string | Filter by server |
| jobId | string | Filter by job |
| executionId | guid | Filter by execution |
| searchTerm | string | Full-text search |
| page | int | Page number (default: 1) |
| pageSize | int | Items per page (default: 50, max: 1000) |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "timestamp": "2024-01-15T10:30:00Z",
      "level": "Warning",
      "message": "High memory usage detected"
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalCount": 150,
  "totalPages": 3
}
```

---

### Get Log Statistics

Get aggregated log statistics.

```http
GET /api/logs/statistics?startTime=2024-01-15T00:00:00Z&endTime=2024-01-15T23:59:59Z&serverName=SERVER-01
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "totalLogs": 10000,
  "traceCount": 5000,
  "debugCount": 3000,
  "informationCount": 1500,
  "warningCount": 300,
  "errorCount": 180,
  "criticalCount": 20,
  "periodStart": "2024-01-15T00:00:00Z",
  "periodEnd": "2024-01-15T23:59:59Z"
}
```

---

### Get Recent Errors

Get recent error and critical logs.

```http
GET /api/logs/recent-errors?count=10
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "timestamp": "2024-01-15T10:30:00Z",
    "level": "Error",
    "message": "Job failed",
    "serverName": "SERVER-01",
    "jobId": "DAILY-BACKUP"
  }
]
```

---

## Jobs Endpoints

### Create Job

Register a new job definition.

```http
POST /api/jobs
Authorization: Bearer <token> (Admin/Operator)
```

**Request Body:**
```json
{
  "jobId": "DAILY-BACKUP",
  "displayName": "Daily Backup Job",
  "description": "Performs daily database backup",
  "serverName": "SERVER-01",
  "schedule": "0 0 * * *",
  "priority": "High",
  "timeoutMinutes": 60,
  "tags": ["backup", "database"]
}
```

**Priority Levels:** `Low`, `Normal`, `High`, `Critical`

**Response:** `201 Created`
```json
{
  "jobId": "DAILY-BACKUP",
  "displayName": "Daily Backup Job",
  "serverName": "SERVER-01",
  "priority": "High",
  "isActive": true,
  "createdAt": "2024-01-15T10:00:00Z"
}
```

---

### Get Job

Retrieve job details.

```http
GET /api/jobs/{jobId}
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "jobId": "DAILY-BACKUP",
  "displayName": "Daily Backup Job",
  "description": "Performs daily database backup",
  "serverName": "SERVER-01",
  "schedule": "0 0 * * *",
  "priority": "High",
  "timeoutMinutes": 60,
  "isActive": true,
  "lastExecutionAt": "2024-01-15T00:00:00Z",
  "lastExecutionStatus": "Success",
  "totalExecutions": 365,
  "successCount": 360,
  "failureCount": 5,
  "consecutiveFailures": 0,
  "averageDurationMs": 45000,
  "createdAt": "2023-01-01T00:00:00Z",
  "updatedAt": "2024-01-15T10:00:00Z"
}
```

---

### List Jobs

Get all jobs with optional filtering.

```http
GET /api/jobs?serverName=SERVER-01&activeOnly=true&page=1&pageSize=20
Authorization: Bearer <token>
```

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| serverName | string | Filter by server |
| activeOnly | bool | Only active jobs |
| searchTerm | string | Search in name/description |
| priority | Priority | Filter by priority |
| page | int | Page number |
| pageSize | int | Items per page |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "jobId": "DAILY-BACKUP",
      "displayName": "Daily Backup Job",
      "serverName": "SERVER-01",
      "priority": "High",
      "isActive": true
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 50,
  "totalPages": 3
}
```

---

### Update Job

Update job configuration.

```http
PUT /api/jobs/{jobId}
Authorization: Bearer <token> (Admin/Operator)
```

**Request Body:**
```json
{
  "displayName": "Updated Job Name",
  "description": "Updated description",
  "priority": "Critical",
  "timeoutMinutes": 120,
  "isActive": true
}
```

**Response:** `200 OK`

---

### Delete Job

Soft delete a job.

```http
DELETE /api/jobs/{jobId}
Authorization: Bearer <token> (Admin)
```

**Response:** `204 No Content`

---

### Get Job Health

Get job health score and metrics.

```http
GET /api/jobs/{jobId}/health
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "jobId": "DAILY-BACKUP",
  "healthScore": 95,
  "status": "Healthy",
  "lastExecutionStatus": "Success",
  "consecutiveFailures": 0,
  "averageDurationMs": 45000,
  "successRate": 0.98,
  "recentExecutions": [
    {
      "id": "guid",
      "status": "Success",
      "durationMs": 43000,
      "completedAt": "2024-01-15T00:45:00Z"
    }
  ],
  "recommendations": []
}
```

---

### Activate/Deactivate Job

```http
POST /api/jobs/{jobId}/activate
POST /api/jobs/{jobId}/deactivate
Authorization: Bearer <token> (Admin/Operator)
```

**Response:** `204 No Content`

---

## Executions Endpoints

### Start Execution

Start a new job execution.

```http
POST /api/executions
Authorization: Bearer <token> (Agent)
```

**Request Body:**
```json
{
  "jobId": "DAILY-BACKUP",
  "serverName": "SERVER-01",
  "triggerType": "Scheduled",
  "triggeredBy": "Scheduler",
  "parameters": { "fullBackup": true }
}
```

**Trigger Types:** `Manual`, `Scheduled`, `Triggered`, `Retry`

**Response:** `201 Created`
```json
{
  "id": "guid",
  "jobId": "DAILY-BACKUP",
  "serverName": "SERVER-01",
  "status": "Running",
  "startedAt": "2024-01-15T10:00:00Z",
  "triggerType": "Scheduled"
}
```

---

### Complete Execution

Mark execution as completed.

```http
PUT /api/executions/{id}/complete
Authorization: Bearer <token> (Agent)
```

**Request Body:**
```json
{
  "status": "Success",
  "errorMessage": null,
  "outputMessage": "Backup completed: 500MB"
}
```

**Status Values:** `Success`, `Failed`, `TimedOut`, `Cancelled`

**Response:** `200 OK`
```json
{
  "id": "guid",
  "status": "Success",
  "startedAt": "2024-01-15T10:00:00Z",
  "completedAt": "2024-01-15T10:45:00Z",
  "durationMs": 2700000
}
```

---

### Cancel Execution

Cancel a running execution.

```http
POST /api/executions/{id}/cancel
Authorization: Bearer <token> (Operator)
```

**Request Body:**
```json
{
  "reason": "User requested cancellation"
}
```

**Response:** `204 No Content`

---

### Get Execution

Get execution details.

```http
GET /api/executions/{id}
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "jobId": "DAILY-BACKUP",
  "serverName": "SERVER-01",
  "status": "Success",
  "startedAt": "2024-01-15T10:00:00Z",
  "completedAt": "2024-01-15T10:45:00Z",
  "durationMs": 2700000,
  "triggerType": "Scheduled",
  "outputMessage": "Backup completed: 500MB",
  "logCount": 150
}
```

---

### Get Running Executions

List all currently running executions.

```http
GET /api/executions/running
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "jobId": "DAILY-BACKUP",
    "serverName": "SERVER-01",
    "status": "Running",
    "startedAt": "2024-01-15T10:00:00Z",
    "elapsedMs": 300000
  }
]
```

---

### Get Execution History

Get job execution history.

```http
GET /api/executions/job/{jobId}?page=1&pageSize=20
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "status": "Success",
      "startedAt": "2024-01-15T10:00:00Z",
      "durationMs": 45000
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 365,
  "totalPages": 19
}
```

---

## Servers Endpoints

### Server Heartbeat

Send server heartbeat (registers server if new).

```http
POST /api/servers/heartbeat
Authorization: Bearer <token> (Agent)
```

**Request Body:**
```json
{
  "serverName": "SERVER-01",
  "status": "Online",
  "agentVersion": "1.2.3",
  "systemInfo": {
    "os": "Windows Server 2022",
    "cpuUsage": 45.5,
    "memoryUsage": 62.3
  }
}
```

**Server Status:** `Unknown`, `Online`, `Offline`, `Maintenance`, `Error`

**Response:** `200 OK`

---

### Get Server

Get server details.

```http
GET /api/servers/{serverName}
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "serverName": "SERVER-01",
  "displayName": "Production Server 1",
  "description": "Primary production server",
  "status": "Online",
  "agentVersion": "1.2.3",
  "lastHeartbeat": "2024-01-15T10:30:00Z",
  "isActive": true,
  "inMaintenance": false,
  "jobCount": 15,
  "runningJobCount": 2
}
```

---

### List Servers

Get all registered servers.

```http
GET /api/servers?activeOnly=true&status=Online
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
[
  {
    "serverName": "SERVER-01",
    "displayName": "Production Server 1",
    "status": "Online",
    "lastHeartbeat": "2024-01-15T10:30:00Z"
  }
]
```

---

### Server Status Summary

Get aggregated server status.

```http
GET /api/servers/status-summary
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "totalServers": 10,
  "onlineCount": 8,
  "offlineCount": 1,
  "maintenanceCount": 1,
  "errorCount": 0
}
```

---

### Set Maintenance Mode

```http
POST /api/servers/{serverName}/maintenance/enable
POST /api/servers/{serverName}/maintenance/disable
Authorization: Bearer <token> (Operator)
```

**Response:** `204 No Content`

---

## Alerts Endpoints

### Create Alert Rule

Create a new alert rule.

```http
POST /api/alerts
Authorization: Bearer <token> (Admin)
```

**Request Body:**
```json
{
  "name": "Job Failure Alert",
  "description": "Alert when any job fails",
  "alertType": "JobFailure",
  "severity": "High",
  "jobId": null,
  "serverName": null,
  "isActive": true,
  "cooldownMinutes": 15,
  "configuration": {
    "consecutiveFailures": 3
  }
}
```

**Alert Types:** `JobFailure`, `ConsecutiveFailures`, `ErrorThreshold`, `ServerOffline`, `JobTimeout`, `Custom`

**Severity:** `Low`, `Medium`, `High`, `Critical`

**Response:** `201 Created`
```json
{
  "id": "guid",
  "name": "Job Failure Alert",
  "alertType": "JobFailure",
  "severity": "High",
  "isActive": true
}
```

---

### Trigger Alert

Manually trigger an alert.

```http
POST /api/alerts/{id}/trigger
Authorization: Bearer <token>
```

**Request Body:**
```json
{
  "message": "Manual alert trigger",
  "jobId": "DAILY-BACKUP",
  "executionId": "guid",
  "metadata": { "key": "value" }
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "alertId": "guid",
  "status": "New",
  "message": "Manual alert trigger",
  "triggeredAt": "2024-01-15T10:30:00Z"
}
```

---

### Acknowledge Alert Instance

```http
POST /api/alerts/instances/{id}/acknowledge
Authorization: Bearer <token> (Operator)
```

**Request Body:**
```json
{
  "note": "Investigating the issue"
}
```

**Response:** `204 No Content`

---

### Resolve Alert Instance

```http
POST /api/alerts/instances/{id}/resolve
Authorization: Bearer <token> (Operator)
```

**Request Body:**
```json
{
  "note": "Issue has been fixed"
}
```

**Response:** `204 No Content`

---

### Get Active Alerts

```http
GET /api/alerts/instances/active
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "alertId": "guid",
    "alertName": "Job Failure Alert",
    "severity": "High",
    "status": "New",
    "message": "Job DAILY-BACKUP failed",
    "triggeredAt": "2024-01-15T10:30:00Z"
  }
]
```

---

## Users Endpoints

### Create User (Admin only)

```http
POST /api/users
Authorization: Bearer <token> (Admin)
```

**Request Body:**
```json
{
  "username": "newuser",
  "email": "newuser@example.com",
  "password": "SecurePass123!",
  "role": "Operator"
}
```

**Roles:** `Admin`, `Operator`, `Viewer`, `Agent`

**Response:** `201 Created`

---

### List Users

```http
GET /api/users
Authorization: Bearer <token> (Admin)
```

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "username": "admin",
    "email": "admin@example.com",
    "role": "Admin",
    "isActive": true
  }
]
```

---

## Dashboard Endpoints

### Get Dashboard Data

```http
GET /api/dashboard
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "serverSummary": {
    "total": 10,
    "online": 8,
    "offline": 1,
    "maintenance": 1
  },
  "executionSummary": {
    "running": 5,
    "completedToday": 150,
    "failedToday": 3
  },
  "alertSummary": {
    "active": 2,
    "critical": 0,
    "high": 1,
    "medium": 1
  },
  "recentLogs": [...],
  "recentAlerts": [...]
}
```

---

## Error Handling

### Error Response Format

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Validation failed",
  "instance": "/api/logs",
  "errors": {
    "Message": ["The Message field is required."]
  }
}
```

### HTTP Status Codes

| Code | Description |
|------|-------------|
| 200 | Success |
| 201 | Created |
| 204 | No Content |
| 400 | Bad Request |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | Not Found |
| 409 | Conflict |
| 422 | Unprocessable Entity |
| 500 | Internal Server Error |

---

## Rate Limiting

| Endpoint | Limit |
|----------|-------|
| `/api/auth/login` | 5 requests/minute |
| `/api/logs/batch` | 100 requests/minute |
| All other endpoints | 1000 requests/minute |

Rate limit headers:
```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1705320000
```

---

## Webhooks (Coming Soon)

Configure webhooks to receive notifications for:
- Alert triggered
- Job failed
- Server offline
- Execution completed

---

## SDK Examples

### C# Example

```csharp
var client = new FmsLogNexusClient("https://api.example.com", "your-token");

// Create log
await client.Logs.CreateAsync(new CreateLogRequest
{
    Timestamp = DateTime.UtcNow,
    Level = LogLevel.Information,
    Message = "Job started",
    JobId = "DAILY-BACKUP"
});

// Start execution
var execution = await client.Executions.StartAsync(new StartExecutionRequest
{
    JobId = "DAILY-BACKUP",
    ServerName = "SERVER-01",
    TriggerType = "Manual"
});
```

### PowerShell Example

```powershell
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Send heartbeat
$body = @{
    serverName = "SERVER-01"
    status = "Online"
    agentVersion = "1.0.0"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/servers/heartbeat" `
    -Method POST -Headers $headers -Body $body
```
