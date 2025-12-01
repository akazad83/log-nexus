#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Install FMS Log Nexus Collector as Windows Service

.DESCRIPTION
    Publishes and installs a collector (FileLog or EventLog) as a Windows Service.

.PARAMETER CollectorType
    Type of collector: FileLog or EventLog

.PARAMETER ServiceName
    Windows service name (default: FMSLogNexus.Collector.{Type})

.PARAMETER DisplayName
    Service display name

.PARAMETER InstallPath
    Installation directory (default: C:\Program Files\FMSLogNexus\Collectors\{Type})

.PARAMETER StartupType
    Service startup type: Automatic, Manual, Disabled (default: Automatic)

.PARAMETER ServiceAccount
    Account to run service as (default: LocalSystem)

.EXAMPLE
    .\Install-CollectorService.ps1 -CollectorType FileLog

.EXAMPLE
    .\Install-CollectorService.ps1 -CollectorType EventLog -ServiceAccount "DOMAIN\ServiceAccount"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("FileLog", "EventLog")]
    [string]$CollectorType,

    [Parameter()]
    [string]$ServiceName = $null,

    [Parameter()]
    [string]$DisplayName = $null,

    [Parameter()]
    [string]$InstallPath = $null,

    [Parameter()]
    [ValidateSet("Automatic", "Manual", "Disabled")]
    [string]$StartupType = "Automatic",

    [Parameter()]
    [string]$ServiceAccount = "LocalSystem",

    [Parameter()]
    [string]$SourcePath = $null,

    [Parameter()]
    [switch]$Uninstall
)

$ErrorActionPreference = "Stop"

# Set defaults based on collector type
if (-not $ServiceName) {
    $ServiceName = "FMSLogNexus.Collector.$CollectorType"
}
if (-not $DisplayName) {
    $DisplayName = "FMS Log Nexus - $CollectorType Collector"
}
if (-not $InstallPath) {
    $InstallPath = "C:\Program Files\FMSLogNexus\Collectors\$CollectorType"
}

$projectName = "FMSLogNexus.Collectors.$CollectorType"

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  FMS Log Nexus - Collector Service Installation" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Collector:     $CollectorType" -ForegroundColor White
Write-Host "  Service Name:  $ServiceName" -ForegroundColor White
Write-Host "  Display Name:  $DisplayName" -ForegroundColor White
Write-Host "  Install Path:  $InstallPath" -ForegroundColor White
Write-Host "  Startup Type:  $StartupType" -ForegroundColor White
Write-Host "  Account:       $ServiceAccount" -ForegroundColor White
Write-Host ""

# Check for admin rights
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "This script requires Administrator privileges. Please run as Administrator."
    exit 1
}

# Handle uninstall
if ($Uninstall) {
    Write-Host "  Uninstalling service..." -ForegroundColor Yellow

    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service) {
        if ($service.Status -ne 'Stopped') {
            Stop-Service -Name $ServiceName -Force
            Start-Sleep -Seconds 2
        }
        sc.exe delete $ServiceName | Out-Null
        Write-Host "  ✓ Service removed" -ForegroundColor Green
    } else {
        Write-Host "  Service not found" -ForegroundColor Yellow
    }

    $response = Read-Host "  Remove installation files? (y/N)"
    if ($response -eq 'y' -or $response -eq 'Y') {
        if (Test-Path $InstallPath) {
            Remove-Item -Path $InstallPath -Recurse -Force
            Write-Host "  ✓ Files removed" -ForegroundColor Green
        }
    }

    Write-Host ""
    Write-Host "  Uninstallation complete." -ForegroundColor Green
    exit 0
}

# Determine source path
if (-not $SourcePath) {
    $SourcePath = Join-Path $PSScriptRoot "..\..\src\$projectName"
    if (-not (Test-Path $SourcePath)) {
        $SourcePath = Join-Path $PSScriptRoot "..\..\..\src\$projectName"
    }
}

$projectFile = Join-Path $SourcePath "$projectName.csproj"
if (-not (Test-Path $projectFile)) {
    Write-Error "Cannot find project at: $projectFile"
    exit 1
}

# Stop existing service
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "  Stopping existing service..." -ForegroundColor White
    if ($existingService.Status -ne 'Stopped') {
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 3
    }
    Write-Host "  ✓ Service stopped" -ForegroundColor Green
}

# Build and publish
Write-Host "  Building and publishing..." -ForegroundColor White

$publishResult = dotnet publish $projectFile `
    --configuration Release `
    --output $InstallPath `
    --no-self-contained `
    --runtime win-x64 `
    2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed: $publishResult"
    exit 1
}

Write-Host "  ✓ Build completed" -ForegroundColor Green

# Find executable
$exePath = Join-Path $InstallPath "$projectName.exe"
if (-not (Test-Path $exePath)) {
    Write-Error "Executable not found: $exePath"
    exit 1
}

# Install/update service
if ($existingService) {
    Write-Host "  Updating service configuration..." -ForegroundColor White
    sc.exe config $ServiceName binPath= "`"$exePath`"" start= auto | Out-Null
} else {
    Write-Host "  Installing service..." -ForegroundColor White

    $scArgs = @(
        "create", $ServiceName,
        "binPath=", "`"$exePath`"",
        "DisplayName=", "`"$DisplayName`"",
        "start=", $StartupType.ToLower()
    )

    if ($ServiceAccount -ne "LocalSystem") {
        # For domain accounts, you'd need to handle password securely
        $scArgs += @("obj=", $ServiceAccount)
    }

    $result = sc.exe @scArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create service: $result"
        exit 1
    }
}

# Set description
$description = "FMS Log Nexus $CollectorType collector service. Collects and forwards logs to the central Log Nexus server."
sc.exe description $ServiceName $description | Out-Null

# Set recovery options (restart on failure)
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null

Write-Host "  ✓ Service installed" -ForegroundColor Green

# Start service
Write-Host "  Starting service..." -ForegroundColor White
Start-Service -Name $ServiceName
Start-Sleep -Seconds 2

$service = Get-Service -Name $ServiceName
Write-Host "  ✓ Service status: $($service.Status)" -ForegroundColor Green

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  Installation Complete!" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "  Service Management Commands:" -ForegroundColor Cyan
Write-Host "    Start:   Start-Service -Name '$ServiceName'" -ForegroundColor Gray
Write-Host "    Stop:    Stop-Service -Name '$ServiceName'" -ForegroundColor Gray
Write-Host "    Status:  Get-Service -Name '$ServiceName'" -ForegroundColor Gray
Write-Host "    Logs:    Get-EventLog -LogName Application -Source '$ServiceName'" -ForegroundColor Gray
Write-Host ""
Write-Host "  Configuration:" -ForegroundColor Cyan
Write-Host "    Edit: $InstallPath\appsettings.json" -ForegroundColor Gray
Write-Host ""
