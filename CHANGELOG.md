# Changelog

All notable changes to FMS Log Nexus will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- Webhook notifications for alerts
- Email notification integration
- Azure Service Bus integration
- Kubernetes operator
- GraphQL API support

---

## [1.0.0] - 2024-01-15

### Added

#### Core Features
- Centralized log aggregation from multiple FMS servers
- Job registration and management
- Job execution tracking and monitoring
- Server registration with heartbeat monitoring
- Alert rule configuration and management
- Real-time dashboard updates via SignalR

#### API
- RESTful API with 90+ endpoints
- JWT-based authentication
- Role-based access control (Admin, Operator, Viewer, Agent)
- API key authentication for agents
- Swagger/OpenAPI documentation
- Request rate limiting
- Response compression

#### Real-time Features
- SignalR hubs for logs, dashboard, alerts, and executions
- Live log streaming
- Real-time dashboard metrics
- Instant alert notifications
- WebSocket connection management

#### Background Services
- Dashboard update service (5-second intervals)
- Server health check service
- Execution timeout detection
- Alert evaluation service
- Database maintenance service
- Log retention service

#### Database
- SQL Server support
- Entity Framework Core with optimized queries
- Comprehensive indexing strategy
- Audit logging
- Data retention policies

#### Infrastructure
- Docker containerization
- Docker Compose for development and production
- Nginx reverse proxy configuration
- Redis caching support
- Prometheus metrics
- Grafana dashboards
- Seq log aggregation

#### Testing
- 128 unit tests
- 125 integration tests
- Test coverage reporting

#### Documentation
- Architecture guide
- API reference
- Deployment guide
- Configuration guide
- Operations guide
- Development guide

### Security
- JWT token authentication
- Refresh token rotation
- Password hashing with BCrypt
- Rate limiting on login endpoints
- CORS configuration
- HTTPS enforcement
- Security headers

---

## [0.9.0] - 2024-01-01 (Beta)

### Added
- Initial beta release
- Core logging functionality
- Basic job management
- Server registration
- Simple alerting

### Known Issues
- SignalR reconnection issues under high load
- Dashboard updates may lag during heavy log ingestion
- Limited error messages for validation failures

---

## Migration Guides

### Migrating from 0.9.x to 1.0.0

#### Breaking Changes

1. **API Endpoint Changes**
   - `/api/log` renamed to `/api/logs`
   - `/api/job` renamed to `/api/jobs`
   - Pagination parameters standardized (`page`, `pageSize`)

2. **Authentication Changes**
   - JWT token format updated
   - New `Agent` role for FMS agents
   - API keys now required for agents

3. **Configuration Changes**
   - `Logging` section replaced with `Serilog`
   - Background service intervals now in seconds
   - Redis connection string format changed

#### Migration Steps

1. **Update Configuration**
   ```json
   // Old
   "BackgroundServices": {
     "DashboardUpdateIntervalMs": 5000
   }
   
   // New
   "BackgroundServices": {
     "DashboardUpdateIntervalSeconds": 5
   }
   ```

2. **Update API Calls**
   ```bash
   # Old
   POST /api/log
   
   # New
   POST /api/logs
   ```

3. **Update FMS Agents**
   - Generate API keys for each agent
   - Update agent configuration with new endpoints
   - Test connectivity before production deployment

4. **Run Database Migrations**
   ```bash
   dotnet ef database update
   ```

---

## Version History

| Version | Release Date | .NET Version | Status |
|---------|--------------|--------------|--------|
| 1.0.0 | 2024-01-15 | .NET 8.0 | Current |
| 0.9.0 | 2024-01-01 | .NET 8.0 | Deprecated |

---

## Links

- [GitHub Repository](https://github.com/your-org/fms-log-nexus)
- [Documentation](https://docs.fms-lognexus.io)
- [Issue Tracker](https://github.com/your-org/fms-log-nexus/issues)
- [Releases](https://github.com/your-org/fms-log-nexus/releases)
