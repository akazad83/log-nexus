-- ============================================================================
-- FMS Log Nexus - Complete Database Schema
-- Version: 1.0.0
-- Date: 2024-01-15
-- 
-- This script creates the complete database schema for FMS Log Nexus
-- Run this script on a SQL Server 2019+ instance
-- ============================================================================

SET NOCOUNT ON;
GO

PRINT '============================================================';
PRINT 'FMS Log Nexus - Database Schema Creation';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================================';
GO

-- ============================================================================
-- SECTION 1: Database Creation
-- ============================================================================
USE [master]
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'FMSLogNexus')
BEGIN
    PRINT 'Creating database [FMSLogNexus]...';
    
    -- Adjust file paths as needed for your environment
    CREATE DATABASE [FMSLogNexus]
    ON PRIMARY 
    (
        NAME = N'FMSLogNexus_Data',
        FILENAME = N'C:\SQLData\FMSLogNexus_Data.mdf',
        SIZE = 512MB,
        MAXSIZE = UNLIMITED,
        FILEGROWTH = 256MB
    ),
    FILEGROUP [LogEntries_FG]
    (
        NAME = N'FMSLogNexus_LogEntries',
        FILENAME = N'C:\SQLData\FMSLogNexus_LogEntries.ndf',
        SIZE = 1024MB,
        MAXSIZE = UNLIMITED,
        FILEGROWTH = 512MB
    )
    LOG ON 
    (
        NAME = N'FMSLogNexus_Log',
        FILENAME = N'C:\SQLLogs\FMSLogNexus_Log.ldf',
        SIZE = 256MB,
        MAXSIZE = UNLIMITED,
        FILEGROWTH = 128MB
    )
    COLLATE SQL_Latin1_General_CP1_CI_AS;
    
    PRINT 'Database [FMSLogNexus] created successfully.';
END
ELSE
BEGIN
    PRINT 'Database [FMSLogNexus] already exists.';
END
GO

USE [FMSLogNexus]
GO

-- Enable snapshot isolation
ALTER DATABASE [FMSLogNexus] SET ALLOW_SNAPSHOT_ISOLATION ON;
ALTER DATABASE [FMSLogNexus] SET READ_COMMITTED_SNAPSHOT ON;
GO

-- ============================================================================
-- SECTION 2: Schema Creation
-- ============================================================================
PRINT 'Creating schemas...';

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'log')
    EXEC('CREATE SCHEMA [log]');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'job')
    EXEC('CREATE SCHEMA [job]');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'alert')
    EXEC('CREATE SCHEMA [alert]');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'auth')
    EXEC('CREATE SCHEMA [auth]');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'system')
    EXEC('CREATE SCHEMA [system]');
GO

PRINT 'Schemas created.';
GO

-- ============================================================================
-- SECTION 3: Enumeration/Lookup Tables
-- ============================================================================
PRINT 'Creating lookup tables...';

-- Log Levels
IF OBJECT_ID('[system].[LogLevels]', 'U') IS NULL
BEGIN
    CREATE TABLE [system].[LogLevels]
    (
        [Id]          INT           NOT NULL,
        [Name]        NVARCHAR(20)  NOT NULL,
        [DisplayName] NVARCHAR(50)  NOT NULL,
        [Description] NVARCHAR(200) NULL,
        [SortOrder]   INT           NOT NULL,
        CONSTRAINT [PK_LogLevels] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_LogLevels_Name] UNIQUE ([Name])
    );

    INSERT INTO [system].[LogLevels] ([Id], [Name], [DisplayName], [Description], [SortOrder])
    VALUES
        (0, 'Trace',       'Trace',       'Extremely detailed diagnostic information', 0),
        (1, 'Debug',       'Debug',       'Diagnostic information for debugging', 1),
        (2, 'Information', 'Information', 'Normal operational messages', 2),
        (3, 'Warning',     'Warning',     'Potential issues that require attention', 3),
        (4, 'Error',       'Error',       'Errors that prevent specific operations', 4),
        (5, 'Critical',    'Critical',    'Critical failures requiring immediate attention', 5);
END
GO

-- Job Types
IF OBJECT_ID('[system].[JobTypes]', 'U') IS NULL
BEGIN
    CREATE TABLE [system].[JobTypes]
    (
        [Id]          INT           NOT NULL,
        [Name]        NVARCHAR(30)  NOT NULL,
        [DisplayName] NVARCHAR(50)  NOT NULL,
        [Description] NVARCHAR(200) NULL,
        CONSTRAINT [PK_JobTypes] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_JobTypes_Name] UNIQUE ([Name])
    );

    INSERT INTO [system].[JobTypes] ([Id], [Name], [DisplayName], [Description])
    VALUES
        (0, 'Unknown',       'Unknown',          'Job type not specified'),
        (1, 'Executable',    'Executable',       'Standalone executable (.exe)'),
        (2, 'PowerShell',    'PowerShell',       'PowerShell script (.ps1)'),
        (3, 'VBScript',      'VBScript',         'VBScript (.vbs)'),
        (4, 'DotNetAssembly','.NET Assembly',    '.NET console application'),
        (5, 'SqlJob',        'SQL Server Job',   'SQL Server Agent job'),
        (6, 'WindowsService','Windows Service',  'Windows service application'),
        (7, 'Other',         'Other',            'Other job type');
END
GO

