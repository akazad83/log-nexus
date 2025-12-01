#Requires -Version 5.1

# Module-level variables
$script:FMSConnection = $null

#region Helper Functions

function Invoke-FMSRequest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Endpoint,
        
        [ValidateSet('GET', 'POST', 'PUT', 'DELETE')]
        [string]$Method = 'GET',
        
        [object]$Body,
        
        [hashtable]$QueryParameters
    )
    
    if (-not $script:FMSConnection) {
        throw "Not connected to FMS Log Nexus. Use Connect-FMSLogNexus first."
    }
    
    $uri = "$($script:FMSConnection.BaseUrl)/api/$Endpoint"
    
    if ($QueryParameters -and $QueryParameters.Count -gt 0) {
        $queryString = ($QueryParameters.GetEnumerator() | 
            Where-Object { $null -ne $_.Value } |
            ForEach-Object { "$($_.Key)=$([System.Web.HttpUtility]::UrlEncode($_.Value.ToString()))" }) -join '&'
        $uri = "$uri`?$queryString"
    }
    
    $headers = @{
        'Accept' = 'application/json'
        'Content-Type' = 'application/json'
    }
    
    if ($script:FMSConnection.AccessToken) {
        $headers['Authorization'] = "Bearer $($script:FMSConnection.AccessToken)"
    }
    elseif ($script:FMSConnection.ApiKey) {
        $headers['X-Api-Key'] = $script:FMSConnection.ApiKey
    }
    
    $params = @{
        Uri = $uri
        Method = $Method
        Headers = $headers
        UseBasicParsing = $true
    }
    
    if ($Body) {
        $params['Body'] = $Body | ConvertTo-Json -Depth 10
    }
    
    try {
        $response = Invoke-RestMethod @params
        return $response
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $errorMessage = $_.ErrorDetails.Message
        
        if ($errorMessage) {
            try {
                $errorObj = $errorMessage | ConvertFrom-Json
                throw "FMS Log Nexus API Error ($statusCode): $($errorObj.title) - $($errorObj.detail)"
            }
            catch {
                throw "FMS Log Nexus API Error ($statusCode): $errorMessage"
            }
        }
        throw "FMS Log Nexus API Error ($statusCode): $($_.Exception.Message)"
    }
}

#endregion

#region Connection Functions

function Connect-FMSLogNexus {
    <#
    .SYNOPSIS
        Connects to an FMS Log Nexus server.
    .DESCRIPTION
        Establishes a connection to an FMS Log Nexus server using either 
        username/password authentication or an API key.
    .PARAMETER BaseUrl
        The base URL of the FMS Log Nexus API (e.g., https://fms-lognexus.example.com).
    .PARAMETER Credential
        PSCredential object containing username and password.
    .PARAMETER ApiKey
        API key for authentication.
    .EXAMPLE
        Connect-FMSLogNexus -BaseUrl "https://fms.example.com" -Credential (Get-Credential)
    .EXAMPLE
        Connect-FMSLogNexus -BaseUrl "https://fms.example.com" -ApiKey "your-api-key"
    #>
    [CmdletBinding(DefaultParameterSetName = 'Credential')]
    param(
        [Parameter(Mandatory)]
        [string]$BaseUrl,
        
        [Parameter(ParameterSetName = 'Credential')]
        [System.Management.Automation.PSCredential]$Credential,
        
        [Parameter(ParameterSetName = 'ApiKey', Mandatory)]
        [string]$ApiKey
    )
    
    $BaseUrl = $BaseUrl.TrimEnd('/')
    
    if ($PSCmdlet.ParameterSetName -eq 'Credential') {
        if (-not $Credential) {
            $Credential = Get-Credential -Message "Enter FMS Log Nexus credentials"
        }
        
        $body = @{
            username = $Credential.UserName
            password = $Credential.GetNetworkCredential().Password
        } | ConvertTo-Json
        
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" `
                -Method POST `
                -Body $body `
                -ContentType 'application/json' `
                -UseBasicParsing
            
            $script:FMSConnection = [PSCustomObject]@{
                BaseUrl = $BaseUrl
                AccessToken = $response.accessToken
                RefreshToken = $response.refreshToken
                ExpiresAt = [DateTime]::Parse($response.expiresAt)
                ApiKey = $null
                User = $response.user
            }
            
            Write-Host "Connected to FMS Log Nexus at $BaseUrl" -ForegroundColor Green
            return $script:FMSConnection
        }
        catch {
            throw "Failed to authenticate: $($_.Exception.Message)"
        }
    }
    else {
        # Validate API key with a simple request
        $headers = @{ 'X-Api-Key' = $ApiKey }
        
        try {
            $null = Invoke-RestMethod -Uri "$BaseUrl/health" `
                -Method GET `
                -Headers $headers `
                -UseBasicParsing
            
            $script:FMSConnection = [PSCustomObject]@{
                BaseUrl = $BaseUrl
                AccessToken = $null
                RefreshToken = $null
                ExpiresAt = $null
                ApiKey = $ApiKey
                User = $null
            }
            
            Write-Host "Connected to FMS Log Nexus at $BaseUrl (API Key)" -ForegroundColor Green
            return $script:FMSConnection
        }
        catch {
            throw "Failed to connect with API key: $($_.Exception.Message)"
        }
    }
}

