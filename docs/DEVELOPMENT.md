# FMS Log Nexus - Development Guide

Guide for setting up a development environment and contributing to the project.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Development Setup](#development-setup)
3. [Project Structure](#project-structure)
4. [Coding Standards](#coding-standards)
5. [Testing](#testing)
6. [Debugging](#debugging)
7. [Database Migrations](#database-migrations)
8. [API Development](#api-development)
9. [Building and Running](#building-and-running)

---

## Prerequisites

### Required Tools

| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | 8.0+ | [Download](https://dotnet.microsoft.com/download) |
| Visual Studio / VS Code | Latest | [VS](https://visualstudio.microsoft.com/) / [VS Code](https://code.visualstudio.com/) |
| Docker Desktop | Latest | [Download](https://www.docker.com/products/docker-desktop) |
| Git | Latest | [Download](https://git-scm.com/) |

### Recommended VS Code Extensions

```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-dotnettools.csdevkit",
    "ms-azuretools.vscode-docker",
    "eamodio.gitlens",
    "streetsidesoftware.code-spell-checker"
  ]
}
```

---

## Development Setup

### 1. Clone Repository

```bash
git clone https://github.com/your-org/fms-log-nexus.git
cd fms-log-nexus
```

### 2. Start Infrastructure (Docker)

```bash
cd docker
cp .env.dev.example .env
docker-compose -f docker-compose.dev.yml up -d sqlserver redis seq maildev
```

### 3. Configure User Secrets

```bash
cd src/FMSLogNexus.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=FMSLogNexus_Dev;User Id=sa;Password=DevPassword123!;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:SecretKey" "DevSecretKeyForLocalDevelopmentOnly123456789012345678901234567890!"
```

### 4. Run Migrations

```bash
cd ../..
dotnet ef database update --project src/FMSLogNexus.Infrastructure --startup-project src/FMSLogNexus.Api
```

### 5. Run Application

```bash
dotnet run --project src/FMSLogNexus.Api
```

### 6. Access Services

| Service | URL |
|---------|-----|
| API | http://localhost:5000 |
| Swagger | http://localhost:5000/swagger |
| Seq | http://localhost:8082 |
| MailDev | http://localhost:1080 |

---

## Project Structure

```
FMSLogNexus/
├── src/
│   ├── FMSLogNexus.Core/           # Domain layer
│   │   ├── Entities/               # Domain entities
│   │   ├── Enums/                  # Enumerations
│   │   ├── DTOs/                   # Data transfer objects
│   │   ├── Interfaces/             # Repository & service interfaces
│   │   └── Exceptions/             # Domain exceptions
│   │
│   ├── FMSLogNexus.Infrastructure/ # Infrastructure layer
│   │   ├── Data/                   # EF Core configuration
│   │   ├── Repositories/           # Repository implementations
│   │   └── Services/               # Service implementations
│   │
│   └── FMSLogNexus.Api/            # Presentation layer
│       ├── Controllers/            # API controllers
│       ├── Hubs/                   # SignalR hubs
│       ├── BackgroundServices/     # Hosted services
│       └── Middleware/             # Custom middleware
│
├── tests/
│   ├── FMSLogNexus.Tests.Unit/
│   └── FMSLogNexus.Tests.Integration/
│
├── docker/                         # Docker configuration
└── docs/                           # Documentation
```

### Layer Dependencies

```
FMSLogNexus.Api
    └── FMSLogNexus.Infrastructure
            └── FMSLogNexus.Core
```

---

## Coding Standards

### Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Class | PascalCase | `LogService` |
| Interface | IPascalCase | `ILogService` |
| Method | PascalCase | `GetByIdAsync` |
| Property | PascalCase | `JobId` |
| Variable | camelCase | `logEntry` |
| Constant | UPPER_CASE | `MAX_PAGE_SIZE` |
| Private field | _camelCase | `_logger` |

### Code Style

```csharp
// Use file-scoped namespaces
namespace FMSLogNexus.Core.Entities;

// Use expression-bodied members where appropriate
public string FullName => $"{FirstName} {LastName}";

// Use async/await consistently
public async Task<LogEntry?> GetByIdAsync(Guid id)
{
    return await _context.LogEntries.FindAsync(id);
}

// Use nullable reference types
public string? Description { get; set; }

// Document public APIs
/// <summary>
/// Creates a new log entry.
/// </summary>
/// <param name="request">The log entry details.</param>
/// <returns>The created log entry.</returns>
public async Task<LogResponse> CreateAsync(CreateLogRequest request)
```

### Project Organization

- One class per file
- Group related classes in folders
- Use partial classes sparingly
- Keep files under 500 lines

---

## Testing

### Running Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/FMSLogNexus.Tests.Unit

# Integration tests only
dotnet test tests/FMSLogNexus.Tests.Integration

# With coverage
dotnet test --collect:"XPlat Code Coverage"
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport"
```

### Test Organization

```
tests/
├── FMSLogNexus.Tests.Unit/
│   ├── Entities/           # Entity tests
│   ├── Services/           # Service tests
│   ├── Controllers/        # Controller tests
│   └── Fixtures/           # Test helpers
│
└── FMSLogNexus.Tests.Integration/
    ├── Api/                # API endpoint tests
    ├── Database/           # Database tests
    └── Fixtures/           # Test infrastructure
```

### Writing Tests

```csharp
public class LogServiceTests
{
    private readonly Mock<ILogRepository> _mockRepository;
    private readonly LogService _service;

    public LogServiceTests()
    {
        _mockRepository = new Mock<ILogRepository>();
        _service = new LogService(_mockRepository.Object, ...);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldReturnLogResponse()
    {
        // Arrange
        var request = new CreateLogRequest { ... };
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<LogEntry>()))
            .ReturnsAsync(new LogEntry { ... });

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be(request.Message);
    }
}
```

---

## Debugging

### Visual Studio

1. Set `FMSLogNexus.Api` as startup project
2. Press F5 to start debugging
3. Set breakpoints as needed

### VS Code

```json
// .vscode/launch.json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/FMSLogNexus.Api/bin/Debug/net8.0/FMSLogNexus.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/FMSLogNexus.Api",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

### Logging for Debugging

```csharp
_logger.LogDebug("Processing request: {@Request}", request);
```

### SQL Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

---

## Database Migrations

### Creating Migrations

```bash
dotnet ef migrations add <MigrationName> \
    --project src/FMSLogNexus.Infrastructure \
    --startup-project src/FMSLogNexus.Api
```

### Applying Migrations

```bash
# Development
dotnet ef database update \
    --project src/FMSLogNexus.Infrastructure \
    --startup-project src/FMSLogNexus.Api

# Production (generate script)
dotnet ef migrations script \
    --project src/FMSLogNexus.Infrastructure \
    --startup-project src/FMSLogNexus.Api \
    --output migrations.sql
```

### Reverting Migrations

```bash
dotnet ef database update <PreviousMigrationName> \
    --project src/FMSLogNexus.Infrastructure \
    --startup-project src/FMSLogNexus.Api
```

---

## API Development

### Adding a New Endpoint

1. **Define DTOs** in `FMSLogNexus.Core/DTOs/`

```csharp
public class CreateThingRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
}

public class ThingResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

2. **Create/Update Entity** in `FMSLogNexus.Core/Entities/`

3. **Define Interface** in `FMSLogNexus.Core/Interfaces/`

```csharp
public interface IThingService
{
    Task<ThingResponse> CreateAsync(CreateThingRequest request);
    Task<ThingResponse?> GetByIdAsync(Guid id);
}
```

4. **Implement Service** in `FMSLogNexus.Infrastructure/Services/`

5. **Create Controller** in `FMSLogNexus.Api/Controllers/`

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ThingsController : ControllerBase
{
    private readonly IThingService _thingService;

    [HttpPost]
    [ProducesResponseType(typeof(ThingResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateThingRequest request)
    {
        var result = await _thingService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
```

6. **Register Services** in `Program.cs`

7. **Write Tests**

---

## Building and Running

### Development

```bash
# Build
dotnet build

# Run
dotnet run --project src/FMSLogNexus.Api

# Watch mode (hot reload)
dotnet watch run --project src/FMSLogNexus.Api
```

### Docker Development

```bash
cd docker
docker-compose -f docker-compose.dev.yml up --build
```

### Production Build

```bash
dotnet publish src/FMSLogNexus.Api -c Release -o ./publish
```

### Docker Production Build

```bash
docker build -t fms-lognexus-api:latest -f docker/Dockerfile .
```

---

## Git Workflow

### Branch Naming

- `feature/` - New features
- `bugfix/` - Bug fixes
- `hotfix/` - Production fixes
- `release/` - Release preparation

### Commit Messages

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

Example:
```
feat(logs): add batch log creation endpoint

- Add POST /api/logs/batch endpoint
- Support up to 1000 logs per request
- Add validation for batch requests

Closes #123
```

### Pull Request Checklist

- [ ] Code follows style guidelines
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] No breaking changes (or documented)
- [ ] CI passes

---

## Troubleshooting Development Issues

### Port Already in Use

```bash
# Find process using port
netstat -ano | findstr :5000
# Kill process
taskkill /PID <pid> /F
```

### Database Connection Failed

1. Check SQL Server is running: `docker ps`
2. Verify connection string
3. Check firewall settings

### NuGet Package Issues

```bash
dotnet nuget locals all --clear
dotnet restore
```