-- Job Statuses
IF OBJECT_ID('[system].[JobStatuses]', 'U') IS NULL
BEGIN
    CREATE TABLE [system].[JobStatuses]
    (
        [Id]          INT           NOT NULL,
        [Name]        NVARCHAR(20)  NOT NULL,
        [DisplayName] NVARCHAR(50)  NOT NULL,
        [Description] NVARCHAR(200) NULL,
        [IsTerminal]  BIT           NOT NULL DEFAULT 0,
        CONSTRAINT [PK_JobStatuses] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_JobStatuses_Name] UNIQUE ([Name])
    );

    INSERT INTO [system].[JobStatuses] ([Id], [Name], [DisplayName], [Description], [IsTerminal])
    VALUES
        (0, 'Pending',   'Pending',   'Job is queued and waiting to start', 0),
        (1, 'Running',   'Running',   'Job is currently executing', 0),
        (2, 'Completed', 'Completed', 'Job finished successfully', 1),
        (3, 'Failed',    'Failed',    'Job finished with errors', 1),
        (4, 'Cancelled', 'Cancelled', 'Job was cancelled before completion', 1),
        (5, 'Timeout',   'Timeout',   'Job exceeded maximum execution time', 1),
        (6, 'Warning',   'Warning',   'Job completed with warnings', 1);
END
GO

-- Server Statuses
IF OBJECT_ID('[system].[ServerStatuses]', 'U') IS NULL
BEGIN
    CREATE TABLE [system].[ServerStatuses]
    (
        [Id]          INT           NOT NULL,
        [Name]        NVARCHAR(20)  NOT NULL,
        [DisplayName] NVARCHAR(50)  NOT NULL,
        [Description] NVARCHAR(200) NULL,
        CONSTRAINT [PK_ServerStatuses] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ServerStatuses_Name] UNIQUE ([Name])
    );

    INSERT INTO [system].[ServerStatuses] ([Id], [Name], [DisplayName], [Description])
    VALUES
        (0, 'Unknown',  'Unknown',  'Server status cannot be determined'),
        (1, 'Online',   'Online',   'Server is online and responsive'),
        (2, 'Offline',  'Offline',  'Server is not responding'),
        (3, 'Degraded', 'Degraded', 'Server is online but experiencing issues');
END
GO

-- Alert Severities
IF OBJECT_ID('[system].[AlertSeverities]', 'U') IS NULL
BEGIN
    CREATE TABLE [system].[AlertSeverities]
    (
        [Id]          INT           NOT NULL,
        [Name]        NVARCHAR(20)  NOT NULL,
        [DisplayName] NVARCHAR(50)  NOT NULL,
        [Description] NVARCHAR(200) NULL,
        [Color]       NVARCHAR(7)   NULL,
        CONSTRAINT [PK_AlertSeverities] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_AlertSeverities_Name] UNIQUE ([Name])
    );

    INSERT INTO [system].[AlertSeverities] ([Id], [Name], [DisplayName], [Description], [Color])
    VALUES
        (0, 'Low',      'Low',      'Low priority alert', '#4CAF50'),
        (1, 'Medium',   'Medium',   'Medium priority alert', '#FF9800'),
        (2, 'High',     'High',     'High priority alert', '#F44336'),
        (3, 'Critical', 'Critical', 'Critical alert requiring immediate action', '#9C27B0');
END
GO

-- Alert Types
IF OBJECT_ID('[system].[AlertTypes]', 'U') IS NULL
BEGIN
    CREATE TABLE [system].[AlertTypes]
    (
        [Id]          INT           NOT NULL,
        [Name]        NVARCHAR(30)  NOT NULL,
        [DisplayName] NVARCHAR(50)  NOT NULL,
        [Description] NVARCHAR(200) NULL,
        CONSTRAINT [PK_AlertTypes] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_AlertTypes_Name] UNIQUE ([Name])
    );

    INSERT INTO [system].[AlertTypes] ([Id], [Name], [DisplayName], [Description])
    VALUES
        (0, 'ErrorThreshold',    'Error Threshold',     'Triggered when error count exceeds threshold'),
        (1, 'JobFailure',        'Job Failure',         'Triggered when a job fails'),
        (2, 'ServerOffline',     'Server Offline',      'Triggered when a server goes offline'),
        (3, 'PerformanceWarning','Performance Warning', 'Triggered when performance degrades'),
        (4, 'CustomQuery',       'Custom Query',        'Triggered by custom SQL query'),
        (5, 'PatternMatch',      'Pattern Match',       'Triggered when log message matches pattern');
END
GO

-- User Roles
IF OBJECT_ID('[system].[UserRoles]', 'U') IS NULL
BEGIN
    CREATE TABLE [system].[UserRoles]
    (
        [Id]          INT           NOT NULL,
        [Name]        NVARCHAR(30)  NOT NULL,
        [DisplayName] NVARCHAR(50)  NOT NULL,
        [Description] NVARCHAR(200) NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_UserRoles_Name] UNIQUE ([Name])
    );

    INSERT INTO [system].[UserRoles] ([Id], [Name], [DisplayName], [Description])
    VALUES
        (0, 'Viewer',        'Viewer',        'Can view dashboard and search logs (read-only)'),
        (1, 'Operator',      'Operator',      'Can acknowledge alerts and manage jobs'),
        (2, 'Administrator', 'Administrator', 'Full access to all features and settings'),
        (3, 'Service',       'Service',       'Service account for API access only');
END
GO

PRINT 'Lookup tables created.';
GO

-- ============================================================================
-- SECTION 4: Core Tables
-- ============================================================================
PRINT 'Creating core tables...';

