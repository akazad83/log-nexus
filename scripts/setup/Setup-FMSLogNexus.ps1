#!/usr/bin/env pwsh
#Requires -Version 5.1
<#
.SYNOPSIS
    FMS Log Nexus - Solution Setup Script
    
.DESCRIPTION
    Creates the complete .NET solution structure for FMS Log Nexus including:
    - Solution file and all projects
    - NuGet package references
    - Directory structure
    - Configuration files
    - Build props and targets
    
.PARAMETER OutputPath
    The root directory where the solution will be created. Defaults to current directory.
    
.PARAMETER SkipRestore
    Skip NuGet package restore after creating the solution.
    
.PARAMETER SkipBuild
    Skip building the solution after creation.

.EXAMPLE
    .\Setup-FMSLogNexus.ps1 -OutputPath "C:\Projects\FMSLogNexus"
    
.EXAMPLE
    .\Setup-FMSLogNexus.ps1 -SkipBuild
    
.NOTES
    Author: FMS Log Nexus Team
    Version: 1.0.0
    Requires: .NET 8 SDK
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$OutputPath = (Get-Location).Path,
    
    [Parameter()]
    [switch]$SkipRestore,
    
    [Parameter()]
    [switch]$SkipBuild
)

# ============================================================================
# Configuration
# ============================================================================
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$SolutionName = "FMSLogNexus"
$DotNetVersion = "8.0"
$LangVersion = "12.0"

# NuGet Package Versions (Central Package Management)
$PackageVersions = @{
    # Microsoft.Extensions
    "Microsoft.Extensions.Hosting" = "8.0.0"
    "Microsoft.Extensions.Logging" = "8.0.0"
    "Microsoft.Extensions.Logging.Abstractions" = "8.0.0"
    "Microsoft.Extensions.Configuration" = "8.0.0"
    "Microsoft.Extensions.Configuration.Json" = "8.0.0"
    "Microsoft.Extensions.DependencyInjection" = "8.0.0"
    "Microsoft.Extensions.DependencyInjection.Abstractions" = "8.0.0"
    "Microsoft.Extensions.Http" = "8.0.0"
    "Microsoft.Extensions.Caching.Memory" = "8.0.0"
    "Microsoft.Extensions.Options" = "8.0.0"
    
    # ASP.NET Core
    "Microsoft.AspNetCore.Authentication.JwtBearer" = "8.0.0"
    "Microsoft.AspNetCore.SignalR.Client" = "8.0.0"
    
    # Entity Framework Core
    "Microsoft.EntityFrameworkCore" = "8.0.0"
    "Microsoft.EntityFrameworkCore.SqlServer" = "8.0.0"
    "Microsoft.EntityFrameworkCore.Design" = "8.0.0"
    "Microsoft.EntityFrameworkCore.Tools" = "8.0.0"
    
    # Data & Serialization
    "System.Text.Json" = "8.0.0"
    "Dapper" = "2.1.28"
    "Microsoft.Data.SqlClient" = "5.1.4"
    
    # Security
    "BCrypt.Net-Next" = "4.0.3"
    "System.IdentityModel.Tokens.Jwt" = "7.2.0"
    
    # Utilities
    "Polly" = "8.2.1"
    "Polly.Extensions.Http" = "3.0.0"
    "Serilog" = "3.1.1"
    "Serilog.AspNetCore" = "8.0.0"
    "Serilog.Sinks.Console" = "5.0.1"
    "Serilog.Sinks.File" = "5.0.0"
    "FluentValidation" = "11.9.0"
    "FluentValidation.DependencyInjectionExtensions" = "11.9.0"
    "AutoMapper" = "13.0.1"
    "Swashbuckle.AspNetCore" = "6.5.0"
    "System.Threading.Channels" = "8.0.0"
    
    # Health Checks
    "AspNetCore.HealthChecks.SqlServer" = "8.0.0"
    "AspNetCore.HealthChecks.UI.Client" = "8.0.0"
    
    # Testing
    "Microsoft.NET.Test.Sdk" = "17.8.0"
    "xunit" = "2.6.4"
    "xunit.runner.visualstudio" = "2.5.6"
    "Moq" = "4.20.70"
    "FluentAssertions" = "6.12.0"
    "Microsoft.AspNetCore.Mvc.Testing" = "8.0.0"
    "coverlet.collector" = "6.0.0"
}

# ============================================================================
# Helper Functions
# ============================================================================

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "  ✓ $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "  → $Message" -ForegroundColor Gray
}

function Write-Warning {
    param([string]$Message)
    Write-Host "  ⚠ $Message" -ForegroundColor Yellow
}

function Test-DotNetSdk {
    try {
        $dotnetVersion = dotnet --version
        if ($dotnetVersion -like "8.*") {
            Write-Success ".NET SDK $dotnetVersion found"
            return $true
        } else {
            Write-Warning ".NET SDK $dotnetVersion found, but .NET 8 is recommended"
            return $true
        }
    }
    catch {
        Write-Error ".NET SDK not found. Please install .NET 8 SDK from https://dotnet.microsoft.com/download"
        return $false
    }
}

