#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploy FMS Log Nexus database schema

.DESCRIPTION
    Runs all database migration scripts in order against the specified SQL Server instance.

.PARAMETER ServerInstance
    SQL Server instance name (default: localhost)

.PARAMETER DatabaseName
    Database name (default: FMSLogNexus)

.PARAMETER IntegratedSecurity
    Use Windows Authentication (default: true)

.PARAMETER Username
    SQL Server username (if not using integrated security)

.PARAMETER Password
    SQL Server password (if not using integrated security)

.EXAMPLE
    .\Deploy-Database.ps1 -ServerInstance "SQLSERVER01" -DatabaseName "FMSLogNexus"

.EXAMPLE
    .\Deploy-Database.ps1 -ServerInstance "SQLSERVER01" -Username "sa" -Password "MyPassword"
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$ServerInstance = "localhost",

    [Parameter()]
    [string]$DatabaseName = "FMSLogNexus",

    [Parameter()]
    [switch]$IntegratedSecurity = $true,

    [Parameter()]
    [string]$Username,

    [Parameter()]
    [string]$Password,

    [Parameter()]
    [string]$ScriptsPath = $null
)

$ErrorActionPreference = "Stop"

# Determine scripts path
if (-not $ScriptsPath) {
    $ScriptsPath = Join-Path $PSScriptRoot "..\database"
    if (-not (Test-Path $ScriptsPath)) {
        $ScriptsPath = Join-Path $PSScriptRoot "..\..\database"
    }
}

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  FMS Log Nexus - Database Deployment" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Server:   $ServerInstance" -ForegroundColor White
Write-Host "  Database: $DatabaseName" -ForegroundColor White
Write-Host "  Scripts:  $ScriptsPath" -ForegroundColor White
Write-Host ""

# Build connection string
if ($IntegratedSecurity -and -not $Username) {
    $connectionString = "Server=$ServerInstance;Database=master;Integrated Security=true;TrustServerCertificate=true"
    $dbConnectionString = "Server=$ServerInstance;Database=$DatabaseName;Integrated Security=true;TrustServerCertificate=true"
} else {
    if (-not $Username -or -not $Password) {
        Write-Error "Username and Password required when not using Integrated Security"
        exit 1
    }
    $connectionString = "Server=$ServerInstance;Database=master;User Id=$Username;Password=$Password;TrustServerCertificate=true"
    $dbConnectionString = "Server=$ServerInstance;Database=$DatabaseName;User Id=$Username;Password=$Password;TrustServerCertificate=true"
}

# Get ordered list of SQL scripts
$scripts = Get-ChildItem -Path $ScriptsPath -Filter "*.sql" | Sort-Object Name

if ($scripts.Count -eq 0) {
    Write-Error "No SQL scripts found in $ScriptsPath"
    exit 1
}

Write-Host "  Found $($scripts.Count) script(s) to execute:" -ForegroundColor Gray
foreach ($script in $scripts) {
    Write-Host "    - $($script.Name)" -ForegroundColor Gray
}
Write-Host ""

# Confirm
$response = Read-Host "  Continue with deployment? (y/N)"
if ($response -ne 'y' -and $response -ne 'Y') {
    Write-Host "  Deployment cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""

# Execute scripts
foreach ($script in $scripts) {
    Write-Host "  Executing: $($script.Name)..." -ForegroundColor White -NoNewline

    try {
        # Use sqlcmd if available
        $sqlcmdPath = Get-Command sqlcmd -ErrorAction SilentlyContinue

        if ($sqlcmdPath) {
            if ($IntegratedSecurity -and -not $Username) {
                $result = sqlcmd -S $ServerInstance -E -i $script.FullName -b 2>&1
            } else {
                $result = sqlcmd -S $ServerInstance -U $Username -P $Password -i $script.FullName -b 2>&1
            }

            if ($LASTEXITCODE -ne 0) {
                throw "sqlcmd failed with exit code $LASTEXITCODE"
            }
        } else {
            # Fall back to Invoke-Sqlcmd if available (requires SqlServer module)
            $sqlContent = Get-Content -Path $script.FullName -Raw

            if (Get-Command Invoke-Sqlcmd -ErrorAction SilentlyContinue) {
                if ($IntegratedSecurity -and -not $Username) {
                    Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $sqlContent -ErrorAction Stop
                } else {
                    $securePassword = ConvertTo-SecureString $Password -AsPlainText -Force
                    $credential = New-Object System.Management.Automation.PSCredential($Username, $securePassword)
                    Invoke-Sqlcmd -ServerInstance $ServerInstance -Credential $credential -Query $sqlContent -ErrorAction Stop
                }
            } else {
                throw "Neither sqlcmd nor Invoke-Sqlcmd is available. Please install SQL Server tools."
            }
        }

        Write-Host " Done" -ForegroundColor Green
    }
    catch {
        Write-Host " FAILED" -ForegroundColor Red
        Write-Host ""
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        exit 1
    }
}

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  Database deployment completed successfully!" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
