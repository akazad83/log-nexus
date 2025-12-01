#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploy FMS Log Nexus API to IIS

.DESCRIPTION
    Publishes and deploys the FMS Log Nexus API to IIS.
    - Builds the API project in Release mode
    - Creates/updates IIS application pool
    - Creates/updates IIS website
    - Copies published files to target directory

.PARAMETER SiteName
    IIS site name (default: FMSLogNexus)

.PARAMETER AppPoolName
    IIS application pool name (default: FMSLogNexus)

.PARAMETER PhysicalPath
    Deployment target directory (default: C:\inetpub\FMSLogNexus)

.PARAMETER Port
    HTTP port (default: 80)

.PARAMETER HttpsPort
    HTTPS port (default: 443)

.PARAMETER CertificateThumbprint
    SSL certificate thumbprint for HTTPS binding

.PARAMETER SourcePath
    Path to the solution/API project

.EXAMPLE
    .\Deploy-ToIIS.ps1 -SiteName "FMSLogNexus" -Port 8080

.EXAMPLE
    .\Deploy-ToIIS.ps1 -CertificateThumbprint "ABC123..." -HttpsPort 443
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$SiteName = "FMSLogNexus",

    [Parameter()]
    [string]$AppPoolName = "FMSLogNexus",

    [Parameter()]
    [string]$PhysicalPath = "C:\inetpub\FMSLogNexus",

    [Parameter()]
    [int]$Port = 80,

    [Parameter()]
    [int]$HttpsPort = 443,

    [Parameter()]
    [string]$CertificateThumbprint,

    [Parameter()]
    [string]$SourcePath = $null,

    [Parameter()]
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

# Import WebAdministration module
Import-Module WebAdministration -ErrorAction Stop

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  FMS Log Nexus - IIS Deployment" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Determine source path
if (-not $SourcePath) {
    $SourcePath = Join-Path $PSScriptRoot "..\..\src\FMSLogNexus.Api"
    if (-not (Test-Path $SourcePath)) {
        $SourcePath = Join-Path $PSScriptRoot "..\..\..\src\FMSLogNexus.Api"
    }
}

$projectFile = Join-Path $SourcePath "FMSLogNexus.Api.csproj"
if (-not (Test-Path $projectFile)) {
    Write-Error "Cannot find API project at: $projectFile"
    exit 1
}

Write-Host "  Site Name:     $SiteName" -ForegroundColor White
Write-Host "  App Pool:      $AppPoolName" -ForegroundColor White
Write-Host "  Physical Path: $PhysicalPath" -ForegroundColor White
Write-Host "  HTTP Port:     $Port" -ForegroundColor White
if ($CertificateThumbprint) {
    Write-Host "  HTTPS Port:    $HttpsPort" -ForegroundColor White
}
Write-Host ""

# Build and publish
if (-not $SkipBuild) {
    Write-Host "  Building and publishing..." -ForegroundColor White

    $publishPath = Join-Path $env:TEMP "FMSLogNexus_Publish_$(Get-Date -Format 'yyyyMMddHHmmss')"

    $publishResult = dotnet publish $projectFile `
        --configuration Release `
        --output $publishPath `
        --no-self-contained `
        2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed: $publishResult"
        exit 1
    }

    Write-Host "  ✓ Build completed" -ForegroundColor Green
} else {
    Write-Host "  Skipping build (using existing files)" -ForegroundColor Yellow
    $publishPath = $PhysicalPath
}

# Stop existing site/pool
Write-Host "  Stopping existing site..." -ForegroundColor White

if (Test-Path "IIS:\Sites\$SiteName") {
    Stop-Website -Name $SiteName -ErrorAction SilentlyContinue
}

if (Test-Path "IIS:\AppPools\$AppPoolName") {
    Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

# Create physical directory
if (-not (Test-Path $PhysicalPath)) {
    New-Item -ItemType Directory -Path $PhysicalPath -Force | Out-Null
    Write-Host "  ✓ Created directory: $PhysicalPath" -ForegroundColor Green
}

# Copy files
if (-not $SkipBuild) {
    Write-Host "  Copying files..." -ForegroundColor White

    # Clear existing files (except logs and data)
    Get-ChildItem -Path $PhysicalPath -Exclude "logs", "data", "appsettings.Production.json" |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

    Copy-Item -Path "$publishPath\*" -Destination $PhysicalPath -Recurse -Force

    Write-Host "  ✓ Files copied" -ForegroundColor Green

    # Cleanup temp publish folder
    Remove-Item -Path $publishPath -Recurse -Force -ErrorAction SilentlyContinue
}

# Create/configure application pool
Write-Host "  Configuring application pool..." -ForegroundColor White

if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
    New-WebAppPool -Name $AppPoolName | Out-Null
    Write-Host "  ✓ Created application pool" -ForegroundColor Green
}

Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "startMode" -Value "AlwaysRunning"
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "processModel.idleTimeout" -Value ([TimeSpan]::FromMinutes(0))
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "recycling.periodicRestart.time" -Value ([TimeSpan]::FromHours(0))

Write-Host "  ✓ Application pool configured" -ForegroundColor Green

# Create/configure website
Write-Host "  Configuring website..." -ForegroundColor White

if (-not (Test-Path "IIS:\Sites\$SiteName")) {
    New-Website -Name $SiteName `
        -PhysicalPath $PhysicalPath `
        -ApplicationPool $AppPoolName `
        -Port $Port `
        | Out-Null
    Write-Host "  ✓ Created website" -ForegroundColor Green
} else {
    Set-ItemProperty -Path "IIS:\Sites\$SiteName" -Name "physicalPath" -Value $PhysicalPath
    Set-ItemProperty -Path "IIS:\Sites\$SiteName" -Name "applicationPool" -Value $AppPoolName
}

# Add HTTPS binding if certificate provided
if ($CertificateThumbprint) {
    Write-Host "  Configuring HTTPS binding..." -ForegroundColor White

    $existingBinding = Get-WebBinding -Name $SiteName -Protocol "https" -Port $HttpsPort -ErrorAction SilentlyContinue

    if (-not $existingBinding) {
        New-WebBinding -Name $SiteName -Protocol "https" -Port $HttpsPort -SslFlags 0

        $cert = Get-ChildItem -Path "Cert:\LocalMachine\My" |
            Where-Object { $_.Thumbprint -eq $CertificateThumbprint }

        if ($cert) {
            $binding = Get-WebBinding -Name $SiteName -Protocol "https" -Port $HttpsPort
            $binding.AddSslCertificate($CertificateThumbprint, "My")
            Write-Host "  ✓ HTTPS binding configured" -ForegroundColor Green
        } else {
            Write-Warning "Certificate not found: $CertificateThumbprint"
        }
    }
}

# Start site and pool
Write-Host "  Starting services..." -ForegroundColor White

Start-WebAppPool -Name $AppPoolName
Start-Website -Name $SiteName

Write-Host "  ✓ Services started" -ForegroundColor Green

# Verify
Start-Sleep -Seconds 2
$siteState = (Get-Website -Name $SiteName).State
$poolState = (Get-WebAppPoolState -Name $AppPoolName).Value

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  Deployment Complete!" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "  Website State:  $siteState" -ForegroundColor White
Write-Host "  App Pool State: $poolState" -ForegroundColor White
Write-Host ""
Write-Host "  URLs:" -ForegroundColor Cyan
Write-Host "    http://localhost:$Port" -ForegroundColor White
if ($CertificateThumbprint) {
    Write-Host "    https://localhost:$HttpsPort" -ForegroundColor White
}
Write-Host ""
