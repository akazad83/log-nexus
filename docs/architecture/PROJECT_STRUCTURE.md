# FMS Log Nexus - Project Structure

## Solution Overview

```
FMSLogNexus/
│
├── 📁 src/                                    # Source code
│   │
│   ├── 📁 FMSLogNexus.Core/                   # Domain layer
│   │   ├── 📁 Constants/                      # Application constants
│   │   ├── 📁 DTOs/                           # Data transfer objects
│   │   │   ├── 📁 Requests/                   # API request DTOs
│   │   │   └── 📁 Responses/                  # API response DTOs
│   │   ├── 📁 Entities/                       # Domain entities
│   │   ├── 📁 Enums/                          # Enumeration types
│   │   ├── 📁 Exceptions/                     # Custom exceptions
│   │   ├── 📁 Extensions/                     # Extension methods
│   │   └── 📁 Interfaces/                     # Abstractions
│   │       ├── 📁 Repositories/               # Repository interfaces
│   │       └── 📁 Services/                   # Service interfaces
│   │
│   ├── 📁 FMSLogNexus.Infrastructure/         # Infrastructure layer
│   │   ├── 📁 Data/                           # Data access
│   │   │   ├── 📁 Configurations/             # EF Core configurations
│   │   │   └── 📁 Migrations/                 # Database migrations
│   │   ├── 📁 Extensions/                     # DI extensions
│   │   ├── 📁 Repositories/                   # Repository implementations
│   │   └── 📁 Services/                       # Service implementations
│   │
│   ├── 📁 FMSLogNexus.Api/                    # Web API layer
│   │   ├── 📁 BackgroundServices/             # Background workers
│   │   ├── 📁 Configuration/                  # Configuration DTOs
│   │   ├── 📁 Controllers/                    # API controllers
│   │   ├── 📁 Hubs/                           # SignalR hubs
│   │   ├── 📁 Mapping/                        # AutoMapper profiles
│   │   ├── 📁 Middleware/                     # Custom middleware
│   │   ├── 📁 Services/                       # API-specific services
│   │   └── 📁 Validators/                     # FluentValidation validators
│   │
│   ├── 📁 FMSLogNexus.LogClient/              # Log client library
│   │   ├── 📁 Core/                           # Core logging logic
│   │   ├── 📁 Extensions/                     # DI extensions
│   │   └── 📁 Transport/                      # HTTP transport
│   │
│   ├── 📁 FMSLogNexus.LogClient.PowerShell/   # PowerShell module
│   │   └── 📁 Cmdlets/                        # PowerShell cmdlets
│   │
│   ├── 📁 FMSLogNexus.Collectors.FileLog/     # File log collector
│   │   ├── 📁 Parsers/                        # Log file parsers
│   │   └── 📁 Services/                       # Collection services
│   │
│   └── 📁 FMSLogNexus.Collectors.EventLog/    # Event log collector
│       └── 📁 Services/                       # Collection services
│
├── 📁 tests/                                  # Test projects
│   ├── 📁 FMSLogNexus.Core.Tests/
│   ├── 📁 FMSLogNexus.Infrastructure.Tests/
│   ├── 📁 FMSLogNexus.Api.Tests/
│   │   ├── 📁 Controllers/                    # Controller unit tests
│   │   └── 📁 Integration/                    # Integration tests
│   └── 📁 FMSLogNexus.LogClient.Tests/
│
├── 📁 scripts/                                # Scripts
│   ├── 📁 setup/                              # Setup scripts
│   ├── 📁 deployment/                         # Deployment scripts
│   └── 📁 database/                           # Database scripts
│
├── 📁 docs/                                   # Documentation
│   ├── 📁 api/                                # API documentation
│   └── 📁 architecture/                       # Architecture docs
│
├── 📄 FMSLogNexus.sln                         # Solution file
├── 📄 Directory.Build.props                   # Common build properties
├── 📄 Directory.Packages.props                # Central package management
├── 📄 global.json                             # SDK version
├── 📄 .editorconfig                           # Code style settings
├── 📄 .gitignore                              # Git ignore rules
└── 📄 README.md                               # Project readme
```

## Project Dependencies

```
┌─────────────────────────────────────────────────────────────────────┐
│                         FMSLogNexus.Api                             │
│                    (ASP.NET Core Web API)                           │
└──────────────────────────┬──────────────────────────────────────────┘
                           │
              ┌────────────┴────────────┐
              ▼                         ▼
┌─────────────────────────┐  ┌─────────────────────────┐
│ FMSLogNexus.Infrastructure│  │   FMSLogNexus.Core      │
│   (Data Access, EF Core)  │  │  (Domain, Interfaces)   │
└────────────┬──────────────┘  └─────────────────────────┘
             │                            ▲
             └────────────────────────────┘

┌─────────────────────────┐     ┌─────────────────────────┐
│ FMSLogNexus.LogClient   │     │ FMSLogNexus.Collectors  │
│   (NuGet Package)       │     │   (Windows Services)    │
└────────────┬────────────┘     └────────────┬────────────┘
             │                               │
             └───────────┬───────────────────┘
                         ▼
                    HTTP/REST API
```

## Technology Stack

| Layer | Technology |
|-------|------------|
| **Runtime** | .NET 8 |
| **Web Framework** | ASP.NET Core 8 |
| **ORM** | Entity Framework Core 8 |
| **Database** | SQL Server 2019+ |
| **Micro ORM** | Dapper (for high-performance queries) |
| **Real-time** | SignalR |
| **Validation** | FluentValidation |
| **Mapping** | AutoMapper |
| **Logging** | Serilog |
| **Resilience** | Polly |
| **API Docs** | Swagger / OpenAPI |
| **Testing** | xUnit, Moq, FluentAssertions |

## NuGet Packages

### Core Project
- `Microsoft.Extensions.Logging.Abstractions`
- `System.Text.Json`

### Infrastructure Project
- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Dapper`
- `Microsoft.Data.SqlClient`
- `Polly`
- `BCrypt.Net-Next`
- `System.IdentityModel.Tokens.Jwt`

### API Project
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Serilog.AspNetCore`
- `FluentValidation.DependencyInjectionExtensions`
- `AutoMapper`
- `Swashbuckle.AspNetCore`
- `AspNetCore.HealthChecks.SqlServer`
- `System.Threading.Channels`

### Log Client Project
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.Http`
- `System.Text.Json`
- `System.Threading.Channels`
- `Polly`

## Build & Run

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run API (development)
dotnet run --project src/FMSLogNexus.Api

# Publish for production
dotnet publish src/FMSLogNexus.Api -c Release -o ./publish
```

## Configuration Files

| File | Purpose |
|------|---------|
| `appsettings.json` | Base configuration |
| `appsettings.Development.json` | Development overrides |
| `appsettings.Production.json` | Production overrides |

## Key Namespaces

| Namespace | Purpose |
|-----------|---------|
| `FMSLogNexus.Core.Entities` | Domain entity classes |
| `FMSLogNexus.Core.DTOs` | Data transfer objects |
| `FMSLogNexus.Core.Enums` | Enumeration types |
| `FMSLogNexus.Core.Interfaces` | Service/repository contracts |
| `FMSLogNexus.Infrastructure.Data` | EF Core DbContext |
| `FMSLogNexus.Infrastructure.Repositories` | Data access implementations |
| `FMSLogNexus.Api.Controllers` | REST API endpoints |
| `FMSLogNexus.Api.Hubs` | SignalR real-time hubs |
| `FMSLogNexus.LogClient` | Client library for logging |