function Disconnect-FMSLogNexus {
    <#
    .SYNOPSIS
        Disconnects from the FMS Log Nexus server.
    #>
    [CmdletBinding()]
    param()
    
    if ($script:FMSConnection -and $script:FMSConnection.AccessToken) {
        try {
            Invoke-FMSRequest -Endpoint 'auth/logout' -Method POST
        }
        catch {
            # Ignore logout errors
        }
    }
    
    $script:FMSConnection = $null
    Write-Host "Disconnected from FMS Log Nexus" -ForegroundColor Yellow
}

function Get-FMSConnection {
    <#
    .SYNOPSIS
        Gets the current FMS Log Nexus connection information.
    #>
    [CmdletBinding()]
    param()
    
    if (-not $script:FMSConnection) {
        Write-Warning "Not connected to FMS Log Nexus"
        return $null
    }
    
    return $script:FMSConnection
}

#endregion

#region Log Functions

function Get-FMSLog {
    <#
    .SYNOPSIS
        Gets log entries from FMS Log Nexus.
    .PARAMETER Id
        Specific log entry ID to retrieve.
    .PARAMETER ServerName
        Filter by server name.
    .PARAMETER JobId
        Filter by job ID.
    .PARAMETER Level
        Filter by log level.
    .PARAMETER StartDate
        Filter logs from this date.
    .PARAMETER EndDate
        Filter logs until this date.
    .PARAMETER PageSize
        Number of results per page (default 50).
    .PARAMETER Page
        Page number (default 1).
    #>
    [CmdletBinding(DefaultParameterSetName = 'Search')]
    param(
        [Parameter(ParameterSetName = 'ById', Mandatory)]
        [guid]$Id,
        
        [Parameter(ParameterSetName = 'Search')]
        [string]$ServerName,
        
        [Parameter(ParameterSetName = 'Search')]
        [string]$JobId,
        
        [Parameter(ParameterSetName = 'Search')]
        [ValidateSet('Trace', 'Debug', 'Information', 'Warning', 'Error', 'Critical')]
        [string]$Level,
        
        [Parameter(ParameterSetName = 'Search')]
        [datetime]$StartDate,
        
        [Parameter(ParameterSetName = 'Search')]
        [datetime]$EndDate,
        
        [Parameter(ParameterSetName = 'Search')]
        [int]$PageSize = 50,
        
        [Parameter(ParameterSetName = 'Search')]
        [int]$Page = 1
    )
    
    if ($Id) {
        return Invoke-FMSRequest -Endpoint "logs/$Id"
    }
    
    $query = @{
        serverName = $ServerName
        jobId = $JobId
        level = $Level
        startDate = if ($StartDate) { $StartDate.ToString('o') } else { $null }
        endDate = if ($EndDate) { $EndDate.ToString('o') } else { $null }
        pageSize = $PageSize
        page = $Page
    }
    
    return Invoke-FMSRequest -Endpoint 'logs/search' -QueryParameters $query
}

