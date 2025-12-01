# FMS Log Nexus - Operations Guide

Guide for operating, monitoring, and maintaining FMS Log Nexus in production.

## Table of Contents

1. [Health Monitoring](#health-monitoring)
2. [Logging and Diagnostics](#logging-and-diagnostics)
3. [Performance Monitoring](#performance-monitoring)
4. [Backup and Recovery](#backup-and-recovery)
5. [Scaling](#scaling)
6. [Maintenance Tasks](#maintenance-tasks)
7. [Troubleshooting](#troubleshooting)
8. [Security Operations](#security-operations)

---

## Health Monitoring

### Health Check Endpoints

| Endpoint | Purpose | Response |
|----------|---------|----------|
| `/health` | Liveness check | 200 OK / 503 Unhealthy |
| `/health/ready` | Readiness check | 200 OK / 503 Not Ready |
| `/health/live` | Basic liveness | 200 OK |

### Health Check Response

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0500000",
      "data": {}
    },
    "redis": {
      "status": "Healthy",
      "duration": "00:00:00.0100000",
      "data": {}
    }
  }
}
```

### Monitoring with Scripts

```bash
# Simple health check
curl -f http://localhost:8080/health || echo "UNHEALTHY"

# Detailed health check
./docker/scripts/health-check.sh prod
```

### Prometheus Metrics

Key metrics to monitor:

| Metric | Description | Alert Threshold |
|--------|-------------|-----------------|
| `http_requests_total` | Total HTTP requests | - |
| `http_request_duration_seconds` | Request latency | P99 > 1s |
| `http_requests_in_progress` | Active requests | > 100 |
| `dotnet_gc_memory_total_available_bytes` | Available memory | < 100MB |
| `sqlserver_connection_pool_size` | DB connections | > 80% max |

### Grafana Dashboards

Pre-configured dashboards:

1. **API Overview** - Request rate, latency, errors
2. **Database Performance** - Query times, connections
3. **SignalR Connections** - Active connections, messages
4. **Background Services** - Task execution, failures
5. **Infrastructure** - CPU, memory, disk

---

## Logging and Diagnostics

### Log Locations

| Environment | Location |
|-------------|----------|
| Docker | `docker logs fms-lognexus-api` |
| File | `/var/log/fms-lognexus/` |
| Seq | http://localhost:8082 |

### Log Levels

```bash
# View logs with specific level
docker logs fms-lognexus-api 2>&1 | grep -E "\[ERR\]|\[FTL\]"
```

### Structured Log Queries (Seq)

```
# Find errors in last hour
@Timestamp > Now() - 1h and @Level = "Error"

# Find slow database queries
SourceContext = "Microsoft.EntityFrameworkCore" and ElapsedMs > 1000

# Find failed authentications
RequestPath = "/api/auth/login" and StatusCode = 401
```

### Request Tracing

Each request includes correlation headers:

```
X-Request-Id: abc123
X-Correlation-Id: xyz789
```

Use these to trace requests across services:

```
CorrelationId = "xyz789"
```

---

## Performance Monitoring

### Key Performance Indicators

| KPI | Target | Critical |
|-----|--------|----------|
| API Response Time (P50) | < 100ms | > 500ms |
| API Response Time (P99) | < 500ms | > 2s |
| Error Rate | < 0.1% | > 1% |
| Database Query Time | < 50ms | > 200ms |
| Log Ingestion Rate | 10,000/sec | < 1,000/sec |

### Performance Testing

```bash
# Load test with wrk
wrk -t12 -c400 -d30s http://localhost:8080/api/jobs

# Stress test with k6
k6 run scripts/load-test.js
```

### Database Performance

```sql
-- Check long-running queries
SELECT 
    r.session_id,
    r.start_time,
    r.status,
    r.command,
    t.text
FROM sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE r.total_elapsed_time > 5000;

-- Check index usage
SELECT 
    OBJECT_NAME(s.object_id) AS TableName,
    i.name AS IndexName,
    s.user_seeks,
    s.user_scans,
    s.user_lookups,
    s.user_updates
FROM sys.dm_db_index_usage_stats s
JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
WHERE database_id = DB_ID('FMSLogNexus')
ORDER BY s.user_seeks + s.user_scans DESC;
```

### Redis Performance

```bash
# Monitor Redis
docker exec fms-lognexus-redis redis-cli info stats

# Check memory usage
docker exec fms-lognexus-redis redis-cli info memory

# Monitor slow commands
docker exec fms-lognexus-redis redis-cli slowlog get 10
```

---

## Backup and Recovery

### Database Backup

**Automated Daily Backup:**

```bash
# Add to crontab
0 2 * * * /opt/fms-lognexus/docker/scripts/backup-db.sh daily_$(date +\%Y\%m\%d)
```

**Manual Backup:**

```bash
./docker/scripts/backup-db.sh manual_backup
```

**Backup Verification:**

```bash
# List backups
docker exec fms-lognexus-sqlserver ls -la /var/opt/mssql/backup/

# Verify backup
docker exec fms-lognexus-sqlserver /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C \
    -Q "RESTORE VERIFYONLY FROM DISK = '/var/opt/mssql/backup/backup.bak'"
```

### Database Recovery

```bash
# Restore from backup
./docker/scripts/restore-db.sh backup_20240115.bak
```

### Redis Backup

```bash
# Trigger RDB snapshot
docker exec fms-lognexus-redis redis-cli BGSAVE

# Copy backup file
docker cp fms-lognexus-redis:/data/dump.rdb ./backups/
```

### Disaster Recovery Checklist

- [ ] Database backup verified
- [ ] Redis snapshot available
- [ ] Configuration files backed up
- [ ] SSL certificates backed up
- [ ] Recovery procedure documented
- [ ] Recovery tested recently

---

## Scaling

### Horizontal Scaling (API)

```yaml
# Docker Compose
services:
  api:
    deploy:
      replicas: 3
```

```bash
# Kubernetes
kubectl scale deployment fms-lognexus-api --replicas=5 -n fms-lognexus
```

### SignalR Backplane (Required for Multiple Instances)

```csharp
// Enable Redis backplane
builder.Services.AddSignalR()
    .AddStackExchangeRedis(connectionString);
```

### Database Scaling

| Strategy | Use Case |
|----------|----------|
| Read Replicas | High read load |
| Connection Pooling | Many connections |
| Table Partitioning | Large log tables |
| Azure Elastic Pool | Cloud deployment |

### Caching Optimization

```bash
# Check cache hit rate
docker exec fms-lognexus-redis redis-cli info stats | grep hit
```

---

## Maintenance Tasks

### Routine Maintenance Schedule

| Task | Frequency | Command |
|------|-----------|---------|
| Database backup | Daily | `./scripts/backup-db.sh` |
| Log rotation | Daily | Automatic |
| Index maintenance | Weekly | SQL maintenance job |
| Docker cleanup | Weekly | `./scripts/cleanup.sh` |
| Security updates | Monthly | `docker-compose pull` |
| SSL renewal | Before expiry | Certbot or manual |

### Database Maintenance

```sql
-- Rebuild indexes
ALTER INDEX ALL ON LogEntries REBUILD;
ALTER INDEX ALL ON JobExecutions REBUILD;

-- Update statistics
UPDATE STATISTICS LogEntries;
UPDATE STATISTICS JobExecutions;

-- Shrink log file (if needed)
DBCC SHRINKFILE (FMSLogNexus_Log, 1024);
```

### Log Retention

Automatic log retention is handled by the LogRetentionService. Configure:

```json
{
  "BackgroundServices": {
    "LogRetentionDays": 90,
    "ExecutionRetentionDays": 365
  }
}
```

Manual cleanup:

```sql
-- Delete old logs
DELETE FROM LogEntries 
WHERE Timestamp < DATEADD(day, -90, GETUTCDATE());

-- Delete old executions
DELETE FROM JobExecutions 
WHERE CompletedAt < DATEADD(day, -365, GETUTCDATE());
```

---

## Troubleshooting

### Common Issues

#### API Not Starting

```bash
# Check logs
docker logs fms-lognexus-api

# Common causes:
# - Database connection failed
# - Invalid configuration
# - Port already in use
```

#### Database Connection Issues

```bash
# Test connection
docker exec fms-lognexus-sqlserver /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1"

# Check firewall
telnet sqlserver 1433
```

#### High Memory Usage

```bash
# Check container stats
docker stats fms-lognexus-api

# Force garbage collection (development only)
curl -X POST http://localhost:8080/debug/gc
```

#### Slow Queries

```sql
-- Find slow queries
SELECT TOP 10
    qs.execution_count,
    qs.total_elapsed_time / qs.execution_count AS avg_elapsed_time,
    SUBSTRING(st.text, 1, 100) AS query_text
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
ORDER BY avg_elapsed_time DESC;
```

#### SignalR Connection Issues

```javascript
// Enable SignalR logging
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/logs")
    .configureLogging(signalR.LogLevel.Debug)
    .build();
```

### Diagnostic Commands

```bash
# Container health
docker inspect --format='{{.State.Health.Status}}' fms-lognexus-api

# Network connectivity
docker exec fms-lognexus-api curl -f http://sqlserver:1433 || echo "Cannot reach SQL Server"

# Disk space
docker system df

# Process list
docker exec fms-lognexus-api ps aux
```

---

## Security Operations

### Security Checklist

- [ ] JWT secrets rotated quarterly
- [ ] Database passwords meet complexity requirements
- [ ] SSL certificates valid and not expiring
- [ ] API rate limiting enabled
- [ ] Failed login attempts monitored
- [ ] Security patches applied
- [ ] Access logs reviewed

### Audit Log Review

```sql
-- Recent admin actions
SELECT * FROM AuditLogs 
WHERE UserId IN (SELECT Id FROM Users WHERE Role = 'Admin')
AND CreatedAt > DATEADD(day, -7, GETUTCDATE())
ORDER BY CreatedAt DESC;

-- Failed login attempts
SELECT * FROM AuditLogs 
WHERE Action = 'LoginFailed'
AND CreatedAt > DATEADD(hour, -24, GETUTCDATE())
ORDER BY CreatedAt DESC;
```

### Security Incident Response

1. **Detect** - Monitor alerts and logs
2. **Contain** - Isolate affected systems
3. **Investigate** - Review audit logs
4. **Remediate** - Apply fixes
5. **Recover** - Restore normal operations
6. **Report** - Document incident

### Rotating Secrets

```bash
# Generate new JWT secret
openssl rand -base64 64

# Update secret
docker-compose -f docker-compose.prod.yml down
# Update .env with new secret
docker-compose -f docker-compose.prod.yml up -d

# Note: Users will need to re-authenticate
```

---

## Alerting

### Critical Alerts

| Alert | Condition | Action |
|-------|-----------|--------|
| API Down | Health check fails 3x | Page on-call |
| High Error Rate | > 1% errors | Page on-call |
| Database Down | Connection fails | Page on-call |
| Disk Full | > 90% usage | Alert team |
| Certificate Expiring | < 30 days | Alert team |

### Alert Configuration (Prometheus)

```yaml
groups:
  - name: fms-lognexus
    rules:
      - alert: APIHighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) / rate(http_requests_total[5m]) > 0.01
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: High error rate detected
```
