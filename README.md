# FMS Log Nexus

<p align="center">
  <img src="docs/images/logo-placeholder.png" alt="FMS Log Nexus Logo" width="200"/>
</p>

<p align="center">
  <strong>Enterprise-Grade Centralized Logging and Job Monitoring System</strong>
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#quick-start">Quick Start</a> •
  <a href="#documentation">Documentation</a> •
  <a href="#api-reference">API Reference</a> •
  <a href="#deployment">Deployment</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 8.0"/>
  <img src="https://img.shields.io/badge/SQL%20Server-2022-CC2927?style=flat-square&logo=microsoftsqlserver" alt="SQL Server"/>
  <img src="https://img.shields.io/badge/Redis-7.0-DC382D?style=flat-square&logo=redis" alt="Redis"/>
  <img src="https://img.shields.io/badge/Docker-Ready-2496ED?style=flat-square&logo=docker" alt="Docker"/>
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="License"/>
</p>

---

## 📋 Overview

**FMS Log Nexus** is a comprehensive, enterprise-grade centralized logging and job monitoring system designed to aggregate logs from multiple FMS (Fleet Management System) servers, track job executions, and provide real-time alerting capabilities.

### Why FMS Log Nexus?

- **Centralized Visibility**: Single pane of glass for all your job logs across multiple servers
- **Real-time Monitoring**: WebSocket-powered live updates for dashboards and alerts
- **Intelligent Alerting**: Configurable alerts for job failures, error thresholds, and server issues
- **Scalable Architecture**: Designed for high-volume log ingestion with efficient storage
- **Developer Friendly**: RESTful API, comprehensive documentation, and easy integration

---

## ✨ Features

### Core Features

| Feature | Description |
|---------|-------------|
| 📊 **Centralized Logging** | Aggregate logs from multiple servers in one place |
| 🔄 **Job Monitoring** | Track job executions, status, and performance |
| ⚡ **Real-time Updates** | Live dashboard updates via SignalR WebSockets |
| 🚨 **Smart Alerting** | Configurable alerts with cooldown and escalation |
| 📈 **Analytics** | Log statistics, trends, and health scores |
| 🔐 **Security** | JWT authentication, role-based access control |
| 🐳 **Docker Ready** | Full containerization support |

### Alerting Capabilities

- **Job Failure Alerts** - Notify when jobs fail
- **Consecutive Failure Detection** - Alert after N consecutive failures
- **Error Threshold Monitoring** - Trigger when error count exceeds threshold
- **Server Offline Detection** - Alert when servers stop reporting
- **Job Timeout Alerts** - Notify when jobs exceed expected duration
- **Custom Alerts** - Define your own alert conditions

### Real-time Features

- **Live Log Streaming** - Watch logs as they arrive
- **Dashboard Updates** - Real-time metrics and statistics
- **Alert Notifications** - Instant alert delivery via WebSocket
- **Execution Status** - Live job execution tracking

---

