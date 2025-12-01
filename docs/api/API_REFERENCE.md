# FMS Log Nexus - API Reference Summary

## Base URL
```
https://lognexus.company.com/api
```

## Authentication

| Method | Header | Use Case |
|--------|--------|----------|
| JWT Bearer | `Authorization: Bearer {token}` | Dashboard users |
| API Key | `X-API-Key: {key}` | Service accounts, jobs |

---

## Endpoints Quick Reference

### Authentication (`/auth`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/auth/login` | Login with credentials |
| POST | `/auth/refresh` | Refresh access token |
| POST | `/auth/logout` | Revoke refresh token |
| GET | `/auth/me` | Get current user |

### Logs (`/logs`)
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/logs` | Ingest single log | API Key |
| POST | `/logs/batch` | Ingest batch (≤1000) | API Key |
| GET | `/logs` | Search logs | JWT |
| GET | `/logs/{id}` | Get log details | JWT |
| GET | `/logs/export` | Export logs | JWT |
| GET | `/logs/statistics` | Get statistics | JWT |

### Jobs (`/jobs`)
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/jobs` | List jobs | JWT |
| POST | `/jobs` | Create job | JWT/API Key |
| GET | `/jobs/{jobId}` | Get job details | JWT |
| PUT | `/jobs/{jobId}` | Update job | JWT |
| DELETE | `/jobs/{jobId}` | Deactivate job | JWT (Admin) |
| GET | `/jobs/{jobId}/executions` | Execution history | JWT |
| POST | `/jobs/{jobId}/executions` | Start execution | API Key |
| GET | `/jobs/executions/{id}` | Execution details | JWT |
| PUT | `/jobs/executions/{id}` | Complete execution | API Key |
| GET | `/jobs/executions/{id}/logs` | Execution logs | JWT |

### Dashboard (`/dashboard`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/dashboard/summary` | Main statistics |
| GET | `/dashboard/trends` | Log volume trends |
| GET | `/dashboard/jobs/health` | Job health overview |
| GET | `/dashboard/servers/status` | Server status |
| GET | `/dashboard/recent-errors` | Recent errors |
| GET | `/dashboard/running-jobs` | Running executions |

### Alerts (`/alerts`)
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/alerts` | List alert definitions | JWT |
| POST | `/alerts` | Create alert | JWT (Admin) |
| GET | `/alerts/{id}` | Get alert details | JWT |
| PUT | `/alerts/{id}` | Update alert | JWT (Admin) |
| DELETE | `/alerts/{id}` | Delete alert | JWT (Admin) |
| GET | `/alerts/instances` | List instances | JWT |
| POST | `/alerts/instances/{id}/acknowledge` | Acknowledge | JWT |
| POST | `/alerts/instances/{id}/resolve` | Resolve | JWT |

### System (`/system`)
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/system/health` | Health check | None |
| GET | `/system/health/ready` | Readiness probe | None |
| GET | `/system/health/live` | Liveness probe | None |
| GET | `/system/servers` | List servers | JWT |
| POST | `/system/servers/heartbeat` | Server heartbeat | API Key |
| GET | `/system/config` | System config | JWT (Admin) |
| PUT | `/system/config` | Update config | JWT (Admin) |

---

## Common Response Codes

| Code | Description |
|------|-------------|
| 200 | Success |
| 201 | Created |
| 202 | Accepted (async) |
| 204 | No Content |
| 400 | Bad Request |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | Not Found |
| 429 | Rate Limited |
| 500 | Server Error |

---

## Pagination

**Request:**
```
?page=1&pageSize=50&sortBy=timestamp&sortOrder=desc
```

**Response:**
```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalPages": 10,
    "totalCount": 487,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

---

## SignalR Hubs

| Hub | URL | Description |
|-----|-----|-------------|
| LogHub | `/hubs/logs` | Real-time log streaming |
| DashboardHub | `/hubs/dashboard` | Dashboard updates |
| AlertHub | `/hubs/alerts` | Alert notifications |

---

## Rate Limits

| Endpoint Type | Limit |
|---------------|-------|
| Log Ingestion | 10,000 req/min per API key |
| Dashboard Queries | 1,000 req/min per user |
| Batch Ingestion | 100 req/min per API key |

---

## Example Requests

### Login
```bash
curl -X POST https://lognexus.company.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "SecurePassword123!"}'
```

### Ingest Log
```bash
curl -X POST https://lognexus.company.com/api/logs \
  -H "X-API-Key: your-api-key" \
  -H "Content-Type: application/json" \
  -d '{
    "timestamp": "2024-01-15T14:32:45.123Z",
    "level": "Information",
    "message": "Job started",
    "jobId": "IMPORT-001",
    "serverName": "JOBSRV01"
  }'
```

### Search Logs
```bash
curl -X GET "https://lognexus.company.com/api/logs?jobId=IMPORT-001&minLevel=Warning&pageSize=100" \
  -H "Authorization: Bearer {token}"
```

### Start Execution
```bash
curl -X POST https://lognexus.company.com/api/jobs/IMPORT-001/executions \
  -H "X-API-Key: your-api-key" \
  -H "Content-Type: application/json" \
  -d '{
    "serverName": "JOBSRV01",
    "triggerType": "Scheduled",
    "parameters": {"date": "2024-01-15"}
  }'
```

### Complete Execution
```bash
curl -X PUT https://lognexus.company.com/api/jobs/executions/{executionId} \
  -H "X-API-Key: your-api-key" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "Completed",
    "resultSummary": {"recordsProcessed": 1500}
  }'
```

---

## OpenAPI Specification

The complete OpenAPI 3.0 specification is available at:
- **YAML**: `/docs/api/openapi.yaml`
- **Swagger UI**: `https://lognexus.company.com/swagger`

Import the OpenAPI spec into tools like Postman, Insomnia, or Swagger UI for interactive API exploration.