-- Servers Table
IF OBJECT_ID('[job].[Servers]', 'U') IS NULL
BEGIN
    CREATE TABLE [job].[Servers]
    (
        [ServerName]      NVARCHAR(100)   NOT NULL,
        [DisplayName]     NVARCHAR(200)   NULL,
        [Description]     NVARCHAR(500)   NULL,
        [IpAddress]       NVARCHAR(45)    NULL,
        [Fqdn]            NVARCHAR(255)   NULL,
        [Status]          INT             NOT NULL DEFAULT 0,
        [LastHeartbeat]   DATETIME2(3)    NULL,
        [HeartbeatIntervalSeconds] INT    NOT NULL DEFAULT 60,
        [AgentVersion]    NVARCHAR(20)    NULL,
        [AgentType]       NVARCHAR(50)    NULL,
        [Metadata]        NVARCHAR(MAX)   NULL,
        [CreatedAt]       DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]       DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
        [IsActive]        BIT             NOT NULL DEFAULT 1,
        
        CONSTRAINT [PK_Servers] PRIMARY KEY CLUSTERED ([ServerName]),
        CONSTRAINT [FK_Servers_Status] FOREIGN KEY ([Status]) 
            REFERENCES [system].[ServerStatuses]([Id]),
        CONSTRAINT [CK_Servers_Metadata_Json] CHECK (
            [Metadata] IS NULL OR ISJSON([Metadata]) = 1
        )
    );
    PRINT 'Table [job].[Servers] created.';
END
GO

-- Jobs Table
IF OBJECT_ID('[job].[Jobs]', 'U') IS NULL
BEGIN
    CREATE TABLE [job].[Jobs]
    (
        [JobId]           NVARCHAR(100)   NOT NULL,
        [DisplayName]     NVARCHAR(200)   NOT NULL,
        [Description]     NVARCHAR(1000)  NULL,
        [Category]        NVARCHAR(100)   NULL,
        [Tags]            NVARCHAR(500)   NULL,
        [JobType]         INT             NOT NULL DEFAULT 0,
        [ServerName]      NVARCHAR(100)   NULL,
        [ExecutablePath]  NVARCHAR(500)   NULL,
        [Schedule]        NVARCHAR(200)   NULL,
        [ScheduleDescription] NVARCHAR(500) NULL,
        [IsActive]        BIT             NOT NULL DEFAULT 1,
        [IsCritical]      BIT             NOT NULL DEFAULT 0,
        [LastExecutionId] UNIQUEIDENTIFIER NULL,
        [LastExecutionAt] DATETIME2(3)    NULL,
        [LastStatus]      INT             NULL,
        [LastDurationMs]  BIGINT          NULL,
        [TotalExecutions] BIGINT          NOT NULL DEFAULT 0,
        [SuccessCount]    BIGINT          NOT NULL DEFAULT 0,
        [FailureCount]    BIGINT          NOT NULL DEFAULT 0,
        [AvgDurationMs]   BIGINT          NULL,
        [Configuration]   NVARCHAR(MAX)   NULL,
        [ExpectedDurationMs]    BIGINT    NULL,
        [MaxDurationMs]         BIGINT    NULL,
        [MinExecutionsPerDay]   INT       NULL,
        [CreatedAt]       DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedBy]       NVARCHAR(100)   NULL,
        [UpdatedAt]       DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedBy]       NVARCHAR(100)   NULL,
        
        CONSTRAINT [PK_Jobs] PRIMARY KEY CLUSTERED ([JobId]),
        CONSTRAINT [FK_Jobs_Servers] FOREIGN KEY ([ServerName]) 
            REFERENCES [job].[Servers]([ServerName]),
        CONSTRAINT [FK_Jobs_JobType] FOREIGN KEY ([JobType]) 
            REFERENCES [system].[JobTypes]([Id]),
        CONSTRAINT [FK_Jobs_LastStatus] FOREIGN KEY ([LastStatus]) 
            REFERENCES [system].[JobStatuses]([Id]),
        CONSTRAINT [CK_Jobs_Configuration_Json] CHECK (
            [Configuration] IS NULL OR ISJSON([Configuration]) = 1
        )
    );

    CREATE NONCLUSTERED INDEX [IX_Jobs_ServerName]
    ON [job].[Jobs] ([ServerName])
    INCLUDE ([DisplayName], [JobType], [IsActive], [LastStatus])
    WHERE [IsActive] = 1;

    CREATE NONCLUSTERED INDEX [IX_Jobs_LastStatus]
    ON [job].[Jobs] ([LastStatus], [IsActive])
    INCLUDE ([JobId], [DisplayName], [LastExecutionAt])
    WHERE [IsActive] = 1;

    CREATE NONCLUSTERED INDEX [IX_Jobs_Category]
    ON [job].[Jobs] ([Category])
    INCLUDE ([JobId], [DisplayName], [Tags])
    WHERE [IsActive] = 1;

    PRINT 'Table [job].[Jobs] created.';
END
GO