function Get-FMSLogStatistics {
    <#
    .SYNOPSIS
        Gets log statistics from FMS Log Nexus.
    #>
    [CmdletBinding()]
    param(
        [string]$ServerName,
        [datetime]$StartDate,
        [datetime]$EndDate
    )
    
    $query = @{
        serverName = $ServerName
        startDate = if ($StartDate) { $StartDate.ToString('o') } else { $null }
        endDate = if ($EndDate) { $EndDate.ToString('o') } else { $null }
    }
    
    return Invoke-FMSRequest -Endpoint 'logs/statistics' -QueryParameters $query
}

function Send-FMSLog {
    <#
    .SYNOPSIS
        Sends a log entry to FMS Log Nexus.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Message,
        
        [ValidateSet('Trace', 'Debug', 'Information', 'Warning', 'Error', 'Critical')]
        [string]$Level = 'Information',
        
        [string]$ServerName = $env:COMPUTERNAME,
        
        [string]$JobId,
        
        [guid]$ExecutionId,
        
        [hashtable]$Properties
    )
    
    $body = @{
        timestamp = (Get-Date).ToUniversalTime().ToString('o')
        level = $Level
        message = $Message
        serverName = $ServerName
        jobId = $JobId
        executionId = if ($ExecutionId -ne [Guid]::Empty) { $ExecutionId.ToString() } else { $null }
        properties = $Properties
    }
    
    return Invoke-FMSRequest -Endpoint 'logs' -Method POST -Body $body
}

#endregion

#region Server Functions

function Get-FMSServer {
    <#
    .SYNOPSIS
        Gets server information from FMS Log Nexus.
    #>
    [CmdletBinding()]
    param(
        [string]$ServerName,
        
        [switch]$OnlineOnly
    )
    
    if ($ServerName) {
        return Invoke-FMSRequest -Endpoint "servers/$ServerName"
    }
    
    $servers = Invoke-FMSRequest -Endpoint 'servers'
    
    if ($OnlineOnly) {
        $servers = $servers | Where-Object { $_.status -eq 'Online' }
    }
    
    return $servers
}

function Set-FMSServerStatus {
    <#
    .SYNOPSIS
        Sets the status of a server.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ServerName,
        
        [Parameter(Mandatory)]
        [ValidateSet('Online', 'Offline', 'Maintenance')]
        [string]$Status
    )
    
    $endpoint = switch ($Status) {
        'Maintenance' { "servers/$ServerName/maintenance" }
        'Online' { "servers/$ServerName/activate" }
        'Offline' { "servers/$ServerName/deactivate" }
    }
    
    return Invoke-FMSRequest -Endpoint $endpoint -Method POST
}

function Send-FMSHeartbeat {
    <#
    .SYNOPSIS
        Sends a heartbeat for a server.
    #>
    [CmdletBinding()]
    param(
        [string]$ServerName = $env:COMPUTERNAME,
        
        [ValidateSet('Online', 'Offline', 'Maintenance')]
        [string]$Status = 'Online',
        
        [string]$AgentVersion = '1.0.0'
    )
    
    $body = @{
        serverName = $ServerName
        status = $Status
        agentVersion = $AgentVersion
        systemInfo = @{
            os = [System.Environment]::OSVersion.ToString()
            machineName = [System.Environment]::MachineName
            processorCount = [System.Environment]::ProcessorCount
        }
    }
    
    return Invoke-FMSRequest -Endpoint 'servers/heartbeat' -Method POST -Body $body
}

#endregion

#region Job Functions

