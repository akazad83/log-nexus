# FMS Log Nexus - Deployment Guide

Complete guide for deploying FMS Log Nexus in various environments.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Deployment Options](#deployment-options)
3. [Docker Deployment](#docker-deployment)
4. [Kubernetes Deployment](#kubernetes-deployment)
5. [Manual Deployment](#manual-deployment)
6. [Database Setup](#database-setup)
7. [SSL/TLS Configuration](#ssltls-configuration)
8. [Load Balancing](#load-balancing)
9. [Post-Deployment](#post-deployment)
10. [Rollback Procedures](#rollback-procedures)

---

## Prerequisites

### System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| CPU | 2 cores | 4+ cores |
| RAM | 4 GB | 8+ GB |
| Disk | 50 GB SSD | 200+ GB SSD |
| Network | 100 Mbps | 1 Gbps |

### Software Requirements

- **.NET 8.0 Runtime** (for manual deployment)
- **Docker 24.0+** and **Docker Compose 2.0+** (for container deployment)
- **SQL Server 2019+** or **Azure SQL Database**
- **Redis 7.0+** (optional, for caching)
- **Nginx** or **Traefik** (for reverse proxy)

### Network Requirements

| Port | Service | Direction |
|------|---------|-----------|
| 80 | HTTP | Inbound |
| 443 | HTTPS | Inbound |
| 1433 | SQL Server | Internal |
| 6379 | Redis | Internal |
| 5341 | Seq | Internal |

---

## Deployment Options

| Method | Best For | Complexity |
|--------|----------|------------|
| Docker Compose | Small to medium deployments | Low |
| Kubernetes | Large-scale, high availability | High |
| Manual | Legacy infrastructure | Medium |
| Cloud PaaS | Managed infrastructure | Low |

---

## Docker Deployment

### Quick Start

```bash
# Clone repository
git clone https://github.com/your-org/fms-log-nexus.git
cd fms-log-nexus/docker

# Copy and configure environment
cp .env.prod.example .env
nano .env  # Edit with secure values

# Start services
docker-compose -f docker-compose.prod.yml up -d
```

### Step-by-Step

#### 1. Prepare Environment File

```bash
cp .env.prod.example .env
```

Edit `.env` with secure values:

```bash
# Required settings
MSSQL_SA_PASSWORD=YourSecurePassword123!
JWT_SECRET_KEY=YourVeryLongSecretKeyAtLeast64Characters...
GRAFANA_ADMIN_PASSWORD=SecureGrafanaPassword!

# Connection strings
DB_CONNECTION_STRING=Server=sqlserver;Database=FMSLogNexus;User Id=sa;Password=YourSecurePassword123!;TrustServerCertificate=True;
REDIS_CONNECTION_STRING=redis:6379,abortConnect=false
```

#### 2. Generate SSL Certificates

**Option A: Self-Signed (Testing)**
```bash
mkdir -p nginx/ssl
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout nginx/ssl/server.key \
    -out nginx/ssl/server.crt \
    -subj "/C=US/ST=State/L=City/O=Org/CN=localhost"
```

**Option B: Let's Encrypt (Production)**
```bash
certbot certonly --standalone -d api.yourdomain.com
cp /etc/letsencrypt/live/api.yourdomain.com/fullchain.pem nginx/ssl/server.crt
cp /etc/letsencrypt/live/api.yourdomain.com/privkey.pem nginx/ssl/server.key
```

#### 3. Build and Start

```bash
# Build images
docker-compose -f docker-compose.prod.yml build

# Start services
docker-compose -f docker-compose.prod.yml up -d

# Verify services
docker-compose -f docker-compose.prod.yml ps
```

#### 4. Initialize Database

```bash
# Wait for SQL Server to be ready
sleep 30

# Run migrations (if using EF migrations)
docker-compose -f docker-compose.prod.yml exec api \
    dotnet ef database update

# Or run initialization script
docker exec fms-lognexus-sqlserver /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "YourPassword" -C \
    -i /docker-entrypoint-initdb.d/init-db.sql
```

#### 5. Create Admin User

```bash
curl -X POST https://localhost/api/auth/register \
    -H "Content-Type: application/json" \
    -k \
    -d '{
        "username": "admin",
        "email": "admin@yourdomain.com",
        "password": "AdminPassword123!"
    }'
```

### Docker Compose Services

| Service | Purpose | Port |
|---------|---------|------|
| nginx | Reverse proxy, SSL termination | 80, 443 |
| api | FMS Log Nexus API | 8080 (internal) |
| sqlserver | SQL Server database | 1433 |
| redis | Caching | 6379 |
| seq | Log aggregation | 5341, 8082 |
| prometheus | Metrics collection | 9090 |
| grafana | Dashboards | 3000 |

---

## Kubernetes Deployment

### Prerequisites

- Kubernetes 1.25+
- kubectl configured
- Helm 3.0+ (optional)

### Kubernetes Manifests

#### Namespace

```yaml
# k8s/namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: fms-lognexus
```

#### ConfigMap

```yaml
# k8s/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: fms-lognexus-config
  namespace: fms-lognexus
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  Logging__LogLevel__Default: "Information"
  BackgroundServices__DashboardUpdateIntervalSeconds: "5"
  BackgroundServices__ServerHealthCheckIntervalSeconds: "30"
```

#### Secrets

```yaml
# k8s/secrets.yaml
apiVersion: v1
kind: Secret
metadata:
  name: fms-lognexus-secrets
  namespace: fms-lognexus
type: Opaque
stringData:
  db-connection-string: "Server=sqlserver;Database=FMSLogNexus;..."
  jwt-secret-key: "YourSecretKey..."
  redis-connection-string: "redis:6379"
```

#### Deployment

```yaml
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: fms-lognexus-api
  namespace: fms-lognexus
spec:
  replicas: 3
  selector:
    matchLabels:
      app: fms-lognexus-api
  template:
    metadata:
      labels:
        app: fms-lognexus-api
    spec:
      containers:
      - name: api
        image: your-registry/fms-lognexus-api:latest
        ports:
        - containerPort: 8080
        envFrom:
        - configMapRef:
            name: fms-lognexus-config
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: fms-lognexus-secrets
              key: db-connection-string
        - name: Jwt__SecretKey
          valueFrom:
            secretKeyRef:
              name: fms-lognexus-secrets
              key: jwt-secret-key
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "1000m"
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
```

#### Service

```yaml
# k8s/service.yaml
apiVersion: v1
kind: Service
metadata:
  name: fms-lognexus-api
  namespace: fms-lognexus
spec:
  selector:
    app: fms-lognexus-api
  ports:
  - port: 80
    targetPort: 8080
  type: ClusterIP
---
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: fms-lognexus-ingress
  namespace: fms-lognexus
  annotations:
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - api.yourdomain.com
    secretName: fms-lognexus-tls
  rules:
  - host: api.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: fms-lognexus-api
            port:
              number: 80
```

### Deploy to Kubernetes

```bash
# Apply manifests
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secrets.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml

# Verify deployment
kubectl get pods -n fms-lognexus
kubectl get svc -n fms-lognexus

# Check logs
kubectl logs -f deployment/fms-lognexus-api -n fms-lognexus
```

---

## Manual Deployment

### 1. Install Prerequisites

**Windows Server:**
```powershell
# Install .NET 8.0 Runtime
winget install Microsoft.DotNet.AspNetCore.8

# Install SQL Server (or use existing)
# Download from https://www.microsoft.com/sql-server
```

**Linux (Ubuntu/Debian):**
```bash
# Install .NET 8.0 Runtime
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-8.0
```

### 2. Build Application

```bash
# Clone and build
git clone https://github.com/your-org/fms-log-nexus.git
cd fms-log-nexus
dotnet publish src/FMSLogNexus.Api -c Release -o ./publish
```

### 3. Configure Application

Create `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=FMSLogNexus;User Id=app_user;Password=YourPassword;TrustServerCertificate=True;",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "SecretKey": "YourVeryLongSecretKeyAtLeast64Characters...",
    "Issuer": "FMSLogNexus",
    "Audience": "FMSLogNexus",
    "ExpirationMinutes": 30
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "/var/log/fms-lognexus/log-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

### 4. Run as Service

**Windows (NSSM):**
```powershell
# Install NSSM
choco install nssm

# Create service
nssm install FMSLogNexus "C:\apps\fms-lognexus\FMSLogNexus.Api.exe"
nssm set FMSLogNexus AppDirectory "C:\apps\fms-lognexus"
nssm set FMSLogNexus AppEnvironmentExtra "ASPNETCORE_ENVIRONMENT=Production"
nssm start FMSLogNexus
```

**Linux (systemd):**
```bash
# Create service file
sudo nano /etc/systemd/system/fms-lognexus.service
```

```ini
[Unit]
Description=FMS Log Nexus API
After=network.target

[Service]
WorkingDirectory=/opt/fms-lognexus
ExecStart=/usr/bin/dotnet /opt/fms-lognexus/FMSLogNexus.Api.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=fms-lognexus
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

```bash
# Enable and start service
sudo systemctl daemon-reload
sudo systemctl enable fms-lognexus
sudo systemctl start fms-lognexus
sudo systemctl status fms-lognexus
```

---

## Database Setup

### SQL Server Setup

```sql
-- Create database
CREATE DATABASE [FMSLogNexus];
GO

-- Create application user
CREATE LOGIN [fms_app_user] WITH PASSWORD = 'YourSecurePassword!';
GO

USE [FMSLogNexus];
GO

CREATE USER [fms_app_user] FOR LOGIN [fms_app_user];
GO

-- Grant permissions
ALTER ROLE db_datareader ADD MEMBER [fms_app_user];
ALTER ROLE db_datawriter ADD MEMBER [fms_app_user];
ALTER ROLE db_ddladmin ADD MEMBER [fms_app_user];
GO
```

### Run Migrations

```bash
# From project root
dotnet ef database update --project src/FMSLogNexus.Infrastructure --startup-project src/FMSLogNexus.Api
```

---

## SSL/TLS Configuration

### Nginx Configuration

```nginx
server {
    listen 443 ssl http2;
    server_name api.yourdomain.com;

    ssl_certificate /etc/nginx/ssl/server.crt;
    ssl_certificate_key /etc/nginx/ssl/server.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256;

    location / {
        proxy_pass http://localhost:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

---

## Load Balancing

### Multiple API Instances

```yaml
# docker-compose.prod.yml (excerpt)
services:
  api:
    deploy:
      replicas: 3
    ...
```

### SignalR Sticky Sessions

When running multiple instances, configure sticky sessions for SignalR:

```nginx
upstream api_servers {
    ip_hash;  # Sticky sessions
    server api1:8080;
    server api2:8080;
    server api3:8080;
}
```

Or use Redis backplane:

```csharp
// Program.cs
builder.Services.AddSignalR()
    .AddStackExchangeRedis(connectionString);
```

---

## Post-Deployment

### Health Checks

```bash
# Check API health
curl https://api.yourdomain.com/health

# Check readiness
curl https://api.yourdomain.com/health/ready
```

### Create Initial Admin

```bash
curl -X POST https://api.yourdomain.com/api/auth/register \
    -H "Content-Type: application/json" \
    -d '{
        "username": "admin",
        "email": "admin@yourdomain.com",
        "password": "SecurePassword123!"
    }'
```

### Verify Services

```bash
# Docker
docker-compose -f docker-compose.prod.yml ps
./scripts/health-check.sh prod

# Kubernetes
kubectl get pods -n fms-lognexus
kubectl logs -f deployment/fms-lognexus-api -n fms-lognexus
```

---

## Rollback Procedures

### Docker Rollback

```bash
# Stop current version
docker-compose -f docker-compose.prod.yml down

# Restore previous image
docker tag fms-lognexus-api:previous fms-lognexus-api:latest

# Start previous version
docker-compose -f docker-compose.prod.yml up -d
```

### Kubernetes Rollback

```bash
# View rollout history
kubectl rollout history deployment/fms-lognexus-api -n fms-lognexus

# Rollback to previous
kubectl rollout undo deployment/fms-lognexus-api -n fms-lognexus

# Rollback to specific revision
kubectl rollout undo deployment/fms-lognexus-api --to-revision=2 -n fms-lognexus
```

### Database Rollback

```bash
# Restore from backup
./scripts/restore-db.sh backup_20240115.bak

# Or rollback migrations
dotnet ef database update <PreviousMigrationName> \
    --project src/FMSLogNexus.Infrastructure
```

---

## Deployment Checklist

- [ ] Environment variables configured
- [ ] SSL certificates installed
- [ ] Database created and migrated
- [ ] Admin user created
- [ ] Health endpoints responding
- [ ] Logging configured and working
- [ ] Backup procedures tested
- [ ] Monitoring dashboards set up
- [ ] Alerting configured
- [ ] Documentation updated