-- Job Executions Table
IF OBJECT_ID('[job].[JobExecutions]', 'U') IS NULL
BEGIN
    CREATE TABLE [job].[JobExecutions]
    (
        [Id]              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        [JobId]           NVARCHAR(100)    NOT NULL,
        [StartedAt]       DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
        [CompletedAt]     DATETIME2(3)     NULL,
        [DurationMs]      AS DATEDIFF(MILLISECOND, [StartedAt], [CompletedAt]) PERSISTED,
        [Status]          INT              NOT NULL DEFAULT 0,
        [ServerName]      NVARCHAR(100)    NOT NULL,
        [LogCount]        INT              NOT NULL DEFAULT 0,
        [TraceCount]      INT              NOT NULL DEFAULT 0,
        [DebugCount]      INT              NOT NULL DEFAULT 0,
        [InfoCount]       INT              NOT NULL DEFAULT 0,
        [WarningCount]    INT              NOT NULL DEFAULT 0,
        [ErrorCount]      INT              NOT NULL DEFAULT 0,
        [CriticalCount]   INT              NOT NULL DEFAULT 0,
        [TriggerType]     NVARCHAR(50)     NULL,
        [TriggeredBy]     NVARCHAR(100)    NULL,
        [CorrelationId]   NVARCHAR(100)    NULL,
        [Parameters]      NVARCHAR(MAX)    NULL,
        [ResultSummary]   NVARCHAR(MAX)    NULL,
        [ResultCode]      INT              NULL,
        [ErrorMessage]    NVARCHAR(MAX)    NULL,
        [ErrorCategory]   NVARCHAR(100)    NULL,
        
        CONSTRAINT [PK_JobExecutions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_JobExecutions_Jobs] FOREIGN KEY ([JobId]) 
            REFERENCES [job].[Jobs]([JobId]),
        CONSTRAINT [FK_JobExecutions_Servers] FOREIGN KEY ([ServerName]) 
            REFERENCES [job].[Servers]([ServerName]),
        CONSTRAINT [FK_JobExecutions_Status] FOREIGN KEY ([Status]) 
            REFERENCES [system].[JobStatuses]([Id]),
        CONSTRAINT [CK_JobExecutions_Parameters_Json] CHECK (
            [Parameters] IS NULL OR ISJSON([Parameters]) = 1
        ),
        CONSTRAINT [CK_JobExecutions_ResultSummary_Json] CHECK (
            [ResultSummary] IS NULL OR ISJSON([ResultSummary]) = 1
        ),
        CONSTRAINT [CK_JobExecutions_Dates] CHECK (
            [CompletedAt] IS NULL OR [CompletedAt] >= [StartedAt]
        )
    );

    CREATE NONCLUSTERED INDEX [IX_JobExecutions_JobId_StartedAt]
    ON [job].[JobExecutions] ([JobId], [StartedAt] DESC)
    INCLUDE ([Status], [DurationMs], [ErrorCount], [WarningCount]);

    CREATE NONCLUSTERED INDEX [IX_JobExecutions_StartedAt]
    ON [job].[JobExecutions] ([StartedAt] DESC)
    INCLUDE ([JobId], [Status], [ServerName], [DurationMs]);

    CREATE NONCLUSTERED INDEX [IX_JobExecutions_Status]
    ON [job].[JobExecutions] ([Status])
    INCLUDE ([JobId], [StartedAt], [ServerName])
    WHERE [Status] IN (0, 1);

    CREATE NONCLUSTERED INDEX [IX_JobExecutions_ServerName]
    ON [job].[JobExecutions] ([ServerName], [StartedAt] DESC)
    INCLUDE ([JobId], [Status]);

    CREATE NONCLUSTERED INDEX [IX_JobExecutions_CorrelationId]
    ON [job].[JobExecutions] ([CorrelationId])
    WHERE [CorrelationId] IS NOT NULL;

    PRINT 'Table [job].[JobExecutions] created.';
END
GO

-- ============================================================================
-- SECTION 5: Log Entries Table (Partitioned)
-- ============================================================================
PRINT 'Creating partitioned LogEntries table...';

-- Create partition function (if not exists)
IF NOT EXISTS (SELECT * FROM sys.partition_functions WHERE name = 'PF_LogEntries_Monthly')
BEGIN
    CREATE PARTITION FUNCTION [PF_LogEntries_Monthly](DATETIME2(3))
    AS RANGE RIGHT FOR VALUES (
        '2024-01-01', '2024-02-01', '2024-03-01', '2024-04-01',
        '2024-05-01', '2024-06-01', '2024-07-01', '2024-08-01',
        '2024-09-01', '2024-10-01', '2024-11-01', '2024-12-01',
        '2025-01-01', '2025-02-01', '2025-03-01', '2025-04-01',
        '2025-05-01', '2025-06-01', '2025-07-01', '2025-08-01',
        '2025-09-01', '2025-10-01', '2025-11-01', '2025-12-01',
        '2026-01-01'
    );
    PRINT 'Partition function created.';
END
GO

-- Create partition scheme (if not exists)
IF NOT EXISTS (SELECT * FROM sys.partition_schemes WHERE name = 'PS_LogEntries_Monthly')
BEGIN
    CREATE PARTITION SCHEME [PS_LogEntries_Monthly]
    AS PARTITION [PF_LogEntries_Monthly]
    ALL TO ([LogEntries_FG]);
    PRINT 'Partition scheme created.';
END
GO

