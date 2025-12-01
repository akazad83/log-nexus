# FMS Log Nexus - Architecture Guide

## Overview

FMS Log Nexus is built using Clean Architecture principles with a focus on maintainability, testability, and scalability. This document provides a comprehensive overview of the system architecture.

## Table of Contents

1. [System Architecture](#system-architecture)
2. [Project Structure](#project-structure)
3. [Layer Responsibilities](#layer-responsibilities)
4. [Data Flow](#data-flow)
5. [Database Design](#database-design)
6. [Real-time Communication](#real-time-communication)
7. [Background Services](#background-services)
8. [Security Architecture](#security-architecture)
9. [Scalability Considerations](#scalability-considerations)

---

## System Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           External Clients                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │  FMS Agents  │  │ Web Dashboard│  │ Mobile Apps  │  │  Webhooks    │ │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                        ┌───────────┴───────────┐
                        ▼                       ▼
                ┌───────────────┐       ┌───────────────┐
                │  Nginx/LB     │       │   CDN         │
                │  (Optional)   │       │  (Static)     │
                └───────────────┘       └───────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        FMS Log Nexus API                                │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                      Presentation Layer                            │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────┐ │ │
│  │  │ Controllers │  │  Middleware │  │   Filters   │  │  SignalR  │ │ │
│  │  │  (REST API) │  │ (Auth,Log)  │  │ (Validation)│  │   Hubs    │ │ │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └───────────┘ │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                      Application Layer                             │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────┐ │ │
│  │  │  Services   │  │  Validators │  │   DTOs      │  │ Mappers   │ │ │
│  │  │  (Business) │  │ (Fluent)    │  │ (Contracts) │  │ (AutoMap) │ │ │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └───────────┘ │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                        Domain Layer                                │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────┐ │ │
│  │  │  Entities   │  │   Enums     │  │ Interfaces  │  │Exceptions │ │ │
│  │  │  (Models)   │  │  (Types)    │  │ (Contracts) │  │ (Domain)  │ │ │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └───────────┘ │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                    Infrastructure Layer                            │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────┐ │ │
│  │  │Repositories │  │  DbContext  │  │   Caching   │  │ External  │ │ │
│  │  │   (EF Core) │  │ (SQL Server)│  │   (Redis)   │  │ Services  │ │ │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └───────────┘ │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
                        │           │           │
                        ▼           ▼           ▼
                ┌───────────┐ ┌───────────┐ ┌───────────┐
                │SQL Server │ │   Redis   │ │    Seq    │
                │ Database  │ │   Cache   │ │   Logs    │
                └───────────┘ └───────────┘ └───────────┘
```

### Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    FMS Log Nexus Components                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐        │
│  │   Logs      │    │    Jobs     │    │  Servers    │        │
│  │  Component  │    │  Component  │    │  Component  │        │
│  │             │    │             │    │             │        │
│  │ • LogCtrl   │    │ • JobCtrl   │    │ • SrvCtrl   │        │
│  │ • LogSvc    │    │ • JobSvc    │    │ • SrvSvc    │        │
│  │ • LogRepo   │    │ • JobRepo   │    │ • SrvRepo   │        │
│  └─────────────┘    └─────────────┘    └─────────────┘        │
│                                                                 │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐        │
│  │ Executions  │    │   Alerts    │    │    Auth     │        │
│  │  Component  │    │  Component  │    │  Component  │        │
│  │             │    │             │    │             │        │
│  │ • ExecCtrl  │    │ • AlertCtrl │    │ • AuthCtrl  │        │
│  │ • ExecSvc   │    │ • AlertSvc  │    │ • AuthSvc   │        │
│  │ • ExecRepo  │    │ • AlertRepo │    │ • UserRepo  │        │
│  └─────────────┘    └─────────────┘    └─────────────┘        │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │                  Background Services                     │  │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐   │  │
│  │  │Dashboard │ │ Server   │ │Execution │ │  Alert   │   │  │
│  │  │ Update   │ │ Health   │ │ Timeout  │ │Evaluation│   │  │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘   │  │
│  └─────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │                    SignalR Hubs                          │  │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐   │  │
│  │  │  Logs    │ │Dashboard │ │  Alerts  │ │Execution │   │  │
│  │  │   Hub    │ │   Hub    │ │   Hub    │ │   Hub    │   │  │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘   │  │
│  └─────────────────────────────────────────────────────────┘  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Project Structure

```
FMSLogNexus/
├── src/
│   ├── FMSLogNexus.Core/           # Domain & Application Layer
│   │   ├── Entities/               # Domain entities
│   │   ├── Enums/                  # Enumerations
│   │   ├── DTOs/                   # Data transfer objects
│   │   ├── Interfaces/             # Repository & service interfaces
│   │   │   ├── Repositories/       # Repository contracts
│   │   │   └── Services/           # Service contracts
│   │   └── Exceptions/             # Domain exceptions
│   │
│   ├── FMSLogNexus.Infrastructure/ # Infrastructure Layer
│   │   ├── Data/                   # Database context & configurations
│   │   │   ├── Configurations/     # Entity configurations
│   │   │   └── FMSLogNexusDbContext.cs
│   │   ├── Repositories/           # Repository implementations
│   │   └── Services/               # Service implementations
│   │
│   └── FMSLogNexus.Api/            # Presentation Layer
│       ├── Controllers/            # API controllers
│       ├── Hubs/                   # SignalR hubs
│       ├── BackgroundServices/     # Hosted services
│       ├── Middleware/             # Custom middleware
│       └── Program.cs              # Application entry point
│
├── tests/
│   ├── FMSLogNexus.Tests.Unit/     # Unit tests
│   └── FMSLogNexus.Tests.Integration/ # Integration tests
│
├── docker/                         # Docker configuration
│   ├── Dockerfile
│   ├── docker-compose.yml
│   └── ...
│
└── docs/                           # Documentation
    ├── ARCHITECTURE.md
    ├── API.md
    └── ...
```

---

## Layer Responsibilities

### Core Layer (Domain + Application)

**Entities** - Domain models representing business concepts:
- `LogEntry` - Individual log records
- `Job` - Job definitions
- `JobExecution` - Job execution instances
- `Server` - Server registrations
- `AlertRule` - Alert definitions
- `AlertInstance` - Triggered alerts
- `User` - System users

**DTOs** - Data transfer objects for API communication:
- Request DTOs (create/update operations)
- Response DTOs (API responses)
- Query DTOs (search/filter parameters)

**Interfaces** - Contracts for dependency inversion:
- Repository interfaces (`ILogRepository`, `IJobRepository`, etc.)
- Service interfaces (`ILogService`, `IJobService`, etc.)

### Infrastructure Layer

**Repositories** - Data access implementation:
- Entity Framework Core for SQL Server
- Optimized queries with projections
- Pagination and filtering support

**Services** - Business logic implementation:
- Input validation
- Business rule enforcement
- Cross-cutting concerns

**Data** - Database configuration:
- DbContext with entity configurations
- Index definitions
- Relationship mappings

### Presentation Layer (API)

**Controllers** - HTTP endpoints:
- RESTful API design
- Request/response handling
- Authorization enforcement

**Hubs** - Real-time communication:
- SignalR hubs for WebSocket connections
- Group-based message distribution
- Connection lifecycle management

**Background Services** - Scheduled tasks:
- Dashboard updates
- Health checks
- Alert evaluation

---

## Data Flow

### Log Ingestion Flow

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  FMS Agent  │────▶│  Log Ctrl   │────▶│  Log Svc    │────▶│  Log Repo   │
│             │     │             │     │             │     │             │
│ POST /logs  │     │ Validate    │     │ Business    │     │ EF Core     │
│             │     │ Authorize   │     │ Logic       │     │ SQL Server  │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
                                               │
                                               ▼
                                        ┌─────────────┐
                                        │  SignalR    │
                                        │  Broadcast  │
                                        └─────────────┘
```

### Job Execution Flow

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│ Start       │────▶│ Create      │────▶│ Broadcast   │
│ Execution   │     │ Execution   │     │ Start Event │
└─────────────┘     └─────────────┘     └─────────────┘
       │                                       │
       ▼                                       ▼
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│ Log Events  │────▶│ Process     │────▶│ Real-time   │
│ (Multiple)  │     │ Logs        │     │ Updates     │
└─────────────┘     └─────────────┘     └─────────────┘
       │                                       │
       ▼                                       ▼
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│ Complete    │────▶│ Update Job  │────▶│ Evaluate    │
│ Execution   │     │ Statistics  │     │ Alerts      │
└─────────────┘     └─────────────┘     └─────────────┘
```

---

## Database Design

### Entity Relationship Diagram

```
┌─────────────────┐       ┌─────────────────┐
│     Server      │───────│      Job        │
├─────────────────┤   1:N ├─────────────────┤
│ ServerName (PK) │       │ JobId (PK)      │
│ DisplayName     │       │ ServerName (FK) │
│ Status          │       │ DisplayName     │
│ LastHeartbeat   │       │ Priority        │
│ IsActive        │       │ IsActive        │
└─────────────────┘       └─────────────────┘
                                  │
                                  │ 1:N
                                  ▼
                          ┌─────────────────┐
                          │  JobExecution   │
                          ├─────────────────┤
                          │ Id (PK)         │
                          │ JobId (FK)      │
                          │ Status          │
                          │ StartedAt       │
                          │ CompletedAt     │
                          └─────────────────┘
                                  │
                                  │ 1:N
                                  ▼
┌─────────────────┐       ┌─────────────────┐
│   AlertRule     │───────│   LogEntry      │
├─────────────────┤   ?:N ├─────────────────┤
│ Id (PK)         │       │ Id (PK)         │
│ Name            │       │ ExecutionId(FK) │
│ AlertType       │       │ Level           │
│ Severity        │       │ Message         │
│ JobId (FK)      │       │ Timestamp       │
└─────────────────┘       └─────────────────┘
        │
        │ 1:N
        ▼
┌─────────────────┐
│  AlertInstance  │
├─────────────────┤
│ Id (PK)         │
│ AlertId (FK)    │
│ Status          │
│ TriggeredAt     │
└─────────────────┘
```

### Key Indexes

| Table | Index | Columns | Purpose |
|-------|-------|---------|---------|
| LogEntries | IX_Timestamp | Timestamp DESC | Time-based queries |
| LogEntries | IX_Level_Timestamp | Level, Timestamp | Error filtering |
| LogEntries | IX_JobId_ExecutionId | JobId, ExecutionId | Execution logs |
| JobExecutions | IX_JobId_Status | JobId, Status | Job history |
| AlertInstances | IX_Status | Status | Active alerts |

---

## Real-time Communication

### SignalR Hub Architecture

```
┌────────────────────────────────────────────────────────────┐
│                      SignalR Hubs                          │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ┌──────────────┐    Methods:                             │
│  │   LogHub     │    • SubscribeToServer(serverName)      │
│  │              │    • SubscribeToJob(jobId)              │
│  │              │    • UnsubscribeFromServer(serverName)  │
│  └──────────────┘    Events:                              │
│                      • LogReceived                         │
│                      • BatchLogsReceived                   │
│                                                            │
│  ┌──────────────┐    Methods:                             │
│  │ DashboardHub │    • SubscribeToDashboard()             │
│  │              │    Events:                              │
│  │              │    • DashboardUpdate                    │
│  └──────────────┘    • StatisticsUpdate                   │
│                                                            │
│  ┌──────────────┐    Methods:                             │
│  │  AlertHub    │    • SubscribeToAlerts()                │
│  │              │    Events:                              │
│  │              │    • AlertTriggered                     │
│  └──────────────┘    • AlertAcknowledged                  │
│                      • AlertResolved                       │
│                                                            │
│  ┌──────────────┐    Methods:                             │
│  │ExecutionHub  │    • SubscribeToExecution(executionId)  │
│  │              │    Events:                              │
│  │              │    • ExecutionStarted                   │
│  └──────────────┘    • ExecutionCompleted                 │
│                      • ExecutionProgress                   │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

### Message Broadcasting

```csharp
// Group-based broadcasting
await _hubContext.Clients.Group($"server_{serverName}")
    .SendAsync("LogReceived", logEntry);

// All clients
await _hubContext.Clients.All
    .SendAsync("DashboardUpdate", dashboardData);
```

---

## Background Services

### Service Overview

| Service | Interval | Purpose |
|---------|----------|---------|
| DashboardUpdateService | 5 sec | Broadcast dashboard metrics |
| ServerHealthCheckService | 30 sec | Check server heartbeats |
| ExecutionTimeoutService | 60 sec | Timeout stale executions |
| AlertEvaluationService | 30 sec | Evaluate alert conditions |
| MaintenanceService | 60 min | Database cleanup |
| LogRetentionService | 24 hr | Archive/delete old logs |

### Service Execution Flow

```
┌─────────────────────────────────────────────────────────────┐
│                  Background Service                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    │
│  │   Timer     │───▶│  Execute    │───▶│  Broadcast  │    │
│  │  Triggers   │    │   Task      │    │   Results   │    │
│  └─────────────┘    └─────────────┘    └─────────────┘    │
│        │                  │                  │             │
│        │                  ▼                  │             │
│        │           ┌─────────────┐           │             │
│        │           │   Scoped    │           │             │
│        │           │  Services   │           │             │
│        │           └─────────────┘           │             │
│        │                                     │             │
│        └──────────────────wait───────────────┘             │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Security Architecture

### Authentication Flow

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Client    │────▶│  /login     │────▶│  Validate   │
│             │     │  Endpoint   │     │ Credentials │
└─────────────┘     └─────────────┘     └─────────────┘
                                               │
                          ┌────────────────────┘
                          ▼
                   ┌─────────────┐
                   │  Generate   │
                   │  JWT Token  │
                   └─────────────┘
                          │
                          ▼
┌─────────────┐     ┌─────────────┐
│   Client    │◀────│  Return     │
│  Stores     │     │  Token      │
│  Token      │     └─────────────┘
└─────────────┘
```

### Role-Based Access Control

| Role | Permissions |
|------|-------------|
| Admin | Full access to all resources |
| Operator | Manage jobs, executions, alerts |
| Viewer | Read-only access |
| Agent | Log submission, heartbeat |

### Authorization Matrix

| Resource | Admin | Operator | Viewer | Agent |
|----------|-------|----------|--------|-------|
| Logs (Create) | ✅ | ✅ | ❌ | ✅ |
| Logs (Read) | ✅ | ✅ | ✅ | ❌ |
| Jobs (CRUD) | ✅ | ✅ | ❌ | ❌ |
| Jobs (Read) | ✅ | ✅ | ✅ | ❌ |
| Alerts (Manage) | ✅ | ✅ | ❌ | ❌ |
| Users (Manage) | ✅ | ❌ | ❌ | ❌ |

---

## Scalability Considerations

### Horizontal Scaling

```
                    ┌─────────────────┐
                    │  Load Balancer  │
                    └─────────────────┘
                            │
            ┌───────────────┼───────────────┐
            ▼               ▼               ▼
     ┌───────────┐   ┌───────────┐   ┌───────────┐
     │  API #1   │   │  API #2   │   │  API #3   │
     └───────────┘   └───────────┘   └───────────┘
            │               │               │
            └───────────────┼───────────────┘
                            ▼
                    ┌───────────────┐
                    │ Redis (SignalR│
                    │   Backplane)  │
                    └───────────────┘
                            │
                            ▼
                    ┌───────────────┐
                    │  SQL Server   │
                    │  (Primary)    │
                    └───────────────┘
```

### Caching Strategy

| Cache Type | TTL | Purpose |
|------------|-----|---------|
| Job Details | 5 min | Reduce DB queries |
| Server List | 1 min | Quick lookups |
| Statistics | 30 sec | Dashboard data |
| User Sessions | 30 min | Auth tokens |

### Performance Optimizations

1. **Batch Operations**: Bulk log insertion
2. **Async Processing**: Non-blocking I/O
3. **Connection Pooling**: SQL Server connections
4. **Response Compression**: Gzip/Brotli
5. **Query Optimization**: Indexed queries, projections
