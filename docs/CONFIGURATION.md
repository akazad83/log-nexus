# FMS Log Nexus - Configuration Guide

Complete reference for all configuration options.

## Table of Contents

1. [Configuration Sources](#configuration-sources)
2. [Connection Strings](#connection-strings)
3. [JWT Authentication](#jwt-authentication)
4. [Background Services](#background-services)
5. [Logging Configuration](#logging-configuration)
6. [CORS Settings](#cors-settings)
7. [Rate Limiting](#rate-limiting)
8. [Feature Flags](#feature-flags)

---

## Configuration Sources

Configuration is loaded in the following order (later sources override earlier):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Environment variables
4. Command line arguments
5. User secrets (development only)

### Environment Variables

All settings can be overridden via environment variables using `__` as separator:

```bash
# Example
ConnectionStrings__DefaultConnection="Server=..."
Jwt__SecretKey="your-secret-key"
BackgroundServices__DashboardUpdateIntervalSeconds=10
```

---

## Connection Strings

### SQL Server

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=FMSLogNexus;User Id=app_user;Password=YourPassword;TrustServerCertificate=True;Encrypt=True;Connection Timeout=30;Max Pool Size=100;"
  }
}
```

| Parameter | Description | Default |
|-----------|-------------|---------|
| Server | SQL Server host | Required |
| Database | Database name | Required |
| User Id | SQL authentication user | Required |
| Password | SQL authentication password | Required |
| TrustServerCertificate | Trust self-signed certs | False |
| Encrypt | Encrypt connection | True |
| Connection Timeout | Connection timeout (seconds) | 30 |
| Max Pool Size | Maximum connections | 100 |

### Redis

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,password=yourpassword,abortConnect=false,connectTimeout=5000,syncTimeout=5000"
  }
}
```

| Parameter | Description | Default |
|-----------|-------------|---------|
| host:port | Redis server address | Required |
| password | Redis password | None |
| abortConnect | Abort if can't connect | true |
| connectTimeout | Connection timeout (ms) | 5000 |
| syncTimeout | Sync operation timeout (ms) | 5000 |
| ssl | Enable SSL | false |

---

## JWT Authentication

```json
{
  "Jwt": {
    "SecretKey": "YourVeryLongSecretKeyAtLeast64CharactersForHS256Algorithm!",
    "Issuer": "FMSLogNexus",
    "Audience": "FMSLogNexus",
    "ExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 7,
    "ClockSkewMinutes": 5
  }
}
```

| Setting | Description | Default |
|---------|-------------|---------|
| SecretKey | HMAC signing key (min 64 chars) | Required |
| Issuer | Token issuer | Required |
| Audience | Token audience | Required |
| ExpirationMinutes | Access token lifetime | 30 |
| RefreshTokenExpirationDays | Refresh token lifetime | 7 |
| ClockSkewMinutes | Allowed clock drift | 5 |

### Security Recommendations

- Use at least 64-character secret key
- Rotate keys periodically
- Use short access token expiration (15-30 min)
- Store refresh tokens securely

---

## Background Services

```json
{
  "BackgroundServices": {
    "DashboardUpdateIntervalSeconds": 5,
    "ServerHealthCheckIntervalSeconds": 30,
    "ExecutionTimeoutCheckIntervalSeconds": 60,
    "AlertEvaluationIntervalSeconds": 30,
    "MaintenanceIntervalMinutes": 60,
    "LogRetentionIntervalHours": 24,
    "ServerTimeoutSeconds": 120,
    "LogRetentionDays": 90,
    "ExecutionRetentionDays": 365
  }
}
```

| Setting | Description | Default | Range |
|---------|-------------|---------|-------|
| DashboardUpdateIntervalSeconds | Dashboard broadcast interval | 5 | 1-60 |
| ServerHealthCheckIntervalSeconds | Server heartbeat check interval | 30 | 10-300 |
| ExecutionTimeoutCheckIntervalSeconds | Check for timed out executions | 60 | 30-600 |
| AlertEvaluationIntervalSeconds | Alert rule evaluation interval | 30 | 10-300 |
| MaintenanceIntervalMinutes | Database maintenance interval | 60 | 30-1440 |
| LogRetentionIntervalHours | Log cleanup interval | 24 | 1-168 |
| ServerTimeoutSeconds | Mark server offline after | 120 | 60-600 |
| LogRetentionDays | Keep logs for N days | 90 | 7-3650 |
| ExecutionRetentionDays | Keep executions for N days | 365 | 30-3650 |

---

## Logging Configuration

### Serilog Configuration

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Seq"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/fms-lognexus-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "fileSizeLimitBytes": 104857600,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341",
          "apiKey": "your-seq-api-key"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"],
    "Properties": {
      "Application": "FMSLogNexus"
    }
  }
}
```

### Log Levels

| Level | Usage |
|-------|-------|
| Verbose | Detailed debugging |
| Debug | Development diagnostics |
| Information | Normal operations |
| Warning | Potential issues |
| Error | Errors requiring attention |
| Fatal | Critical failures |

---

## CORS Settings

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://dashboard.yourdomain.com",
      "https://admin.yourdomain.com"
    ],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "OPTIONS"],
    "AllowedHeaders": ["Content-Type", "Authorization", "X-Request-Id"],
    "AllowCredentials": true,
    "MaxAgeSeconds": 86400
  }
}
```

| Setting | Description | Default |
|---------|-------------|---------|
| AllowedOrigins | Allowed origin URLs | [] |
| AllowedMethods | Allowed HTTP methods | All |
| AllowedHeaders | Allowed request headers | All |
| AllowCredentials | Allow credentials | false |
| MaxAgeSeconds | Preflight cache duration | 86400 |

---

## Rate Limiting

```json
{
  "RateLimiting": {
    "EnableRateLimiting": true,
    "GeneralLimit": {
      "PermitLimit": 1000,
      "WindowSeconds": 60,
      "QueueLimit": 100
    },
    "LoginLimit": {
      "PermitLimit": 5,
      "WindowSeconds": 60,
      "QueueLimit": 0
    },
    "BatchLogLimit": {
      "PermitLimit": 100,
      "WindowSeconds": 60,
      "QueueLimit": 50
    }
  }
}
```

| Setting | Description | Default |
|---------|-------------|---------|
| EnableRateLimiting | Enable/disable rate limiting | true |
| PermitLimit | Requests allowed per window | varies |
| WindowSeconds | Time window duration | 60 |
| QueueLimit | Queued requests when limit hit | 0 |

---

## Feature Flags

```json
{
  "Features": {
    "EnableSwagger": true,
    "EnableMetrics": true,
    "EnableHealthChecks": true,
    "EnableSignalR": true,
    "EnableCaching": true,
    "EnableAuditLogging": true,
    "EnableEmailNotifications": false,
    "EnableWebhooks": false
  }
}
```

| Flag | Description | Default |
|------|-------------|---------|
| EnableSwagger | Enable Swagger UI | true (dev) |
| EnableMetrics | Enable Prometheus metrics | true |
| EnableHealthChecks | Enable health endpoints | true |
| EnableSignalR | Enable real-time features | true |
| EnableCaching | Enable Redis caching | true |
| EnableAuditLogging | Log user actions | true |
| EnableEmailNotifications | Send email alerts | false |
| EnableWebhooks | Enable webhook callbacks | false |

---

## Email Configuration

```json
{
  "Email": {
    "SmtpHost": "smtp.example.com",
    "SmtpPort": 587,
    "SmtpUsername": "notifications@example.com",
    "SmtpPassword": "your-password",
    "UseSsl": true,
    "FromAddress": "noreply@example.com",
    "FromName": "FMS Log Nexus"
  }
}
```

---

## Caching Configuration

```json
{
  "Caching": {
    "DefaultExpirationMinutes": 5,
    "JobCacheMinutes": 5,
    "ServerCacheMinutes": 1,
    "StatisticsCacheSeconds": 30,
    "UserSessionMinutes": 30
  }
}
```

---

## Environment-Specific Files

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "Features": {
    "EnableSwagger": true
  }
}
```

### appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Features": {
    "EnableSwagger": false
  }
}
```

---

## Docker Environment Variables

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ConnectionStrings__DefaultConnection=Server=...
  - ConnectionStrings__Redis=redis:6379
  - Jwt__SecretKey=your-secret-key
  - Jwt__Issuer=FMSLogNexus
  - Jwt__Audience=FMSLogNexus
  - Serilog__WriteTo__0__Args__serverUrl=http://seq:5341
```

---

## Validation

Configuration is validated at startup. Invalid settings will prevent the application from starting.

Required settings:
- `ConnectionStrings:DefaultConnection`
- `Jwt:SecretKey` (min 64 characters)
- `Jwt:Issuer`
- `Jwt:Audience`

---

## Security Best Practices

1. **Never commit secrets** - Use environment variables or secret managers
2. **Use strong JWT keys** - Minimum 64 characters, randomly generated
3. **Encrypt connections** - Enable SSL/TLS for all connections
4. **Limit CORS origins** - Don't use wildcards in production
5. **Enable rate limiting** - Protect against abuse
6. **Rotate credentials** - Regularly rotate passwords and keys
