# Contributing to FMS Log Nexus

Thank you for your interest in contributing to FMS Log Nexus! This document provides guidelines and instructions for contributing.

## Table of Contents

1. [Code of Conduct](#code-of-conduct)
2. [Getting Started](#getting-started)
3. [How to Contribute](#how-to-contribute)
4. [Development Process](#development-process)
5. [Pull Request Guidelines](#pull-request-guidelines)
6. [Coding Standards](#coding-standards)
7. [Testing Requirements](#testing-requirements)
8. [Documentation](#documentation)

---

## Code of Conduct

This project adheres to a Code of Conduct. By participating, you are expected to:

- Be respectful and inclusive
- Welcome newcomers and help them get started
- Accept constructive criticism gracefully
- Focus on what's best for the community
- Show empathy towards others

---

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Docker Desktop
- Git
- IDE (Visual Studio, VS Code, or Rider)

### Setting Up Development Environment

1. **Fork the repository** on GitHub

2. **Clone your fork**
   ```bash
   git clone https://github.com/YOUR_USERNAME/fms-log-nexus.git
   cd fms-log-nexus
   ```

3. **Add upstream remote**
   ```bash
   git remote add upstream https://github.com/your-org/fms-log-nexus.git
   ```

4. **Start development infrastructure**
   ```bash
   cd docker
   docker-compose -f docker-compose.dev.yml up -d sqlserver redis seq
   ```

5. **Run the application**
   ```bash
   dotnet run --project src/FMSLogNexus.Api
   ```

6. **Run tests**
   ```bash
   dotnet test
   ```

---

## How to Contribute

### Reporting Bugs

1. **Check existing issues** to avoid duplicates
2. **Use the bug report template**
3. **Include**:
   - Clear title and description
   - Steps to reproduce
   - Expected vs actual behavior
   - Environment details (OS, .NET version, etc.)
   - Logs or error messages

### Suggesting Features

1. **Check existing feature requests**
2. **Use the feature request template**
3. **Describe**:
   - The problem you're trying to solve
   - Your proposed solution
   - Alternative solutions considered
   - Impact on existing functionality

### Contributing Code

1. **Find an issue** to work on or create one
2. **Comment** on the issue to claim it
3. **Fork and clone** the repository
4. **Create a feature branch**
5. **Make your changes**
6. **Write/update tests**
7. **Submit a pull request**

---

## Development Process

### Branch Strategy

```
main (protected)
  └── develop
        ├── feature/add-webhook-support
        ├── feature/improve-search
        ├── bugfix/fix-timeout-issue
        └── hotfix/security-patch
```

### Branch Naming

| Type | Format | Example |
|------|--------|---------|
| Feature | `feature/<description>` | `feature/add-email-notifications` |
| Bug Fix | `bugfix/<issue-number>-<description>` | `bugfix/123-fix-login-error` |
| Hot Fix | `hotfix/<description>` | `hotfix/security-vulnerability` |
| Release | `release/<version>` | `release/1.1.0` |

### Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `style`: Code style (formatting, semicolons, etc.)
- `refactor`: Code refactoring
- `perf`: Performance improvement
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

**Examples:**
```
feat(alerts): add email notification support

Add SMTP configuration and email service for sending
alert notifications to configured recipients.

Closes #456
```

```
fix(api): handle null reference in log search

Check for null before accessing properties in
LogRepository.SearchAsync method.

Fixes #789
```

---

## Pull Request Guidelines

### Before Submitting

- [ ] Code follows project style guidelines
- [ ] All tests pass (`dotnet test`)
- [ ] New code has test coverage
- [ ] Documentation is updated
- [ ] Commit messages follow conventions
- [ ] Branch is up to date with `develop`

### PR Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
Describe testing performed

## Checklist
- [ ] Tests pass
- [ ] Documentation updated
- [ ] No breaking changes (or documented)

## Related Issues
Closes #123
```

### Review Process

1. **Automated checks** run (CI/CD)
2. **Code review** by maintainers
3. **Address feedback**
4. **Approval** from at least one maintainer
5. **Merge** to develop branch

---

## Coding Standards

### C# Style Guide

```csharp
// File-scoped namespaces
namespace FMSLogNexus.Core.Services;

// Interface names start with I
public interface ILogService { }

// Async methods end with Async
public async Task<LogResponse> GetByIdAsync(Guid id)

// Use expression bodies for simple members
public string FullName => $"{FirstName} {LastName}";

// Null-conditional operators
var name = user?.Name ?? "Unknown";

// Use pattern matching
if (obj is LogEntry log)
{
    // Use log
}
```

### Project Structure

- One class per file
- File name matches class name
- Group related files in folders
- Keep files under 500 lines
- Keep methods under 50 lines

### Dependency Injection

```csharp
// Register services in Program.cs or extension methods
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLogServices(this IServiceCollection services)
    {
        services.AddScoped<ILogService, LogService>();
        services.AddScoped<ILogRepository, LogRepository>();
        return services;
    }
}
```

---

## Testing Requirements

### Test Coverage

- Minimum 80% code coverage for new code
- All public APIs must have tests
- Critical paths require integration tests

### Test Organization

```
tests/
├── FMSLogNexus.Tests.Unit/
│   ├── Services/
│   │   └── LogServiceTests.cs
│   ├── Controllers/
│   │   └── LogsControllerTests.cs
│   └── Entities/
│       └── LogEntryTests.cs
│
└── FMSLogNexus.Tests.Integration/
    ├── Api/
    │   └── LogsApiTests.cs
    └── Database/
        └── LogRepositoryTests.cs
```

### Test Naming

```csharp
[Fact]
public async Task MethodName_StateUnderTest_ExpectedBehavior()
{
    // Arrange
    // Act
    // Assert
}

// Examples:
// CreateAsync_WithValidRequest_ReturnsCreatedLog
// GetByIdAsync_WithInvalidId_ReturnsNull
// SearchAsync_WithDateFilter_ReturnsFilteredResults
```

### Running Tests

```bash
# All tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Specific project
dotnet test tests/FMSLogNexus.Tests.Unit
```

---

## Documentation

### Code Documentation

```csharp
/// <summary>
/// Creates a new log entry.
/// </summary>
/// <param name="request">The log entry details.</param>
/// <returns>The created log entry response.</returns>
/// <exception cref="ValidationException">Thrown when request is invalid.</exception>
public async Task<LogResponse> CreateAsync(CreateLogRequest request)
```

### API Documentation

- Update Swagger annotations
- Include request/response examples
- Document error responses

### User Documentation

- Update relevant docs in `/docs`
- Include screenshots where helpful
- Keep language clear and concise

---

## Getting Help

- **Questions**: Open a [Discussion](https://github.com/your-org/fms-log-nexus/discussions)
- **Bugs**: Open an [Issue](https://github.com/your-org/fms-log-nexus/issues)
- **Chat**: Join our [Discord](https://discord.gg/fms-lognexus)

---

## Recognition

Contributors are recognized in:
- CHANGELOG.md
- GitHub Contributors page
- Release notes

Thank you for contributing! 🎉