-- Log Entries Table
IF OBJECT_ID('[log].[LogEntries]', 'U') IS NULL
BEGIN
    CREATE TABLE [log].[LogEntries]
    (
        [Id]                UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        [Timestamp]         DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
        [Level]             INT              NOT NULL,
        [Message]           NVARCHAR(4000)   NOT NULL,
        [MessageTemplate]   NVARCHAR(2000)   NULL,
        [JobId]             NVARCHAR(100)    NULL,
        [JobExecutionId]    UNIQUEIDENTIFIER NULL,
        [ServerName]        NVARCHAR(100)    NOT NULL,
        [Category]          NVARCHAR(500)    NULL,
        [SourceContext]     NVARCHAR(500)    NULL,
        [CorrelationId]     NVARCHAR(100)    NULL,
        [TraceId]           NVARCHAR(50)     NULL,
        [SpanId]            NVARCHAR(50)     NULL,
        [ParentSpanId]      NVARCHAR(50)     NULL,
        [ExceptionType]     NVARCHAR(500)    NULL,
        [ExceptionMessage]  NVARCHAR(4000)   NULL,
        [ExceptionStackTrace] NVARCHAR(MAX)  NULL,
        [ExceptionSource]   NVARCHAR(500)    NULL,
        [HasException]      AS CAST(CASE WHEN [ExceptionType] IS NOT NULL THEN 1 ELSE 0 END AS BIT) PERSISTED,
        [Properties]        NVARCHAR(MAX)    NULL,
        [Tags]              NVARCHAR(500)    NULL,
        [Environment]       NVARCHAR(50)     NULL,
        [ApplicationVersion] NVARCHAR(50)    NULL,
        [ReceivedAt]        DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
        [ClientIp]          NVARCHAR(45)     NULL,
        
        CONSTRAINT [PK_LogEntries] PRIMARY KEY CLUSTERED ([Timestamp], [Id])
            ON [PS_LogEntries_Monthly]([Timestamp]),
        CONSTRAINT [FK_LogEntries_Level] FOREIGN KEY ([Level]) 
            REFERENCES [system].[LogLevels]([Id]),
        CONSTRAINT [CK_LogEntries_Properties_Json] CHECK (
            [Properties] IS NULL OR ISJSON([Properties]) = 1
        ),
        CONSTRAINT [CK_LogEntries_Level_Range] CHECK (
            [Level] >= 0 AND [Level] <= 5
        )
    )
    ON [PS_LogEntries_Monthly]([Timestamp]);

    -- Indexes
    CREATE NONCLUSTERED INDEX [IX_LogEntries_JobId_Timestamp]
    ON [log].[LogEntries] ([JobId], [Timestamp] DESC)
    INCLUDE ([Level], [Message], [JobExecutionId])
    WHERE [JobId] IS NOT NULL
    ON [PS_LogEntries_Monthly]([Timestamp]);

    CREATE NONCLUSTERED INDEX [IX_LogEntries_JobExecutionId]
    ON [log].[LogEntries] ([JobExecutionId], [Timestamp])
    INCLUDE ([Level], [Message])
    WHERE [JobExecutionId] IS NOT NULL
    ON [PS_LogEntries_Monthly]([Timestamp]);

    CREATE NONCLUSTERED INDEX [IX_LogEntries_Level_Timestamp]
    ON [log].[LogEntries] ([Level], [Timestamp] DESC)
    INCLUDE ([JobId], [Message], [ServerName], [ExceptionType])
    WHERE [Level] >= 3
    ON [PS_LogEntries_Monthly]([Timestamp]);

    CREATE NONCLUSTERED INDEX [IX_LogEntries_ServerName_Timestamp]
    ON [log].[LogEntries] ([ServerName], [Timestamp] DESC)
    INCLUDE ([JobId], [Level], [Message])
    ON [PS_LogEntries_Monthly]([Timestamp]);

    CREATE NONCLUSTERED INDEX [IX_LogEntries_CorrelationId]
    ON [log].[LogEntries] ([CorrelationId], [Timestamp])
    INCLUDE ([JobId], [Level], [Message])
    WHERE [CorrelationId] IS NOT NULL
    ON [PS_LogEntries_Monthly]([Timestamp]);

    CREATE NONCLUSTERED INDEX [IX_LogEntries_ExceptionType]
    ON [log].[LogEntries] ([ExceptionType], [Timestamp] DESC)
    INCLUDE ([JobId], [ExceptionMessage], [ServerName])
    WHERE [ExceptionType] IS NOT NULL
    ON [PS_LogEntries_Monthly]([Timestamp]);

    PRINT 'Table [log].[LogEntries] created with partitioning.';
END
GO

-- ============================================================================
-- SECTION 6: Authentication Tables
-- ============================================================================
PRINT 'Creating authentication tables...';

-- Users Table
IF OBJECT_ID('[auth].[Users]', 'U') IS NULL
BEGIN
    CREATE TABLE [auth].[Users]
    (
        [Id]              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        [Username]        NVARCHAR(100)    NOT NULL,
        [Email]           NVARCHAR(255)    NOT NULL,
        [NormalizedUsername] AS UPPER([Username]) PERSISTED,
        [NormalizedEmail] AS UPPER([Email]) PERSISTED,
        [PasswordHash]    NVARCHAR(500)    NOT NULL,
        [SecurityStamp]   NVARCHAR(100)    NOT NULL DEFAULT NEWID(),
        [DisplayName]     NVARCHAR(200)    NULL,
        [Role]            INT              NOT NULL DEFAULT 0,
        [IsActive]        BIT              NOT NULL DEFAULT 1,
        [IsLocked]        BIT              NOT NULL DEFAULT 0,
        [LockoutEnd]      DATETIME2(3)     NULL,
        [FailedLoginCount] INT             NOT NULL DEFAULT 0,
        [LastLoginAt]     DATETIME2(3)     NULL,
        [LastLoginIp]     NVARCHAR(45)     NULL,
        [LastPasswordChangeAt] DATETIME2(3) NULL,
        [Preferences]     NVARCHAR(MAX)    NULL,
        [CreatedAt]       DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]       DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
        
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_Users_Username] UNIQUE ([NormalizedUsername]),
        CONSTRAINT [UQ_Users_Email] UNIQUE ([NormalizedEmail]),
        CONSTRAINT [FK_Users_Role] FOREIGN KEY ([Role]) 
            REFERENCES [system].[UserRoles]([Id]),
        CONSTRAINT [CK_Users_Preferences_Json] CHECK (
            [Preferences] IS NULL OR ISJSON([Preferences]) = 1
        )
    );

    CREATE NONCLUSTERED INDEX [IX_Users_Username]
    ON [auth].[Users] ([NormalizedUsername])
    INCLUDE ([PasswordHash], [Role], [IsActive]);

    PRINT 'Table [auth].[Users] created.';