function Get-FMSJob {
    <#
    .SYNOPSIS
        Gets job information from FMS Log Nexus.
    #>
    [CmdletBinding()]
    param(
        [string]$JobId,
        [string]$ServerName,
        [switch]$ActiveOnly
    )
    
    if ($JobId) {
        return Invoke-FMSRequest -Endpoint "jobs/$JobId"
    }
    
    if ($ServerName) {
        return Invoke-FMSRequest -Endpoint "jobs/server/$ServerName"
    }
    
    $jobs = Invoke-FMSRequest -Endpoint 'jobs'
    
    if ($ActiveOnly) {
        $jobs = $jobs | Where-Object { $_.isActive }
    }
    
    return $jobs
}

function Register-FMSJob {
    <#
    .SYNOPSIS
        Registers a new job with FMS Log Nexus.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$JobId,
        
        [Parameter(Mandatory)]
        [string]$DisplayName,
        
        [string]$Description,
        
        [string]$ServerName = $env:COMPUTERNAME,
        
        [string]$Schedule,
        
        [ValidateSet('Low', 'Normal', 'High', 'Critical')]
        [string]$Priority = 'Normal',
        
        [int]$TimeoutMinutes,
        
        [string[]]$Tags
    )
    
    $body = @{
        jobId = $JobId
        displayName = $DisplayName
        description = $Description
        serverName = $ServerName
        schedule = $Schedule
        priority = $Priority
        timeoutMinutes = $TimeoutMinutes
        tags = $Tags
    }
    
    return Invoke-FMSRequest -Endpoint 'jobs/register' -Method POST -Body $body
}

function Enable-FMSJob {
    <#
    .SYNOPSIS
        Activates a job.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$JobId
    )
    
    return Invoke-FMSRequest -Endpoint "jobs/$JobId/activate" -Method POST
}

function Disable-FMSJob {
    <#
    .SYNOPSIS
        Deactivates a job.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$JobId
    )
    
    return Invoke-FMSRequest -Endpoint "jobs/$JobId/deactivate" -Method POST
}

#endregion

#region Execution Functions

function Get-FMSExecution {
    <#
    .SYNOPSIS
        Gets execution information from FMS Log Nexus.
    #>
    [CmdletBinding()]
    param(
        [guid]$Id,
        [string]$JobId,
        [switch]$RunningOnly
    )
    
    if ($Id -and $Id -ne [Guid]::Empty) {
        return Invoke-FMSRequest -Endpoint "executions/$Id"
    }
    
    if ($RunningOnly) {
        return Invoke-FMSRequest -Endpoint 'executions/running'
    }
    
    if ($JobId) {
        return Invoke-FMSRequest -Endpoint "executions/job/$JobId"
    }
    
    return Invoke-FMSRequest -Endpoint 'executions/running'
}

function Start-FMSExecution {
    <#
    .SYNOPSIS
        Starts a new job execution.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$JobId,
        
        [string]$ServerName = $env:COMPUTERNAME,
        
        [ValidateSet('Manual', 'Scheduled', 'Triggered', 'Retry')]
        [string]$TriggerType = 'Manual',
        
        [string]$TriggeredBy,
        
        [hashtable]$Parameters
    )
    
    $body = @{
        jobId = $JobId
        serverName = $ServerName
        triggerType = $TriggerType
        triggeredBy = if ($TriggeredBy) { $TriggeredBy } else { $env:USERNAME }
        parameters = $Parameters
    }
    
    return Invoke-FMSRequest -Endpoint 'executions' -Method POST -Body $body
}

function Stop-FMSExecution {
    <#
    .SYNOPSIS
        Cancels a running execution.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [guid]$Id,
        
        [string]$Reason
    )
    
    $body = @{ reason = $Reason }
    return Invoke-FMSRequest -Endpoint "executions/$Id/cancel" -Method POST -Body $body
}

