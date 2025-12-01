# FMS Log Nexus PowerShell Module

PowerShell module for administering FMS Log Nexus centralized logging system.

## Installation

### From PowerShell Gallery (when published)

```powershell
Install-Module -Name FMSLogNexus
```

### Manual Installation

1. Copy the `FMSLogNexus` folder to one of your PowerShell module paths:
   - User: `$HOME\Documents\PowerShell\Modules\`
   - System: `$env:ProgramFiles\PowerShell\Modules\`

2. Import the module:
   ```powershell
   Import-Module FMSLogNexus
   ```

## Quick Start

```powershell
# Connect using credentials
Connect-FMSLogNexus -BaseUrl "https://fms-lognexus.example.com" -Credential (Get-Credential)

# Or connect using API key
Connect-FMSLogNexus -BaseUrl "https://fms-lognexus.example.com" -ApiKey "your-api-key"

# Get dashboard overview
Get-FMSDashboard

# Get recent logs
Get-FMSLog -PageSize 10

# Get all servers
Get-FMSServer

# Disconnect
Disconnect-FMSLogNexus
```

## Commands

### Connection

| Command | Alias | Description |
|---------|-------|-------------|
| `Connect-FMSLogNexus` | `cfms` | Connect to FMS Log Nexus server |
| `Disconnect-FMSLogNexus` | `dfms` | Disconnect from server |
| `Get-FMSConnection` | | Get current connection info |

### Logs

| Command | Alias | Description |
|---------|-------|-------------|
| `Get-FMSLog` | `gfl` | Get log entries |
| `Get-FMSLogStatistics` | | Get log statistics |
| `Send-FMSLog` | | Send a log entry |

### Servers

| Command | Alias | Description |
|---------|-------|-------------|
| `Get-FMSServer` | `gfs` | Get server information |
| `Set-FMSServerStatus` | | Set server status |
| `Send-FMSHeartbeat` | | Send server heartbeat |

### Jobs

| Command | Alias | Description |
|---------|-------|-------------|
| `Get-FMSJob` | `gfj` | Get job information |
| `Register-FMSJob` | | Register a new job |
| `Enable-FMSJob` | | Activate a job |
| `Disable-FMSJob` | | Deactivate a job |

### Executions

| Command | Alias | Description |
|---------|-------|-------------|
| `Get-FMSExecution` | `gfe` | Get execution information |
| `Start-FMSExecution` | | Start a job execution |
| `Stop-FMSExecution` | | Cancel an execution |
| `Complete-FMSExecution` | | Complete an execution |

### Alerts

| Command | Alias | Description |
|---------|-------|-------------|
| `Get-FMSAlert` | `gfa` | Get alert rules |
| `Get-FMSAlertInstance` | | Get alert instances |
| `Acknowledge-FMSAlertInstance` | | Acknowledge an alert |
| `Resolve-FMSAlertInstance` | | Resolve an alert |

### Users

| Command | Description |
|---------|-------------|
| `Get-FMSUser` | Get users |
| `New-FMSUser` | Create a new user |

## Examples

### Working with Logs

```powershell
# Get logs for a specific server
Get-FMSLog -ServerName "SERVER01" -Level Error

# Get logs for a date range
Get-FMSLog -StartDate (Get-Date).AddDays(-1) -EndDate (Get-Date)

# Send a log entry
Send-FMSLog -Message "Backup completed" -Level Information -JobId "BACKUP-001"

# Get log statistics
Get-FMSLogStatistics -ServerName "SERVER01"
```

### Working with Jobs

```powershell
# Register a new job
Register-FMSJob -JobId "CLEANUP-001" -DisplayName "Cleanup Job" -Priority High

# Start an execution
$execution = Start-FMSExecution -JobId "CLEANUP-001"

# Complete the execution
Complete-FMSExecution -Id $execution.id -Status Success -OutputMessage "Cleaned 100 files"

# Get running executions
Get-FMSExecution -RunningOnly
```

### Working with Alerts

```powershell
# Get active alerts
Get-FMSAlertInstance -ActiveOnly

# Acknowledge an alert
Acknowledge-FMSAlertInstance -Id "guid-here" -Notes "Investigating..."

# Resolve an alert
Resolve-FMSAlertInstance -Id "guid-here" -Notes "Issue resolved"
```

### Scripting Example

```powershell
# Automated job execution with logging
$jobId = "NIGHTLY-BACKUP"

try {
    # Start execution
    $execution = Start-FMSExecution -JobId $jobId -TriggerType Scheduled
    Send-FMSLog -Message "Backup started" -Level Information -JobId $jobId -ExecutionId $execution.id
    
    # Do work...
    Start-Sleep -Seconds 5
    
    # Complete successfully
    Complete-FMSExecution -Id $execution.id -Status Success -OutputMessage "Backup completed"
    Send-FMSLog -Message "Backup completed successfully" -Level Information -JobId $jobId -ExecutionId $execution.id
}
catch {
    # Report failure
    Complete-FMSExecution -Id $execution.id -Status Failed -ErrorMessage $_.Exception.Message
    Send-FMSLog -Message "Backup failed: $($_.Exception.Message)" -Level Error -JobId $jobId -ExecutionId $execution.id
}
```

## Requirements

- PowerShell 5.1 or later
- Network access to FMS Log Nexus API

## License

MIT License