END
GO

-- User API Keys Table
IF OBJECT_ID('[auth].[UserApiKeys]', 'U') IS NULL
BEGIN
    CREATE TABLE [auth].[UserApiKeys]
    (
        [Id]              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        [UserId]          UNIQUEIDENTIFIER NULL,
        [ServerName]      NVARCHAR(100)    NULL,
        [Name]            NVARCHAR(100)    NOT NULL,
        [KeyHash]         NVARCHAR(500)    NOT NULL,
        [KeyPrefix]       NVARCHAR(10)     NOT NULL,
        [Scopes]          NVARCHAR(500)    NOT NULL DEFAULT 'logs:write',
        [IsActive]        BIT              NOT NULL DEFAULT 1,
        [ExpiresAt]       DATETIME2(3)     NULL,
        [LastUsedAt]      DATETIME2(3)     NULL,
        [LastUsedIp]      NVARCHAR(45)     NULL,
        [UsageCount]      BIGINT           NOT NULL DEFAULT 0,
        [CreatedAt]       DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedBy]       UNIQUEIDENTIFIER NULL,
        [RevokedAt]       DATETIME2(3)     NULL,
        [RevokedBy]       UNIQUEIDENTIFIER NULL,
        [RevocationReason] NVARCHAR(500)   NULL,
        
        CONSTRAINT [PK_UserApiKeys] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_UserApiKeys_Users] FOREIGN KEY ([UserId]) 
            REFERENCES [auth].[Users]([Id]),
        CONSTRAINT [FK_UserApiKeys_Servers] FOREIGN KEY ([ServerName]) 
            REFERENCES [job].[Servers]([ServerName]),
        CONSTRAINT [UQ_UserApiKeys_KeyHash] UNIQUE ([KeyHash])
    );

    CREATE NONCLUSTERED INDEX [IX_UserApiKeys_KeyPrefix]
    ON [auth].[UserApiKeys] ([KeyPrefix])
    INCLUDE ([KeyHash], [Scopes], [IsActive], [ExpiresAt])
    WHERE [IsActive] = 1;

    CREATE NONCLUSTERED INDEX [IX_UserApiKeys_UserId]
    ON [auth].[UserApiKeys] ([UserId])
    WHERE [UserId] IS NOT NULL;

    PRINT 'Table [auth].[UserApiKeys] created.';
END
GO

-- Refresh Tokens Table
IF OBJECT_ID('[auth].[RefreshTokens]', 'U') IS NULL
BEGIN
    CREATE TABLE [auth].[RefreshTokens]
    (
        [Id]              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        [UserId]          UNIQUEIDENTIFIER NOT NULL,
        [TokenHash]       NVARCHAR(500)    NOT NULL,
        [ExpiresAt]       DATETIME2(3)     NOT NULL,
        [CreatedAt]       DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedByIp]     NVARCHAR(45)     NULL,
        [RevokedAt]       DATETIME2(3)     NULL,
        [RevokedByIp]     NVARCHAR(45)     NULL,
        [ReplacedByTokenId] UNIQUEIDENTIFIER NULL,
        
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users] FOREIGN KEY ([UserId]) 
            REFERENCES [auth].[Users]([Id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_RefreshTokens_TokenHash]
    ON [auth].[RefreshTokens] ([TokenHash])
    INCLUDE ([UserId], [ExpiresAt], [RevokedAt]);

    CREATE NONCLUSTERED INDEX [IX_RefreshTokens_UserId]
    ON [auth].[RefreshTokens] ([UserId], [ExpiresAt] DESC);

    PRINT 'Table [auth].[RefreshTokens] created.';
END
GO

-- ============================================================================
-- SECTION 7: Alert Tables
-- ============================================================================
PRINT 'Creating alert tables...';

-- Alerts Table
IF OBJECT_ID('[alert].[Alerts]', 'U') IS NULL
BEGIN
    CREATE TABLE [alert].[Alerts]
    (
        [Id]              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        [Name]            NVARCHAR(200)    NOT NULL,
        [Description]     NVARCHAR(1000)   NULL,
        [AlertType]       INT              NOT NULL,
        [Severity]        INT              NOT NULL DEFAULT 1,
        [Condition]       NVARCHAR(MAX)    NOT NULL,
        [IsActive]        BIT              NOT NULL DEFAULT 1,
        [ThrottleMinutes] INT              NOT NULL DEFAULT 15,
        [LastTriggeredAt] DATETIME2(3)     NULL,
        [TriggerCount]    INT              NOT NULL DEFAULT 0,
        [NotificationChannels] NVARCHAR(MAX) NULL,
        [JobId]           NVARCHAR(100)    NULL,
        [ServerName]      NVARCHAR(100)    NULL,
        [CreatedAt]       DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedBy]       NVARCHAR(100)    NULL,
        [UpdatedAt]       DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedBy]       NVARCHAR(100)    NULL,
        
        CONSTRAINT [PK_Alerts] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Alerts_AlertType] FOREIGN KEY ([AlertType]) 
            REFERENCES [system].[AlertTypes]([Id]),
        CONSTRAINT [FK_Alerts_Severity] FOREIGN KEY ([Severity]) 
            REFERENCES [system].[AlertSeverities]([Id]),
        CONSTRAINT [FK_Alerts_Jobs] FOREIGN KEY ([JobId]) 
            REFERENCES [job].[Jobs]([JobId]),
        CONSTRAINT [FK_Alerts_Servers] FOREIGN KEY ([ServerName]) 
            REFERENCES [job].[Servers]([ServerName]),
        CONSTRAINT [CK_Alerts_Condition_Json] CHECK (ISJSON([Condition]) = 1),
        CONSTRAINT [CK_Alerts_NotificationChannels_Json] CHECK (
            [NotificationChannels] IS NULL OR ISJSON([NotificationChannels]) = 1
        ),
        CONSTRAINT [CK_Alerts_ThrottleMinutes] CHECK ([ThrottleMinutes] >= 0)
    );

    CREATE NONCLUSTERED INDEX [IX_Alerts_IsActive]
    ON [alert].[Alerts] ([IsActive])
    INCLUDE ([AlertType], [Severity], [Condition], [LastTriggeredAt])
    WHERE [IsActive] = 1;

    PRINT 'Table [alert].[Alerts] created.';