function Complete-FMSExecution {
    <#
    .SYNOPSIS
        Completes an execution with a status.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [guid]$Id,
        
        [Parameter(Mandatory)]
        [ValidateSet('Success', 'Failed')]
        [string]$Status,
        
        [string]$OutputMessage,
        
        [string]$ErrorMessage
    )
    
    $body = @{
        status = $Status
        outputMessage = $OutputMessage
        errorMessage = $ErrorMessage
    }
    
    return Invoke-FMSRequest -Endpoint "executions/$Id/complete" -Method PUT -Body $body
}

#endregion

#region Alert Functions

function Get-FMSAlert {
    <#
    .SYNOPSIS
        Gets alert rules from FMS Log Nexus.
    #>
    [CmdletBinding()]
    param(
        [guid]$Id,
        [switch]$ActiveOnly
    )
    
    if ($Id -and $Id -ne [Guid]::Empty) {
        return Invoke-FMSRequest -Endpoint "alerts/$Id"
    }
    
    $alerts = Invoke-FMSRequest -Endpoint 'alerts'
    
    if ($ActiveOnly) {
        $alerts = $alerts | Where-Object { $_.isActive }
    }
    
    return $alerts
}

function Get-FMSAlertInstance {
    <#
    .SYNOPSIS
        Gets alert instances from FMS Log Nexus.
    #>
    [CmdletBinding()]
    param(
        [switch]$ActiveOnly
    )
    
    if ($ActiveOnly) {
        return Invoke-FMSRequest -Endpoint 'alerts/instances/active'
    }
    
    return Invoke-FMSRequest -Endpoint 'alerts/instances'
}

function Acknowledge-FMSAlertInstance {
    <#
    .SYNOPSIS
        Acknowledges an alert instance.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [guid]$Id,
        
        [string]$Notes
    )
    
    $body = @{ notes = $Notes }
    return Invoke-FMSRequest -Endpoint "alerts/instances/$Id/acknowledge" -Method POST -Body $body
}

function Resolve-FMSAlertInstance {
    <#
    .SYNOPSIS
        Resolves an alert instance.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [guid]$Id,
        
        [string]$Notes
    )
    
    $body = @{ notes = $Notes }
    return Invoke-FMSRequest -Endpoint "alerts/instances/$Id/resolve" -Method POST -Body $body
}

#endregion

#region Dashboard Functions

function Get-FMSDashboard {
    <#
    .SYNOPSIS
        Gets dashboard data from FMS Log Nexus.
    #>
    [CmdletBinding()]
    param()
    
    return Invoke-FMSRequest -Endpoint 'dashboard'
}

#endregion

#region User Functions

function Get-FMSUser {
    <#
    .SYNOPSIS
        Gets user information from FMS Log Nexus.
    #>
    [CmdletBinding()]
    param()
    
    return Invoke-FMSRequest -Endpoint 'users'
}

function New-FMSUser {
    <#
    .SYNOPSIS
        Creates a new user in FMS Log Nexus.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Username,
        
        [Parameter(Mandatory)]
        [string]$Email,
        
        [Parameter(Mandatory)]
        [securestring]$Password,
        
        [ValidateSet('Admin', 'Operator', 'Viewer', 'Agent')]
        [string]$Role = 'Viewer',
        
        [string]$DisplayName
    )
    
    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)
    $plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
    
    $body = @{
        username = $Username
        email = $Email
        password = $plainPassword
        role = $Role
        displayName = $DisplayName
    }
    
    return Invoke-FMSRequest -Endpoint 'users' -Method POST -Body $body
}

#endregion

#region Aliases
Set-Alias -Name cfms -Value Connect-FMSLogNexus
Set-Alias -Name dfms -Value Disconnect-FMSLogNexus
Set-Alias -Name gfl -Value Get-FMSLog
Set-Alias -Name gfs -Value Get-FMSServer
Set-Alias -Name gfj -Value Get-FMSJob
Set-Alias -Name gfe -Value Get-FMSExecution
Set-Alias -Name gfa -Value Get-FMSAlert
#endregion

# Export module members
Export-ModuleMember -Function * -Alias *
