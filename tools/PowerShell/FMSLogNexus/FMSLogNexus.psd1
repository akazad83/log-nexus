@{
    # Module metadata
    RootModule = 'FMSLogNexus.psm1'
    ModuleVersion = '1.0.0'
    GUID = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'
    Author = 'FMS Log Nexus Team'
    CompanyName = 'Your Organization'
    Copyright = '(c) 2024 Your Organization. All rights reserved.'
    Description = 'PowerShell module for administering FMS Log Nexus centralized logging system.'
    PowerShellVersion = '5.1'
    
    # Functions to export
    FunctionsToExport = @(
        # Connection
        'Connect-FMSLogNexus',
        'Disconnect-FMSLogNexus',
        'Get-FMSConnection',
        
        # Logs
        'Get-FMSLog',
        'Get-FMSLogStatistics',
        'Send-FMSLog',
        
        # Servers
        'Get-FMSServer',
        'Set-FMSServerStatus',
        'Send-FMSHeartbeat',
        
        # Jobs
        'Get-FMSJob',
        'Register-FMSJob',
        'Enable-FMSJob',
        'Disable-FMSJob',
        
        # Executions
        'Get-FMSExecution',
        'Start-FMSExecution',
        'Stop-FMSExecution',
        'Complete-FMSExecution',
        
        # Alerts
        'Get-FMSAlert',
        'Get-FMSAlertInstance',
        'Acknowledge-FMSAlertInstance',
        'Resolve-FMSAlertInstance',
        
        # Dashboard
        'Get-FMSDashboard',
        
        # Users
        'Get-FMSUser',
        'New-FMSUser'
    )
    
    # Cmdlets to export
    CmdletsToExport = @()
    
    # Variables to export
    VariablesToExport = @()
    
    # Aliases to export
    AliasesToExport = @(
        'cfms',    # Connect-FMSLogNexus
        'dfms',    # Disconnect-FMSLogNexus
        'gfl',     # Get-FMSLog
        'gfs',     # Get-FMSServer
        'gfj',     # Get-FMSJob
        'gfe',     # Get-FMSExecution
        'gfa'      # Get-FMSAlert
    )
    
    # Private data
    PrivateData = @{
        PSData = @{
            Tags = @('FMSLogNexus', 'Logging', 'Monitoring', 'Administration')
            LicenseUri = 'https://github.com/your-org/fms-log-nexus/blob/main/LICENSE'
            ProjectUri = 'https://github.com/your-org/fms-log-nexus'
            ReleaseNotes = 'Initial release of FMS Log Nexus PowerShell module.'
        }
    }
}