END
GO

-- Alert Instances Table
IF OBJECT_ID('[alert].[AlertInstances]', 'U') IS NULL
BEGIN
    CREATE TABLE [alert].[AlertInstances]
    (
        [Id]              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        [AlertId]         UNIQUEIDENTIFIER NOT NULL,
        [TriggeredAt]     DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
        [Message]         NVARCHAR(2000)   NOT NULL,
        [Context]         NVARCHAR(MAX)    NULL,
        [JobId]           NVARCHAR(100)    NULL,
        [JobExecutionId]  UNIQUEIDENTIFIER NULL,
        [ServerName]      NVARCHAR(100)    NULL,
        [Severity]        INT              NULL,
        [Status]          INT              NOT NULL DEFAULT 0,
        [AcknowledgedAt]  DATETIME2(3)     NULL,
        [AcknowledgedBy]  NVARCHAR(100)    NULL,
        [AcknowledgeNote] NVARCHAR(1000)   NULL,
        [ResolvedAt]      DATETIME2(3)     NULL,
        [ResolvedBy]      NVARCHAR(100)    NULL,
        [ResolutionNote]  NVARCHAR(1000)   NULL,
        [NotificationsSent] NVARCHAR(MAX)  NULL,
        
        CONSTRAINT [PK_AlertInstances] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_AlertInstances_Alerts] FOREIGN KEY ([AlertId]) 
            REFERENCES [alert].[Alerts]([Id]),
        CONSTRAINT [FK_AlertInstances_Jobs] FOREIGN KEY ([JobId]) 
            REFERENCES [job].[Jobs]([JobId]),
        CONSTRAINT [FK_AlertInstances_JobExecutions] FOREIGN KEY ([JobExecutionId]) 
            REFERENCES [job].[JobExecutions]([Id]),
        CONSTRAINT [CK_AlertInstances_Context_Json] CHECK (
            [Context] IS NULL OR ISJSON([Context]) = 1
        ),
        CONSTRAINT [CK_AlertInstances_Status] CHECK ([Status] IN (0, 1, 2, 3))
    );

    CREATE NONCLUSTERED INDEX [IX_AlertInstances_TriggeredAt]
    ON [alert].[AlertInstances] ([TriggeredAt] DESC)
    INCLUDE ([AlertId], [Status], [Message], [Severity]);

    CREATE NONCLUSTERED INDEX [IX_AlertInstances_Status]
    ON [alert].[AlertInstances] ([Status], [TriggeredAt] DESC)
    INCLUDE ([AlertId], [Message])
    WHERE [Status] = 0;

    CREATE NONCLUSTERED INDEX [IX_AlertInstances_AlertId]
    ON [alert].[AlertInstances] ([AlertId], [TriggeredAt] DESC)
    INCLUDE ([Status], [Message]);

    PRINT 'Table [alert].[AlertInstances] created.';
END
GO

-- ============================================================================
-- SECTION 8: System Tables
-- ============================================================================
PRINT 'Creating system tables...';