## 🚀 Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server 2019+](https://www.microsoft.com/sql-server) or [Docker](https://www.docker.com/)
- [Redis](https://redis.io/) (optional, for caching)

### Option 1: Docker (Recommended)

```bash
# Clone the repository
git clone https://github.com/your-org/fms-log-nexus.git
cd fms-log-nexus

# Start with Docker Compose
cd docker
cp .env.dev.example .env
docker-compose -f docker-compose.dev.yml up --build
```

Access the API at http://localhost:5000/swagger

### Option 2: Local Development

```bash
# Clone the repository
git clone https://github.com/your-org/fms-log-nexus.git
cd fms-log-nexus

# Restore dependencies
dotnet restore

# Update connection string in appsettings.Development.json
# Then run migrations
dotnet ef database update --project src/FMSLogNexus.Infrastructure

# Run the application
dotnet run --project src/FMSLogNexus.Api
```

### First Steps

1. **Create Admin User**
   ```bash
   curl -X POST http://localhost:5000/api/auth/register \
     -H "Content-Type: application/json" \
     -d '{"username":"admin","email":"admin@example.com","password":"SecurePass123!"}'
   ```

2. **Login**
   ```bash
   curl -X POST http://localhost:5000/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"username":"admin","password":"SecurePass123!"}'
   ```

3. **Register a Server**
   ```bash
   curl -X POST http://localhost:5000/api/servers/heartbeat \
     -H "Authorization: Bearer <your-token>" \
     -H "Content-Type: application/json" \
     -d '{"serverName":"SERVER-01","status":"Online","agentVersion":"1.0.0"}'
   ```

---

## 📖 Documentation

| Document | Description |
|----------|-------------|
| [Architecture Guide](docs/ARCHITECTURE.md) | System design and component overview |
| [API Reference](docs/API.md) | Complete API endpoint documentation |
| [Deployment Guide](docs/DEPLOYMENT.md) | Production deployment instructions |
| [Configuration Guide](docs/CONFIGURATION.md) | All configuration options |
| [Development Guide](docs/DEVELOPMENT.md) | Contributing and development setup |
| [Operations Guide](docs/OPERATIONS.md) | Monitoring, backup, troubleshooting |

---

## 🔌 API Reference

### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | User login |
| POST | `/api/auth/refresh` | Refresh token |
| POST | `/api/auth/logout` | User logout |

### Logs

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/logs` | Create log entry |
| POST | `/api/logs/batch` | Batch create logs |
| GET | `/api/logs/{id}` | Get log by ID |
| GET | `/api/logs` | Search logs |
| GET | `/api/logs/statistics` | Get log statistics |

### Jobs

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/jobs` | Create job |
| GET | `/api/jobs/{id}` | Get job details |
| PUT | `/api/jobs/{id}` | Update job |
| DELETE | `/api/jobs/{id}` | Delete job |
| GET | `/api/jobs/{id}/health` | Get job health score |

### Executions

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/executions` | Start execution |
| PUT | `/api/executions/{id}/complete` | Complete execution |
| POST | `/api/executions/{id}/cancel` | Cancel execution |
| GET | `/api/executions/running` | Get running executions |

### Servers

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/servers/heartbeat` | Server heartbeat |
| GET | `/api/servers` | List servers |
| GET | `/api/servers/status-summary` | Status summary |

### Alerts

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/alerts` | Create alert rule |
| POST | `/api/alerts/{id}/trigger` | Trigger alert |
| POST | `/api/alerts/instances/{id}/acknowledge` | Acknowledge alert |
| POST | `/api/alerts/instances/{id}/resolve` | Resolve alert |

[View Complete API Documentation →](docs/API.md)

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Client Applications                       │
│  (FMS Agents, Web Dashboard, Mobile Apps, Third-party Systems)  │
└─────────────────────────────────────────────────────────────────┘
                                │
                    ┌───────────┴───────────┐
                    ▼                       ▼
            ┌───────────────┐       ┌───────────────┐
            │   REST API    │       │   SignalR     │
            │  (HTTP/JSON)  │       │  (WebSocket)  │
            └───────────────┘       └───────────────┘
                    │                       │
                    └───────────┬───────────┘
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      FMS Log Nexus API                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐ │
│  │ Controllers │  │  Services   │  │  Background Services    │ │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘ │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐ │
│  │    Hubs     │  │Repositories │  │     SignalR Hubs        │ │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                                │
                    ┌───────────┼───────────┐
                    ▼           ▼           ▼
            ┌───────────┐ ┌───────────┐ ┌───────────┐
            │SQL Server │ │   Redis   │ │    Seq    │
            │ (Primary) │ │  (Cache)  │ │  (Logs)   │
            └───────────┘ └───────────┘ └───────────┘
```

[View Detailed Architecture →](docs/ARCHITECTURE.md)

---

## 🐳 Deployment

### Docker Deployment

```bash
# Production deployment
cd docker
cp .env.prod.example .env
# Edit .env with secure values
./scripts/start-prod.sh
```

### Kubernetes Deployment

```bash
# Apply Kubernetes manifests
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secrets.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
```

### Manual Deployment

1. Build the application
   ```bash
   dotnet publish src/FMSLogNexus.Api -c Release -o ./publish
   ```

2. Configure environment variables

3. Run database migrations

4. Start the application
   ```bash
   dotnet FMSLogNexus.Api.dll
   ```

[View Complete Deployment Guide →](docs/DEPLOYMENT.md)

---

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test tests/FMSLogNexus.Tests.Unit

# Run integration tests
dotnet test tests/FMSLogNexus.Tests.Integration

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## 📊 Monitoring

### Health Checks

- **Liveness**: `GET /health`
- **Readiness**: `GET /health/ready`

### Metrics

Prometheus metrics available at `/metrics`:
- Request duration
- Request count
- Active connections
- Database query performance
- Cache hit/miss ratio

### Logging

Structured logging with Serilog:
- Console output (development)
- File output
- Seq integration
- Custom sinks supported

---

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- [ASP.NET Core](https://docs.microsoft.com/aspnet/core) - Web framework
- [Entity Framework Core](https://docs.microsoft.com/ef/core) - ORM
- [SignalR](https://docs.microsoft.com/aspnet/signalr) - Real-time communication
- [Serilog](https://serilog.net/) - Structured logging
- [FluentValidation](https://fluentvalidation.net/) - Validation
- [Swagger/OpenAPI](https://swagger.io/) - API documentation

---

<p align="center">
  Made with ❤️ by the FMS Log Nexus Team
</p>