function New-Directory {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function New-File {
    param(
        [string]$Path,
        [string]$Content
    )
    $directory = Split-Path -Path $Path -Parent
    if ($directory -and -not (Test-Path $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
    Set-Content -Path $Path -Value $Content -Encoding UTF8
}

# ============================================================================
# Main Script
# ============================================================================

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║                                                              ║" -ForegroundColor Magenta
Write-Host "║              FMS LOG NEXUS - SOLUTION SETUP                  ║" -ForegroundColor Magenta
Write-Host "║                                                              ║" -ForegroundColor Magenta
Write-Host "║  Centralized Logging and Monitoring Platform                 ║" -ForegroundColor Magenta
Write-Host "║                                                              ║" -ForegroundColor Magenta
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
Write-Host ""

# Verify prerequisites
Write-Step "Checking Prerequisites"

if (-not (Test-DotNetSdk)) {
    exit 1
}

# Set up paths
$SolutionRoot = Join-Path $OutputPath $SolutionName
Write-Info "Solution will be created at: $SolutionRoot"

if (Test-Path $SolutionRoot) {
    Write-Warning "Directory already exists: $SolutionRoot"
    $response = Read-Host "  Do you want to continue and overwrite? (y/N)"
    if ($response -ne 'y' -and $response -ne 'Y') {
        Write-Host "  Aborted." -ForegroundColor Red
        exit 0
    }
}

# Create directory structure
Write-Step "Creating Directory Structure"

$directories = @(
    "src/FMSLogNexus.Core/Entities"
    "src/FMSLogNexus.Core/Enums"
    "src/FMSLogNexus.Core/DTOs/Requests"
    "src/FMSLogNexus.Core/DTOs/Responses"
    "src/FMSLogNexus.Core/Interfaces/Repositories"
    "src/FMSLogNexus.Core/Interfaces/Services"
    "src/FMSLogNexus.Core/Constants"
    "src/FMSLogNexus.Core/Exceptions"
    "src/FMSLogNexus.Core/Extensions"
    "src/FMSLogNexus.Infrastructure/Data/Configurations"
    "src/FMSLogNexus.Infrastructure/Data/Migrations"
    "src/FMSLogNexus.Infrastructure/Repositories"
    "src/FMSLogNexus.Infrastructure/Services"
    "src/FMSLogNexus.Infrastructure/Extensions"
    "src/FMSLogNexus.Api/Controllers"
    "src/FMSLogNexus.Api/Hubs"
    "src/FMSLogNexus.Api/Services"
    "src/FMSLogNexus.Api/Middleware"
    "src/FMSLogNexus.Api/BackgroundServices"
    "src/FMSLogNexus.Api/Configuration"
    "src/FMSLogNexus.Api/Validators"
    "src/FMSLogNexus.Api/Mapping"
    "src/FMSLogNexus.LogClient/Core"
    "src/FMSLogNexus.LogClient/Transport"
    "src/FMSLogNexus.LogClient/Extensions"
    "src/FMSLogNexus.LogClient.PowerShell/Cmdlets"
    "src/FMSLogNexus.Collectors.FileLog/Services"
    "src/FMSLogNexus.Collectors.FileLog/Parsers"
    "src/FMSLogNexus.Collectors.EventLog/Services"
    "tests/FMSLogNexus.Core.Tests"
    "tests/FMSLogNexus.Infrastructure.Tests"
    "tests/FMSLogNexus.Api.Tests/Controllers"
    "tests/FMSLogNexus.Api.Tests/Integration"
    "tests/FMSLogNexus.LogClient.Tests"
    "scripts/setup"
    "scripts/deployment"
    "scripts/database"
    "docs/api"
    "docs/architecture"
)

foreach ($dir in $directories) {
    $fullPath = Join-Path $SolutionRoot $dir
    New-Directory -Path $fullPath
    Write-Info "Created: $dir"
}

Write-Success "Directory structure created"

# ============================================================================
# Create Solution-Level Files
# ============================================================================
Write-Step "Creating Solution-Level Files"

# global.json
$globalJson = @"
{
  "sdk": {
    "version": "$DotNetVersion.0",
    "rollForward": "latestFeature"
  }
}
"@
New-File -Path (Join-Path $SolutionRoot "global.json") -Content $globalJson
Write-Info "Created: global.json"

# Directory.Build.props
$directoryBuildProps = @"
<Project>
  <PropertyGroup>
    <TargetFramework>net$DotNetVersion</TargetFramework>
    <LangVersion>$LangVersion</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>`$(NoWarn);1591</NoWarn>
    
    <!-- Assembly Info -->
    <Company>Your Company</Company>
    <Product>FMS Log Nexus</Product>
    <Copyright>Copyright © `$(Company) `$([System.DateTime]::Now.Year)</Copyright>
    
    <!-- Version Info -->
    <VersionPrefix>1.0.0</VersionPrefix>
    <VersionSuffix></VersionSuffix>
    
    <!-- Build Settings -->
    <Deterministic>true</Deterministic>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
  </PropertyGroup>
  
  <PropertyGroup Condition="'`$(Configuration)' == 'Release'">
    <DebugType>none</DebugType>
    <DebugSymbols>false</DebugSymbols>
    <Optimize>true</Optimize>
  </PropertyGroup>
  
  <ItemGroup>
    <Using Include="System" />
    <Using Include="System.Collections.Generic" />
    <Using Include="System.Linq" />
    <Using Include="System.Threading.Tasks" />
    <Using Include="System.Threading" />
  </ItemGroup>
</Project>
"@
New-File -Path (Join-Path $SolutionRoot "Directory.Build.props") -Content $directoryBuildProps
Write-Info "Created: Directory.Build.props"

# Directory.Packages.props (Central Package Management)
$packagePropsContent = @"
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Microsoft.Extensions -->
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="$($PackageVersions['Microsoft.Extensions.Hosting'])" />
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="$($PackageVersions['Microsoft.Extensions.Logging'])" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="$($PackageVersions['Microsoft.Extensions.Logging.Abstractions'])" />
    <PackageVersion Include="Microsoft.Extensions.Configuration" Version="$($PackageVersions['Microsoft.Extensions.Configuration'])" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Json" Version="$($PackageVersions['Microsoft.Extensions.Configuration.Json'])" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="$($PackageVersions['Microsoft.Extensions.DependencyInjection'])" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="$($PackageVersions['Microsoft.Extensions.DependencyInjection.Abstractions'])" />
    <PackageVersion Include="Microsoft.Extensions.Http" Version="$($PackageVersions['Microsoft.Extensions.Http'])" />
    <PackageVersion Include="Microsoft.Extensions.Caching.Memory" Version="$($PackageVersions['Microsoft.Extensions.Caching.Memory'])" />
    <PackageVersion Include="Microsoft.Extensions.Options" Version="$($PackageVersions['Microsoft.Extensions.Options'])" />
    
    <!-- ASP.NET Core -->
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="$($PackageVersions['Microsoft.AspNetCore.Authentication.JwtBearer'])" />
    <PackageVersion Include="Microsoft.AspNetCore.SignalR.Client" Version="$($PackageVersions['Microsoft.AspNetCore.SignalR.Client'])" />
    
    <!-- Entity Framework Core -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="$($PackageVersions['Microsoft.EntityFrameworkCore'])" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="$($PackageVersions['Microsoft.EntityFrameworkCore.SqlServer'])" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="$($PackageVersions['Microsoft.EntityFrameworkCore.Design'])" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Tools" Version="$($PackageVersions['Microsoft.EntityFrameworkCore.Tools'])" />
    
    <!-- Data & Serialization -->
    <PackageVersion Include="System.Text.Json" Version="$($PackageVersions['System.Text.Json'])" />
    <PackageVersion Include="Dapper" Version="$($PackageVersions['Dapper'])" />
    <PackageVersion Include="Microsoft.Data.SqlClient" Version="$($PackageVersions['Microsoft.Data.SqlClient'])" />
    
    <!-- Security -->
    <PackageVersion Include="BCrypt.Net-Next" Version="$($PackageVersions['BCrypt.Net-Next'])" />
    <PackageVersion Include="System.IdentityModel.Tokens.Jwt" Version="$($PackageVersions['System.IdentityModel.Tokens.Jwt'])" />
    
    <!-- Utilities -->
    <PackageVersion Include="Polly" Version="$($PackageVersions['Polly'])" />
    <PackageVersion Include="Polly.Extensions.Http" Version="$($PackageVersions['Polly.Extensions.Http'])" />
    <PackageVersion Include="Serilog" Version="$($PackageVersions['Serilog'])" />
    <PackageVersion Include="Serilog.AspNetCore" Version="$($PackageVersions['Serilog.AspNetCore'])" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="$($PackageVersions['Serilog.Sinks.Console'])" />
    <PackageVersion Include="Serilog.Sinks.File" Version="$($PackageVersions['Serilog.Sinks.File'])" />
    <PackageVersion Include="FluentValidation" Version="$($PackageVersions['FluentValidation'])" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="$($PackageVersions['FluentValidation.DependencyInjectionExtensions'])" />
    <PackageVersion Include="AutoMapper" Version="$($PackageVersions['AutoMapper'])" />
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="$($PackageVersions['Swashbuckle.AspNetCore'])" />
    <PackageVersion Include="System.Threading.Channels" Version="$($PackageVersions['System.Threading.Channels'])" />
    
    <!-- Health Checks -->
    <PackageVersion Include="AspNetCore.HealthChecks.SqlServer" Version="$($PackageVersions['AspNetCore.HealthChecks.SqlServer'])" />
    <PackageVersion Include="AspNetCore.HealthChecks.UI.Client" Version="$($PackageVersions['AspNetCore.HealthChecks.UI.Client'])" />
    
    <!-- Testing -->
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="$($PackageVersions['Microsoft.NET.Test.Sdk'])" />
    <PackageVersion Include="xunit" Version="$($PackageVersions['xunit'])" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="$($PackageVersions['xunit.runner.visualstudio'])" />
    <PackageVersion Include="Moq" Version="$($PackageVersions['Moq'])" />
    <PackageVersion Include="FluentAssertions" Version="$($PackageVersions['FluentAssertions'])" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="$($PackageVersions['Microsoft.AspNetCore.Mvc.Testing'])" />
    <PackageVersion Include="coverlet.collector" Version="$($PackageVersions['coverlet.collector'])" />
  </ItemGroup>
</Project>
"@
New-File -Path (Join-Path $SolutionRoot "Directory.Packages.props") -Content $packagePropsContent
Write-Info "Created: Directory.Packages.props"

# .editorconfig
$editorConfig = @"
# EditorConfig - FMS Log Nexus
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.{cs,csx}]
indent_size = 4

# C# formatting rules
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# C# naming conventions
dotnet_naming_rule.private_fields_should_be_camel_case.severity = suggestion
dotnet_naming_rule.private_fields_should_be_camel_case.symbols = private_fields
dotnet_naming_rule.private_fields_should_be_camel_case.style = camel_case_underscore

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private

dotnet_naming_style.camel_case_underscore.required_prefix = _
dotnet_naming_style.camel_case_underscore.capitalization = camel_case

# C# code style
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion

csharp_prefer_braces = true:suggestion
csharp_using_directive_placement = outside_namespace:suggestion
csharp_style_namespace_declarations = file_scoped:suggestion
csharp_style_prefer_primary_constructors = true:suggestion

[*.{json,yaml,yml}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false

[*.sql]
indent_size = 4
"@
New-File -Path (Join-Path $SolutionRoot ".editorconfig") -Content $editorConfig
Write-Info "Created: .editorconfig"

# .gitignore
$gitignore = @"
## .NET
bin/
obj/
*.user
*.suo
*.userosscache
*.sln.docstates

## Visual Studio
.vs/
*.rsuser
*.cache
*.log

## Build results
[Dd]ebug/
[Rr]elease/
x64/
x86/
[Ww][Ii][Nn]32/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Oo]ut/
msbuild.log
msbuild.err
msbuild.wrn

## NuGet
*.nupkg
*.snupkg
**/[Pp]ackages/*
!**/[Pp]ackages/build/

## Test Results
[Tt]est[Rr]esult*/
[Bb]uild[Ll]og.*
*.trx
*.coverage
*.coveragexml
coverage*.json
coverage*.xml
coverage/

## Secrets
appsettings.*.json
!appsettings.json
!appsettings.Development.json.template
*.pfx
*.key
secrets.json
.env
.env.*

## IDE
.idea/
*.swp
*~

## OS
.DS_Store
Thumbs.db

## Project specific
logs/
"@
New-File -Path (Join-Path $SolutionRoot ".gitignore") -Content $gitignore
Write-Info "Created: .gitignore"

# README.md
$readme = @"
# FMS Log Nexus

Centralized Logging and Monitoring Platform for distributed job processing environments.

## Overview

FMS Log Nexus aggregates, standardizes, and visualizes job execution data across multiple Windows servers, providing real-time visibility into job health and system status through an intuitive dashboard.

## Features

- **Centralized Logging**: Aggregate logs from VB.NET, VBScript, PowerShell, and .NET applications
- **Real-time Dashboard**: Monitor job health, server status, and log trends
- **Alerting System**: Configurable alerts for error thresholds, job failures, and server offline events
- **Structured Logging**: Consistent log format with rich metadata and correlation support
- **Legacy Integration**: Support for VBScript via COM wrapper, PowerShell module, and file/event log collectors

## Technology Stack

- **Backend**: .NET 8, ASP.NET Core Web API
- **Database**: SQL Server 2019+
- **Real-time**: SignalR
- **Frontend**: Angular 17+ with Kendo UI

## Project Structure

\`\`\`
src/
├── FMSLogNexus.Core/           # Domain layer (entities, interfaces, DTOs)
├── FMSLogNexus.Infrastructure/ # Data access, external services
├── FMSLogNexus.Api/            # Web API, SignalR, background services
├── FMSLogNexus.LogClient/      # NuGet package for .NET integration
├── FMSLogNexus.LogClient.PowerShell/  # PowerShell module
├── FMSLogNexus.Collectors.FileLog/    # File log collector service
└── FMSLogNexus.Collectors.EventLog/   # Event log collector service
\`\`\`

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server 2019+
- Node.js 18+ (for frontend)

### Build

\`\`\`powershell
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run API
dotnet run --project src/FMSLogNexus.Api
\`\`\`

### Database Setup

\`\`\`powershell
# Run database scripts in order
sqlcmd -S localhost -d master -i scripts/database/001_CreateSchema.sql
sqlcmd -S localhost -d FMSLogNexus -i scripts/database/002_SupportingObjects.sql
\`\`\`

## Configuration

See \`appsettings.json\` for configuration options:

- Database connection string
- JWT settings
- Alert configuration
- Retention policies

## Documentation

- [API Reference](docs/api/API_REFERENCE.md)
- [OpenAPI Specification](docs/api/openapi.yaml)
- [Architecture Guide](docs/architecture/)

## License

Proprietary - Internal Use Only
"@
New-File -Path (Join-Path $SolutionRoot "README.md") -Content $readme
Write-Info "Created: README.md"

Write-Success "Solution-level files created"

# ============================================================================
# Create Solution and Projects
# ============================================================================
Write-Step "Creating Solution and Projects"

Set-Location $SolutionRoot

# Create solution
dotnet new sln -n $SolutionName --force | Out-Null
Write-Info "Created: $SolutionName.sln"

# Create projects with dotnet new
$projects = @(
    @{ Name = "FMSLogNexus.Core"; Template = "classlib"; Path = "src/FMSLogNexus.Core" }
    @{ Name = "FMSLogNexus.Infrastructure"; Template = "classlib"; Path = "src/FMSLogNexus.Infrastructure" }
    @{ Name = "FMSLogNexus.Api"; Template = "webapi"; Path = "src/FMSLogNexus.Api" }
    @{ Name = "FMSLogNexus.LogClient"; Template = "classlib"; Path = "src/FMSLogNexus.LogClient" }
    @{ Name = "FMSLogNexus.LogClient.PowerShell"; Template = "classlib"; Path = "src/FMSLogNexus.LogClient.PowerShell" }
    @{ Name = "FMSLogNexus.Collectors.FileLog"; Template = "worker"; Path = "src/FMSLogNexus.Collectors.FileLog" }
    @{ Name = "FMSLogNexus.Collectors.EventLog"; Template = "worker"; Path = "src/FMSLogNexus.Collectors.EventLog" }
    @{ Name = "FMSLogNexus.Core.Tests"; Template = "xunit"; Path = "tests/FMSLogNexus.Core.Tests" }
    @{ Name = "FMSLogNexus.Infrastructure.Tests"; Template = "xunit"; Path = "tests/FMSLogNexus.Infrastructure.Tests" }
    @{ Name = "FMSLogNexus.Api.Tests"; Template = "xunit"; Path = "tests/FMSLogNexus.Api.Tests" }
    @{ Name = "FMSLogNexus.LogClient.Tests"; Template = "xunit"; Path = "tests/FMSLogNexus.LogClient.Tests" }
)

foreach ($project in $projects) {
    $projectPath = Join-Path $SolutionRoot $project.Path
    
    # Create project
    dotnet new $project.Template -n $project.Name -o $projectPath --force 2>&1 | Out-Null
    
    # Add to solution
    $csprojPath = Join-Path $projectPath "$($project.Name).csproj"
    dotnet sln add $csprojPath 2>&1 | Out-Null
    
    Write-Info "Created: $($project.Name)"
}

Write-Success "Projects created and added to solution"

# ============================================================================
# Update Project Files with Custom Content
# ============================================================================
Write-Step "Configuring Project Files"

# FMSLogNexus.Core.csproj
$coreProject = @"
<Project Sdk="Microsoft.NET.Sdk">
  
  <PropertyGroup>
    <RootNamespace>FMSLogNexus.Core</RootNamespace>
    <AssemblyName>FMSLogNexus.Core</AssemblyName>
    <Description>Core domain entities, interfaces, and DTOs for FMS Log Nexus</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="System.Text.Json" />
  </ItemGroup>

</Project>
"@
New-File -Path (Join-Path $SolutionRoot "src/FMSLogNexus.Core/FMSLogNexus.Core.csproj") -Content $coreProject
Write-Info "Configured: FMSLogNexus.Core.csproj"

# FMSLogNexus.Infrastructure.csproj
$infrastructureProject = @"
<Project Sdk="Microsoft.NET.Sdk">
  
  <PropertyGroup>
    <RootNamespace>FMSLogNexus.Infrastructure</RootNamespace>
    <AssemblyName>FMSLogNexus.Infrastructure</AssemblyName>
    <Description>Data access and infrastructure services for FMS Log Nexus</Description>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\FMSLogNexus.Core\FMSLogNexus.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
    <PackageReference Include="Microsoft.Extensions.Caching.Memory" />
    <PackageReference Include="Dapper" />
    <PackageReference Include="Microsoft.Data.SqlClient" />
    <PackageReference Include="Polly" />
    <PackageReference Include="BCrypt.Net-Next" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" />
  </ItemGroup>

</Project>
"@
New-File -Path (Join-Path $SolutionRoot "src/FMSLogNexus.Infrastructure/FMSLogNexus.Infrastructure.csproj") -Content $infrastructureProject
Write-Info "Configured: FMSLogNexus.Infrastructure.csproj"

# FMSLogNexus.Api.csproj
$apiProject = @"
<Project Sdk="Microsoft.NET.Sdk.Web">
  
  <PropertyGroup>
    <RootNamespace>FMSLogNexus.Api</RootNamespace>
    <AssemblyName>FMSLogNexus.Api</AssemblyName>
    <Description>Web API for FMS Log Nexus</Description>
    <UserSecretsId>fms-log-nexus-api</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\FMSLogNexus.Core\FMSLogNexus.Core.csproj" />
    <ProjectReference Include="..\FMSLogNexus.Infrastructure\FMSLogNexus.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Serilog.AspNetCore" />
    <PackageReference Include="Serilog.Sinks.Console" />
    <PackageReference Include="Serilog.Sinks.File" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="AutoMapper" />
    <PackageReference Include="Swashbuckle.AspNetCore" />
    <PackageReference Include="AspNetCore.HealthChecks.SqlServer" />
    <PackageReference Include="AspNetCore.HealthChecks.UI.Client" />
    <PackageReference Include="System.Threading.Channels" />
  </ItemGroup>

</Project>
"@
New-File -Path (Join-Path $SolutionRoot "src/FMSLogNexus.Api/FMSLogNexus.Api.csproj") -Content $apiProject
Write-Info "Configured: FMSLogNexus.Api.csproj"

# FMSLogNexus.LogClient.csproj
$logClientProject = @"
<Project Sdk="Microsoft.NET.Sdk">
  
  <PropertyGroup>
    <TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>
    <LangVersion>$LangVersion</LangVersion>
    <Nullable>enable</Nullable>
    <RootNamespace>FMSLogNexus.LogClient</RootNamespace>
    <AssemblyName>FMSLogNexus.LogClient</AssemblyName>
    <Description>Log client library for FMS Log Nexus - enables .NET applications to send structured logs</Description>
    
    <!-- NuGet Package Properties -->
    <PackageId>FMSLogNexus.LogClient</PackageId>
    <PackageVersion>1.0.0</PackageVersion>
    <Authors>FMS Log Nexus Team</Authors>
    <PackageDescription>Client library for integrating .NET applications with FMS Log Nexus centralized logging platform.</PackageDescription>
    <PackageTags>logging;monitoring;fms;nexus</PackageTags>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Http" />
    <PackageReference Include="Microsoft.Extensions.Options" />
    <PackageReference Include="System.Text.Json" />
    <PackageReference Include="System.Threading.Channels" />
    <PackageReference Include="Polly" />
  </ItemGroup>

</Project>
"@
New-File -Path (Join-Path $SolutionRoot "src/FMSLogNexus.LogClient/FMSLogNexus.LogClient.csproj") -Content $logClientProject
Write-Info "Configured: FMSLogNexus.LogClient.csproj"

# FMSLogNexus.LogClient.PowerShell.csproj
$psProject = @"
<Project Sdk="Microsoft.NET.Sdk">
  
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>$LangVersion</LangVersion>
    <RootNamespace>FMSLogNexus.LogClient.PowerShell</RootNamespace>
    <AssemblyName>FMSLogNexus.LogClient.PowerShell</AssemblyName>
    <Description>PowerShell module for FMS Log Nexus logging</Description>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\FMSLogNexus.LogClient\FMSLogNexus.LogClient.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="PowerShellStandard.Library" Version="5.1.1" PrivateAssets="all" />
  </ItemGroup>

</Project>
"@
New-File -Path (Join-Path $SolutionRoot "src/FMSLogNexus.LogClient.PowerShell/FMSLogNexus.LogClient.PowerShell.csproj") -Content $psProject
Write-Info "Configured: FMSLogNexus.LogClient.PowerShell.csproj"

# FMSLogNexus.Collectors.FileLog.csproj
$fileCollectorProject = @"
<Project Sdk="Microsoft.NET.Sdk.Worker">
  
  <PropertyGroup>
    <RootNamespace>FMSLogNexus.Collectors.FileLog</RootNamespace>
    <AssemblyName>FMSLogNexus.Collectors.FileLog</AssemblyName>
    <Description>Windows service that collects logs from files and forwards to FMS Log Nexus</Description>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\FMSLogNexus.LogClient\FMSLogNexus.LogClient.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" />
    <PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="8.0.0" />
    <PackageReference Include="Serilog.AspNetCore" />
    <PackageReference Include="Serilog.Sinks.Console" />
    <PackageReference Include="Serilog.Sinks.File" />
  </ItemGroup>

</Project>
"@
New-File -Path (Join-Path $SolutionRoot "src/FMSLogNexus.Collectors.FileLog/FMSLogNexus.Collectors.FileLog.csproj") -Content $fileCollectorProject
Write-Info "Configured: FMSLogNexus.Collectors.FileLog.csproj"

# FMSLogNexus.Collectors.EventLog.csproj
$eventCollectorProject = @"
<Project Sdk="Microsoft.NET.Sdk.Worker">
  
  <PropertyGroup>
    <RootNamespace>FMSLogNexus.Collectors.EventLog</RootNamespace>
    <AssemblyName>FMSLogNexus.Collectors.EventLog</AssemblyName>
    <Description>Windows service that collects Windows Event Logs and forwards to FMS Log Nexus</Description>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\FMSLogNexus.LogClient\FMSLogNexus.LogClient.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" />
    <PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="8.0.0" />
    <PackageReference Include="Serilog.AspNetCore" />
    <PackageReference Include="Serilog.Sinks.Console" />
    <PackageReference Include="Serilog.Sinks.File" />
    <PackageReference Include="System.Diagnostics.EventLog" Version="8.0.0" />
  </ItemGroup>

</Project>
"@
New-File -Path (Join-Path $SolutionRoot "src/FMSLogNexus.Collectors.EventLog/FMSLogNexus.Collectors.EventLog.csproj") -Content $eventCollectorProject
Write-Info "Configured: FMSLogNexus.Collectors.EventLog.csproj"

# Test Projects
$testProjectTemplate = @"
<Project Sdk="Microsoft.NET.Sdk">
  
  <PropertyGroup>
    <RootNamespace>PROJECT_NAMESPACE</RootNamespace>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="PROJECT_REFERENCE" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Moq" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="coverlet.collector">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

</Project>
"@

$testProjects = @(
    @{ Name = "FMSLogNexus.Core.Tests"; Reference = "..\..\src\FMSLogNexus.Core\FMSLogNexus.Core.csproj" }
    @{ Name = "FMSLogNexus.Infrastructure.Tests"; Reference = "..\..\src\FMSLogNexus.Infrastructure\FMSLogNexus.Infrastructure.csproj" }
    @{ Name = "FMSLogNexus.LogClient.Tests"; Reference = "..\..\src\FMSLogNexus.LogClient\FMSLogNexus.LogClient.csproj" }
)

foreach ($testProject in $testProjects) {
    $content = $testProjectTemplate -replace "PROJECT_NAMESPACE", $testProject.Name -replace "PROJECT_REFERENCE", $testProject.Reference
    New-File -Path (Join-Path $SolutionRoot "tests/$($testProject.Name)/$($testProject.Name).csproj") -Content $content
    Write-Info "Configured: $($testProject.Name).csproj"
}

# Api.Tests needs additional reference
$apiTestProject = @"
<Project Sdk="Microsoft.NET.Sdk">
  
  <PropertyGroup>
    <RootNamespace>FMSLogNexus.Api.Tests</RootNamespace>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\FMSLogNexus.Api\FMSLogNexus.Api.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Moq" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="coverlet.collector">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

</Project>
"@
New-File -Path (Join-Path $SolutionRoot "tests/FMSLogNexus.Api.Tests/FMSLogNexus.Api.Tests.csproj") -Content $apiTestProject
Write-Info "Configured: FMSLogNexus.Api.Tests.csproj"

Write-Success "Project files configured"

# ============================================================================
# Create Initial Source Files
# ============================================================================
Write-Step "Creating Initial Source Files"

# Core - GlobalUsings.cs
$coreGlobalUsings = @"
// Global using directives for FMSLogNexus.Core

global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.Linq;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Threading;
global using System.Threading.Tasks;
"@
New-File -Path (Join-Path $SolutionRoot "src/FMSLogNexus.Core/GlobalUsings.cs") -Content $coreGlobalUsings

# Infrastructure - GlobalUsings.cs
$infraGlobalUsings = @"
// Global using directives for FMSLogNexus.Infrastructure

global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;

global using FMSLogNexus.Core.Entities;
global using FMSLogNexus.Core.Enums;
global using FMSLogNexus.Core.Interfaces.Repositories;
global using FMSLogNexus.Core.Interfaces.Services;
"@
New-File -Path (Join-Path $SolutionRoot "src/FMSLogNexus.Infrastructure/GlobalUsings.cs") -Content $infraGlobalUsings

# Api - GlobalUsings.cs
$apiGlobalUsings = @"
// Global using directives for FMSLogNexus.Api

global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.SignalR;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;

global using FMSLogNexus.Core.DTOs.Requests;
global using FMSLogNexus.Core.DTOs.Responses;
global using FMSLogNexus.Core.Entities;
global using FMSLogNexus.Core.Enums;
global using FMSLogNexus.Core.Interfaces.Services;
"@
New-File -Path (Join-Path $SolutionRoot "src/FMSLogNexus.Api/GlobalUsings.cs") -Content $apiGlobalUsings

# Remove auto-generated files
$filesToRemove = @(
    "src/FMSLogNexus.Core/Class1.cs"
    "src/FMSLogNexus.Infrastructure/Class1.cs"
    "src/FMSLogNexus.LogClient/Class1.cs"
    "src/FMSLogNexus.LogClient.PowerShell/Class1.cs"
    "tests/FMSLogNexus.Core.Tests/UnitTest1.cs"
    "tests/FMSLogNexus.Infrastructure.Tests/UnitTest1.cs"
    "tests/FMSLogNexus.Api.Tests/UnitTest1.cs"
    "tests/FMSLogNexus.LogClient.Tests/UnitTest1.cs"
)

foreach ($file in $filesToRemove) {
    $filePath = Join-Path $SolutionRoot $file
    if (Test-Path $filePath) {
        Remove-Item $filePath -Force
    }
}

Write-Success "Initial source files created"

# ============================================================================
# Create appsettings.json
# ============================================================================
Write-Step "Creating Configuration Files"

$appsettings = @"
{
  "ConnectionStrings": {
    "LogNexusDb": "Server=localhost;Database=FMSLogNexus;Integrated Security=true;TrustServerCertificate=true"
  },
  "JwtSettings": {
    "Secret": "CHANGE_THIS_TO_A_SECURE_SECRET_KEY_AT_LEAST_32_CHARACTERS",
    "Issuer": "FMSLogNexus",
    "Audience": "FMSLogNexus.Dashboard",
    "ExpirationMinutes": 480
  },
  "ApiKeySettings": {
    "HeaderName": "X-API-Key"
  },
  "CacheSettings": {
    "DashboardStatsTtlSeconds": 30,
    "LogTrendsTtlSeconds": 60,
    "ServerHealthTtlSeconds": 60
  },
  "RetentionSettings": {
    "DefaultRetentionDays": 90,
    "ErrorRetentionDays": 180,
    "CriticalRetentionDays": 365,
    "CleanupTimeUtc": "02:00:00"
  },
  "AlertSettings": {
    "EvaluationIntervalSeconds": 30,
    "DefaultThrottleMinutes": 15,
    "EmailEnabled": false,
    "SmtpServer": "",
    "SmtpPort": 25,
    "FromAddress": "lognexus@company.com"
  },
  "IngestionSettings": {
    "MaxBatchSize": 1000,
    "ProcessingIntervalMs": 100,
    "MaxQueueSize": 50000
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  },
  "AllowedHosts": "*"
}
"@
New-File -Path (Join-Path $SolutionRoot "src/FMSLogNexus.Api/appsettings.json") -Content $appsettings

$appsettingsDev = @"
{
  "ConnectionStrings": {
    "LogNexusDb": "Server=localhost;Database=FMSLogNexus_Dev;Integrated Security=true;TrustServerCertificate=true"
  },
  "JwtSettings": {
    "Secret": "DEV_ONLY_SECRET_KEY_DO_NOT_USE_IN_PRODUCTION_32CHARS"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
"@
New-File -Path (Join-Path $SolutionRoot "src/FMSLogNexus.Api/appsettings.Development.json") -Content $appsettingsDev

Write-Info "Created: appsettings.json"
Write-Info "Created: appsettings.Development.json"

# Collector appsettings
$collectorSettings = @"
{
  "NexusApi": {
    "ServerUrl": "https://localhost:5001/api",
    "ApiKey": "YOUR_API_KEY_HERE",
    "BatchSize": 100,
    "FlushIntervalMs": 1000
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      { "Name": "Console" }
    ]
  }
}
"@
New-File -Path (Join-Path $SolutionRoot "src/FMSLogNexus.Collectors.FileLog/appsettings.json") -Content $collectorSettings
New-File -Path (Join-Path $SolutionRoot "src/FMSLogNexus.Collectors.EventLog/appsettings.json") -Content $collectorSettings

Write-Success "Configuration files created"

# ============================================================================
# Restore and Build
# ============================================================================
if (-not $SkipRestore) {
    Write-Step "Restoring NuGet Packages"
    
    $restoreResult = dotnet restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Packages restored successfully"
    } else {
        Write-Warning "Package restore had warnings/errors. Check output for details."
        Write-Host $restoreResult -ForegroundColor Yellow
    }
}

if (-not $SkipBuild) {
    Write-Step "Building Solution"
    
    $buildResult = dotnet build --no-restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Solution built successfully"
    } else {
        Write-Warning "Build had warnings/errors. Check output for details."
        Write-Host $buildResult -ForegroundColor Yellow
    }
}

# ============================================================================
# Summary
# ============================================================================
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║                                                              ║" -ForegroundColor Green
Write-Host "║                 SETUP COMPLETED SUCCESSFULLY                 ║" -ForegroundColor Green
Write-Host "║                                                              ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "  Solution created at: $SolutionRoot" -ForegroundColor White
Write-Host ""
Write-Host "  Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Open the solution in Visual Studio or VS Code" -ForegroundColor White
Write-Host "  2. Update connection string in appsettings.json" -ForegroundColor White
Write-Host "  3. Run database scripts (scripts/database/)" -ForegroundColor White
Write-Host "  4. Start implementing entity classes (src/FMSLogNexus.Core/Entities/)" -ForegroundColor White
Write-Host ""
Write-Host "  Useful Commands:" -ForegroundColor Cyan
Write-Host "    dotnet run --project src/FMSLogNexus.Api     # Run the API" -ForegroundColor Gray
Write-Host "    dotnet test                                   # Run all tests" -ForegroundColor Gray
Write-Host "    dotnet build                                  # Build solution" -ForegroundColor Gray
Write-Host ""

Set-Location $OutputPath