-- Configuration Table
IF OBJECT_ID('[system].[Configuration]', 'U') IS NULL
BEGIN
    CREATE TABLE [system].[Configuration]
    (
        [Key]             NVARCHAR(200)    NOT NULL,
        [Value]           NVARCHAR(MAX)    NOT NULL,
        [DataType]        NVARCHAR(50)     NOT NULL DEFAULT 'string',
        [Category]        NVARCHAR(100)    NOT NULL DEFAULT 'General',
        [Description]     NVARCHAR(500)    NULL,
        [IsEncrypted]     BIT              NOT NULL DEFAULT 0,
        [UpdatedAt]       DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedBy]       NVARCHAR(100)    NULL,
        
        CONSTRAINT [PK_Configuration] PRIMARY KEY CLUSTERED ([Key]),
        CONSTRAINT [CK_Configuration_DataType] CHECK (
            [DataType] IN ('string', 'int', 'bool', 'json', 'datetime')
        )
    );

    INSERT INTO [system].[Configuration] ([Key], [Value], [DataType], [Category], [Description])
    VALUES
        ('Retention.DefaultDays', '90', 'int', 'Retention', 'Default log retention period in days'),
        ('Retention.ErrorDays', '180', 'int', 'Retention', 'Error log retention period in days'),
        ('Retention.CriticalDays', '365', 'int', 'Retention', 'Critical log retention period in days'),
        ('Retention.CleanupTimeUtc', '02:00:00', 'string', 'Retention', 'UTC time for daily cleanup job'),
        ('Ingestion.MaxBatchSize', '1000', 'int', 'Ingestion', 'Maximum batch size for log ingestion'),
        ('Ingestion.MaxQueueSize', '50000', 'int', 'Ingestion', 'Maximum in-memory queue size'),
        ('Dashboard.StatsCacheTtlSeconds', '30', 'int', 'Dashboard', 'Dashboard statistics cache TTL'),
        ('Alert.EvaluationIntervalSeconds', '30', 'int', 'Alerts', 'Alert evaluation interval'),
        ('Alert.DefaultThrottleMinutes', '15', 'int', 'Alerts', 'Default alert throttle period'),
        ('Server.HeartbeatTimeoutSeconds', '180', 'int', 'Servers', 'Server offline threshold'),
        ('System.MaintenanceMode', 'false', 'bool', 'System', 'Enable maintenance mode');

    PRINT 'Table [system].[Configuration] created.';
END
GO

-- Audit Log Table
IF OBJECT_ID('[system].[AuditLog]', 'U') IS NULL
BEGIN
    CREATE TABLE [system].[AuditLog]
    (
        [Id]              BIGINT IDENTITY(1,1) NOT NULL,
        [Timestamp]       DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
        [UserId]          UNIQUEIDENTIFIER NULL,
        [Username]        NVARCHAR(100)    NULL,
        [Action]          NVARCHAR(100)    NOT NULL,
        [EntityType]      NVARCHAR(100)    NOT NULL,
        [EntityId]        NVARCHAR(200)    NULL,
        [OldValues]       NVARCHAR(MAX)    NULL,
        [NewValues]       NVARCHAR(MAX)    NULL,
        [IpAddress]       NVARCHAR(45)     NULL,
        [UserAgent]       NVARCHAR(500)    NULL,
        
        CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [CK_AuditLog_OldValues_Json] CHECK (
            [OldValues] IS NULL OR ISJSON([OldValues]) = 1
        ),
        CONSTRAINT [CK_AuditLog_NewValues_Json] CHECK (
            [NewValues] IS NULL OR ISJSON([NewValues]) = 1
        )
    );

    CREATE NONCLUSTERED INDEX [IX_AuditLog_Timestamp]
    ON [system].[AuditLog] ([Timestamp] DESC)
    INCLUDE ([UserId], [Action], [EntityType]);

    CREATE NONCLUSTERED INDEX [IX_AuditLog_UserId]
    ON [system].[AuditLog] ([UserId], [Timestamp] DESC)
    WHERE [UserId] IS NOT NULL;

    PRINT 'Table [system].[AuditLog] created.';
END
GO

-- Dashboard Cache Table
IF OBJECT_ID('[system].[DashboardCache]', 'U') IS NULL
BEGIN
    CREATE TABLE [system].[DashboardCache]
    (
        [Key]             NVARCHAR(200)    NOT NULL,
        [Value]           NVARCHAR(MAX)    NOT NULL,
        [ComputedAt]      DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
        [ExpiresAt]       DATETIME2(3)     NOT NULL,
        
        CONSTRAINT [PK_DashboardCache] PRIMARY KEY CLUSTERED ([Key])
    );

    PRINT 'Table [system].[DashboardCache] created.';
END
GO

-- Schema Version Table
IF OBJECT_ID('[system].[SchemaVersion]', 'U') IS NULL
BEGIN
    CREATE TABLE [system].[SchemaVersion]
    (
        [Version]         INT              NOT NULL,
        [Description]     NVARCHAR(500)    NOT NULL,
        [AppliedAt]       DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
        [AppliedBy]       NVARCHAR(100)    NULL,
        [Script]          NVARCHAR(500)    NULL,
        
        CONSTRAINT [PK_SchemaVersion] PRIMARY KEY CLUSTERED ([Version])
    );

    INSERT INTO [system].[SchemaVersion] ([Version], [Description], [AppliedBy], [Script])
    VALUES (1, 'Initial schema creation', SYSTEM_USER, '001_CreateSchema.sql');

    PRINT 'Table [system].[SchemaVersion] created.';
END
GO

-- ============================================================================
-- SECTION 9: Seed Data
-- ============================================================================
PRINT 'Inserting seed data...';

-- Create default admin user (password needs to be set properly via application)
IF NOT EXISTS (SELECT 1 FROM [auth].[Users] WHERE [Username] = 'admin')
BEGIN
    INSERT INTO [auth].[Users] 
    (
        [Id], [Username], [Email], [PasswordHash], [DisplayName], [Role], [IsActive]
    )
    VALUES
    (
        NEWID(),
        'admin',
        'admin@company.com',
        -- Placeholder - must be set via application using proper password hashing
        'PLACEHOLDER_CHANGE_VIA_APPLICATION',
        'System Administrator',
        2,  -- Administrator
        1
    );
    PRINT 'Default admin user created.';
END
GO

-- ============================================================================
-- COMPLETION
-- ============================================================================
PRINT '============================================================';
PRINT 'FMS Log Nexus - Database Schema Creation Complete';
PRINT 'Completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================================';
GO
