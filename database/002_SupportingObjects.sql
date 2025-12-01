-- ============================================================================
-- FMS Log Nexus - Supporting Database Objects
-- Version: 1.0.0
-- Date: 2024-01-15
-- 
-- This script creates views, stored procedures, functions, and types
-- Run this AFTER 001_CreateSchema.sql
-- ============================================================================

SET NOCOUNT ON;
GO

USE [FMSLogNexus]
GO

PRINT '============================================================';
PRINT 'FMS Log Nexus - Supporting Objects Creation';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================================';
GO

-- ============================================================================
-- SECTION 1: User-Defined Types
-- ============================================================================
PRINT 'Creating user-defined types...';

IF NOT EXISTS (SELECT * FROM sys.types WHERE name = 'LogEntryTableType' AND schema_id = SCHEMA_ID('log'))
BEGIN
    CREATE TYPE [log].[LogEntryTableType] AS TABLE
    (
        [Id]                UNIQUEIDENTIFIER NULL,
        [Timestamp]         DATETIME2(3)     NULL,
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
        [Properties]        NVARCHAR(MAX)    NULL,
        [Tags]              NVARCHAR(500)    NULL,
        [Environment]       NVARCHAR(50)     NULL,
        [ApplicationVersion] NVARCHAR(50)    NULL,
        [ClientIp]          NVARCHAR(45)     NULL
    );
    PRINT 'Type [log].[LogEntryTableType] created.';
END
GO

-- ============================================================================
-- SECTION 2: Scalar Functions
-- ============================================================================
PRINT 'Creating scalar functions...';

-- Get Log Level Name
CREATE OR ALTER FUNCTION [system].[fn_GetLogLevelName](@LevelId INT)
RETURNS NVARCHAR(20)
AS
BEGIN
    RETURN (SELECT [Name] FROM [system].[LogLevels] WHERE [Id] = @LevelId);
END
GO

-- Get Job Status Name
CREATE OR ALTER FUNCTION [system].[fn_GetJobStatusName](@StatusId INT)
RETURNS NVARCHAR(20)
AS
BEGIN
    RETURN (SELECT [Name] FROM [system].[JobStatuses] WHERE [Id] = @StatusId);
END
GO

-- Format Duration
CREATE OR ALTER FUNCTION [system].[fn_FormatDuration](@DurationMs BIGINT)
RETURNS NVARCHAR(50)
AS
BEGIN
    IF @DurationMs IS NULL RETURN NULL;
    IF @DurationMs < 1000 RETURN CAST(@DurationMs AS NVARCHAR(20)) + 'ms';
    IF @DurationMs < 60000 RETURN CAST(CAST(@DurationMs / 1000.0 AS DECIMAL(10,1)) AS NVARCHAR(20)) + 's';
    IF @DurationMs < 3600000 RETURN CAST(@DurationMs / 60000 AS NVARCHAR(10)) + 'm ' + CAST((@DurationMs % 60000) / 1000 AS NVARCHAR(10)) + 's';
    RETURN CAST(@DurationMs / 3600000 AS NVARCHAR(10)) + 'h ' + CAST((@DurationMs % 3600000) / 60000 AS NVARCHAR(10)) + 'm';
END
GO

-- Get Config Value
CREATE OR ALTER FUNCTION [system].[fn_GetConfigValue](@Key NVARCHAR(200), @DefaultValue NVARCHAR(MAX) = NULL)
RETURNS NVARCHAR(MAX)
AS
BEGIN
    DECLARE @Value NVARCHAR(MAX);
    SELECT @Value = [Value] FROM [system].[Configuration] WHERE [Key] = @Key;
    RETURN ISNULL(@Value, @DefaultValue);
END
GO

-- Get Config Int
CREATE OR ALTER FUNCTION [system].[fn_GetConfigInt](@Key NVARCHAR(200), @DefaultValue INT = 0)
RETURNS INT
AS
BEGIN
    DECLARE @Value NVARCHAR(MAX);
    SELECT @Value = [Value] FROM [system].[Configuration] WHERE [Key] = @Key AND [DataType] = 'int';
    RETURN ISNULL(TRY_CAST(@Value AS INT), @DefaultValue);
END
GO

-- Calculate Job Health Score
CREATE OR ALTER FUNCTION [job].[fn_CalculateJobHealthScore](@JobId NVARCHAR(100), @DaysToConsider INT = 7)
RETURNS INT
AS
BEGIN
    DECLARE @HealthScore INT = 100;
    DECLARE @TotalExecutions INT, @FailedExecutions INT, @WarningExecutions INT;
    DECLARE @AvgErrorsPerExecution DECIMAL(10,2);
    DECLARE @CutoffDate DATETIME2(3) = DATEADD(DAY, -@DaysToConsider, SYSUTCDATETIME());

    SELECT 
        @TotalExecutions = COUNT(*),
        @FailedExecutions = SUM(CASE WHEN [Status] = 3 THEN 1 ELSE 0 END),
        @WarningExecutions = SUM(CASE WHEN [Status] = 6 THEN 1 ELSE 0 END),
        @AvgErrorsPerExecution = AVG(CAST([ErrorCount] AS DECIMAL(10,2)))
    FROM [job].[JobExecutions]
    WHERE [JobId] = @JobId AND [StartedAt] >= @CutoffDate AND [Status] IN (2, 3, 6);

    IF @TotalExecutions = 0 RETURN 100;

    SET @HealthScore = @HealthScore - CAST((CAST(@FailedExecutions AS DECIMAL) / @TotalExecutions * 50) AS INT);
    SET @HealthScore = @HealthScore - CAST((CAST(@WarningExecutions AS DECIMAL) / @TotalExecutions * 20) AS INT);

    IF @AvgErrorsPerExecution > 0
        SET @HealthScore = @HealthScore - CASE 
            WHEN @AvgErrorsPerExecution > 10 THEN 30
            WHEN @AvgErrorsPerExecution > 5 THEN 20
            WHEN @AvgErrorsPerExecution > 1 THEN 10
            ELSE 5 END;

    RETURN CASE WHEN @HealthScore < 0 THEN 0 ELSE @HealthScore END;
END
GO

PRINT 'Scalar functions created.';
GO

-- ============================================================================
-- SECTION 3: Table-Valued Functions
-- ============================================================================
PRINT 'Creating table-valued functions...';

-- Get Log Statistics
CREATE OR ALTER FUNCTION [log].[fn_GetLogStatistics](
    @StartDate DATETIME2(3), @EndDate DATETIME2(3),
    @JobId NVARCHAR(100) = NULL, @ServerName NVARCHAR(100) = NULL)
RETURNS TABLE AS RETURN (
    SELECT 
        COUNT(*) AS TotalLogs,
        SUM(CASE WHEN [Level] = 0 THEN 1 ELSE 0 END) AS TraceCount,
        SUM(CASE WHEN [Level] = 1 THEN 1 ELSE 0 END) AS DebugCount,
        SUM(CASE WHEN [Level] = 2 THEN 1 ELSE 0 END) AS InfoCount,
        SUM(CASE WHEN [Level] = 3 THEN 1 ELSE 0 END) AS WarningCount,
        SUM(CASE WHEN [Level] = 4 THEN 1 ELSE 0 END) AS ErrorCount,
        SUM(CASE WHEN [Level] = 5 THEN 1 ELSE 0 END) AS CriticalCount,
        SUM(CASE WHEN [ExceptionType] IS NOT NULL THEN 1 ELSE 0 END) AS ExceptionCount,
        COUNT(DISTINCT [JobId]) AS UniqueJobs,
        COUNT(DISTINCT [JobExecutionId]) AS UniqueExecutions,
        MIN([Timestamp]) AS FirstLog, MAX([Timestamp]) AS LastLog
    FROM [log].[LogEntries]
    WHERE [Timestamp] >= @StartDate AND [Timestamp] < @EndDate
      AND (@JobId IS NULL OR [JobId] = @JobId)
      AND (@ServerName IS NULL OR [ServerName] = @ServerName)
);
GO

-- Get Hourly Log Trend
CREATE OR ALTER FUNCTION [log].[fn_GetHourlyLogTrend](
    @StartDate DATETIME2(3), @EndDate DATETIME2(3), @JobId NVARCHAR(100) = NULL)
RETURNS TABLE AS RETURN (
    SELECT 
        DATEADD(HOUR, DATEDIFF(HOUR, 0, [Timestamp]), 0) AS HourBucket,
        COUNT(*) AS TotalCount,
        SUM(CASE WHEN [Level] >= 4 THEN 1 ELSE 0 END) AS ErrorCount,
        SUM(CASE WHEN [Level] = 3 THEN 1 ELSE 0 END) AS WarningCount,
        SUM(CASE WHEN [Level] <= 2 THEN 1 ELSE 0 END) AS InfoCount
    FROM [log].[LogEntries]
    WHERE [Timestamp] >= @StartDate AND [Timestamp] < @EndDate
      AND (@JobId IS NULL OR [JobId] = @JobId)
    GROUP BY DATEADD(HOUR, DATEDIFF(HOUR, 0, [Timestamp]), 0)
);
GO

-- Get Top Exceptions
CREATE OR ALTER FUNCTION [log].[fn_GetTopExceptions](
    @StartDate DATETIME2(3), @EndDate DATETIME2(3), @TopN INT = 10)
RETURNS TABLE AS RETURN (
    SELECT TOP (@TopN)
        [ExceptionType], COUNT(*) AS OccurrenceCount,
        COUNT(DISTINCT [JobId]) AS AffectedJobs,
        MAX([Timestamp]) AS LastOccurrence, MIN([Timestamp]) AS FirstOccurrence
    FROM [log].[LogEntries]
    WHERE [Timestamp] >= @StartDate AND [Timestamp] < @EndDate AND [ExceptionType] IS NOT NULL
    GROUP BY [ExceptionType]
    ORDER BY COUNT(*) DESC
);
GO

PRINT 'Table-valued functions created.';
GO

-- ============================================================================
-- SECTION 4: Views
-- ============================================================================
PRINT 'Creating views...';

-- Recent Logs View
CREATE OR ALTER VIEW [log].[vw_RecentLogs] AS
SELECT TOP 10000
    le.[Id], le.[Timestamp], le.[Level], ll.[Name] AS LevelName,
    le.[Message], le.[JobId], le.[JobExecutionId], le.[ServerName],
    le.[Category], le.[CorrelationId], le.[ExceptionType], le.[ExceptionMessage],
    le.[HasException], le.[Properties], le.[Tags]
FROM [log].[LogEntries] le
INNER JOIN [system].[LogLevels] ll ON le.[Level] = ll.[Id]
WHERE le.[Timestamp] >= DATEADD(HOUR, -24, SYSUTCDATETIME());
GO

-- Error Logs View
CREATE OR ALTER VIEW [log].[vw_ErrorLogs] AS
SELECT 
    le.[Id], le.[Timestamp], le.[Level], ll.[Name] AS LevelName,
    le.[Message], le.[JobId], j.[DisplayName] AS JobDisplayName,
    le.[JobExecutionId], le.[ServerName], le.[Category], le.[CorrelationId],
    le.[ExceptionType], le.[ExceptionMessage], le.[ExceptionStackTrace], le.[Properties]
FROM [log].[LogEntries] le
INNER JOIN [system].[LogLevels] ll ON le.[Level] = ll.[Id]
LEFT JOIN [job].[Jobs] j ON le.[JobId] = j.[JobId]
WHERE le.[Level] >= 3;
GO

-- Job Overview View
CREATE OR ALTER VIEW [job].[vw_JobOverview] AS
SELECT 
    j.[JobId], j.[DisplayName], j.[Description], j.[Category],
    jt.[Name] AS JobTypeName, j.[ServerName], s.[DisplayName] AS ServerDisplayName,
    j.[Schedule], j.[IsActive], j.[IsCritical], j.[LastExecutionAt],
    js.[Name] AS LastStatusName, j.[LastDurationMs],
    [system].[fn_FormatDuration](j.[LastDurationMs]) AS LastDurationFormatted,
    j.[TotalExecutions], j.[SuccessCount], j.[FailureCount],
    CASE WHEN j.[TotalExecutions] = 0 THEN NULL
         ELSE CAST(j.[SuccessCount] * 100.0 / j.[TotalExecutions] AS DECIMAL(5,2)) END AS SuccessRate,
    j.[AvgDurationMs], [system].[fn_FormatDuration](j.[AvgDurationMs]) AS AvgDurationFormatted,
    [job].[fn_CalculateJobHealthScore](j.[JobId], 7) AS HealthScore,
    j.[CreatedAt], j.[UpdatedAt]
FROM [job].[Jobs] j
LEFT JOIN [system].[JobTypes] jt ON j.[JobType] = jt.[Id]
LEFT JOIN [system].[JobStatuses] js ON j.[LastStatus] = js.[Id]
LEFT JOIN [job].[Servers] s ON j.[ServerName] = s.[ServerName];
GO

-- Active Jobs Summary View
CREATE OR ALTER VIEW [job].[vw_ActiveJobsSummary] AS
SELECT 
    j.[JobId], j.[DisplayName], j.[Category], j.[ServerName],
    j.[LastExecutionAt], j.[LastStatus], js.[Name] AS LastStatusName,
    j.[LastDurationMs], j.[TotalExecutions], j.[SuccessCount], j.[FailureCount],
    [job].[fn_CalculateJobHealthScore](j.[JobId], 7) AS HealthScore,
    CASE 
        WHEN j.[LastExecutionAt] IS NULL THEN 'Never Run'
        WHEN j.[LastStatus] = 3 THEN 'Failed'
        WHEN DATEDIFF(HOUR, j.[LastExecutionAt], SYSUTCDATETIME()) > 48 THEN 'Stale'
        ELSE 'OK' END AS HealthStatus
FROM [job].[Jobs] j
LEFT JOIN [system].[JobStatuses] js ON j.[LastStatus] = js.[Id]
WHERE j.[IsActive] = 1;
GO

-- Recent Executions View
CREATE OR ALTER VIEW [job].[vw_RecentExecutions] AS
SELECT TOP 1000
    je.[Id], je.[JobId], j.[DisplayName] AS JobDisplayName,
    je.[StartedAt], je.[CompletedAt], je.[DurationMs],
    [system].[fn_FormatDuration](je.[DurationMs]) AS DurationFormatted,
    je.[Status], js.[Name] AS StatusName, je.[ServerName],
    je.[LogCount], je.[ErrorCount], je.[WarningCount],
    je.[TriggerType], je.[TriggeredBy], je.[CorrelationId], je.[ErrorMessage]
FROM [job].[JobExecutions] je
INNER JOIN [job].[Jobs] j ON je.[JobId] = j.[JobId]
INNER JOIN [system].[JobStatuses] js ON je.[Status] = js.[Id];
GO

-- Running Executions View
CREATE OR ALTER VIEW [job].[vw_RunningExecutions] AS
SELECT 
    je.[Id], je.[JobId], j.[DisplayName] AS JobDisplayName, j.[IsCritical],
    je.[StartedAt], DATEDIFF(SECOND, je.[StartedAt], SYSUTCDATETIME()) AS RunningSeconds,
    [system].[fn_FormatDuration](DATEDIFF(MILLISECOND, je.[StartedAt], SYSUTCDATETIME())) AS RunningDuration,
    je.[ServerName], je.[LogCount], je.[ErrorCount], je.[WarningCount],
    je.[TriggerType], j.[MaxDurationMs],
    CASE WHEN j.[MaxDurationMs] IS NOT NULL 
         AND DATEDIFF(MILLISECOND, je.[StartedAt], SYSUTCDATETIME()) > j.[MaxDurationMs]
         THEN 1 ELSE 0 END AS IsOverdue
FROM [job].[JobExecutions] je
INNER JOIN [job].[Jobs] j ON je.[JobId] = j.[JobId]
WHERE je.[Status] IN (0, 1);
GO

-- Server Status View
CREATE OR ALTER VIEW [job].[vw_ServerStatus] AS
SELECT 
    s.[ServerName], s.[DisplayName], s.[IpAddress], s.[Status],
    ss.[Name] AS StatusName, s.[LastHeartbeat],
    DATEDIFF(SECOND, s.[LastHeartbeat], SYSUTCDATETIME()) AS SecondsSinceHeartbeat,
    s.[AgentVersion], s.[AgentType], s.[IsActive],
    (SELECT COUNT(*) FROM [job].[Jobs] WHERE [ServerName] = s.[ServerName] AND [IsActive] = 1) AS ActiveJobCount,
    (SELECT COUNT(*) FROM [job].[JobExecutions] WHERE [ServerName] = s.[ServerName] AND [Status] = 1) AS RunningJobCount,
    CASE 
        WHEN s.[LastHeartbeat] IS NULL THEN 'Unknown'
        WHEN DATEDIFF(SECOND, s.[LastHeartbeat], SYSUTCDATETIME()) > s.[HeartbeatIntervalSeconds] * 3 THEN 'Offline'
        WHEN DATEDIFF(SECOND, s.[LastHeartbeat], SYSUTCDATETIME()) > s.[HeartbeatIntervalSeconds] * 2 THEN 'Degraded'
        ELSE 'Online' END AS CalculatedStatus
FROM [job].[Servers] s
LEFT JOIN [system].[ServerStatuses] ss ON s.[Status] = ss.[Id]
WHERE s.[IsActive] = 1;
GO

-- Active Alerts View
CREATE OR ALTER VIEW [alert].[vw_ActiveAlerts] AS
SELECT 
    a.[Id], a.[Name], a.[Description], at.[Name] AS AlertTypeName,
    a.[Severity], asv.[Name] AS SeverityName, asv.[Color] AS SeverityColor,
    a.[Condition], a.[ThrottleMinutes], a.[LastTriggeredAt], a.[TriggerCount],
    a.[JobId], j.[DisplayName] AS JobDisplayName,
    a.[ServerName], s.[DisplayName] AS ServerDisplayName,
    a.[NotificationChannels], a.[CreatedAt], a.[CreatedBy]
FROM [alert].[Alerts] a
INNER JOIN [system].[AlertTypes] at ON a.[AlertType] = at.[Id]
INNER JOIN [system].[AlertSeverities] asv ON a.[Severity] = asv.[Id]
LEFT JOIN [job].[Jobs] j ON a.[JobId] = j.[JobId]
LEFT JOIN [job].[Servers] s ON a.[ServerName] = s.[ServerName]
WHERE a.[IsActive] = 1;
GO

-- Unacknowledged Alerts View
CREATE OR ALTER VIEW [alert].[vw_UnacknowledgedAlerts] AS
SELECT 
    ai.[Id], ai.[AlertId], a.[Name] AS AlertName, ai.[TriggeredAt], ai.[Message],
    ai.[Severity], asv.[Name] AS SeverityName, asv.[Color] AS SeverityColor,
    ai.[JobId], j.[DisplayName] AS JobDisplayName, ai.[ServerName], ai.[Context],
    DATEDIFF(MINUTE, ai.[TriggeredAt], SYSUTCDATETIME()) AS MinutesSinceTriggered
FROM [alert].[AlertInstances] ai
INNER JOIN [alert].[Alerts] a ON ai.[AlertId] = a.[Id]
INNER JOIN [system].[AlertSeverities] asv ON ai.[Severity] = asv.[Id]
LEFT JOIN [job].[Jobs] j ON ai.[JobId] = j.[JobId]
WHERE ai.[Status] = 0;
GO

-- Dashboard Summary View
CREATE OR ALTER VIEW [system].[vw_DashboardSummary] AS
SELECT 
    (SELECT COUNT(*) FROM [log].[LogEntries] WHERE [Timestamp] >= DATEADD(HOUR, -24, SYSUTCDATETIME())) AS LogsLast24Hours,
    (SELECT COUNT(*) FROM [log].[LogEntries] WHERE [Timestamp] >= DATEADD(HOUR, -24, SYSUTCDATETIME()) AND [Level] >= 4) AS ErrorsLast24Hours,
    (SELECT COUNT(*) FROM [log].[LogEntries] WHERE [Timestamp] >= DATEADD(HOUR, -24, SYSUTCDATETIME()) AND [Level] = 3) AS WarningsLast24Hours,
    (SELECT COUNT(*) FROM [job].[Jobs] WHERE [IsActive] = 1) AS ActiveJobs,
    (SELECT COUNT(*) FROM [job].[JobExecutions] WHERE [Status] = 1) AS RunningJobs,
    (SELECT COUNT(*) FROM [job].[JobExecutions] WHERE [StartedAt] >= DATEADD(HOUR, -24, SYSUTCDATETIME()) AND [Status] = 3) AS FailedJobsLast24Hours,
    (SELECT COUNT(*) FROM [job].[JobExecutions] WHERE [StartedAt] >= DATEADD(HOUR, -24, SYSUTCDATETIME()) AND [Status] = 2) AS SuccessfulJobsLast24Hours,
    (SELECT COUNT(*) FROM [job].[Servers] WHERE [IsActive] = 1) AS TotalServers,
    (SELECT COUNT(*) FROM [job].[Servers] WHERE [IsActive] = 1 AND [LastHeartbeat] >= DATEADD(SECOND, -[HeartbeatIntervalSeconds] * 3, SYSUTCDATETIME())) AS OnlineServers,
    (SELECT COUNT(*) FROM [alert].[AlertInstances] WHERE [Status] = 0) AS UnacknowledgedAlerts,
    (SELECT COUNT(*) FROM [alert].[AlertInstances] WHERE [TriggeredAt] >= DATEADD(HOUR, -24, SYSUTCDATETIME())) AS AlertsLast24Hours;
GO

PRINT 'Views created.';
GO

-- ============================================================================
-- SECTION 5: Stored Procedures - Log Operations
-- ============================================================================
PRINT 'Creating log operation stored procedures...';

-- Search Logs
CREATE OR ALTER PROCEDURE [log].[usp_SearchLogs]
    @StartDate DATETIME2(3) = NULL, @EndDate DATETIME2(3) = NULL,
    @JobId NVARCHAR(100) = NULL, @JobExecutionId UNIQUEIDENTIFIER = NULL,
    @ServerName NVARCHAR(100) = NULL, @MinLevel INT = NULL, @MaxLevel INT = NULL,
    @SearchText NVARCHAR(500) = NULL, @ExceptionType NVARCHAR(500) = NULL,
    @CorrelationId NVARCHAR(100) = NULL, @HasException BIT = NULL,
    @Tags NVARCHAR(500) = NULL, @PageNumber INT = 1, @PageSize INT = 50,
    @SortColumn NVARCHAR(50) = 'Timestamp', @SortDirection NVARCHAR(4) = 'DESC',
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    SET @StartDate = ISNULL(@StartDate, DATEADD(HOUR, -24, SYSUTCDATETIME()));
    SET @EndDate = ISNULL(@EndDate, SYSUTCDATETIME());
    SET @PageNumber = CASE WHEN @PageNumber < 1 THEN 1 ELSE @PageNumber END;
    SET @PageSize = CASE WHEN @PageSize < 1 THEN 50 WHEN @PageSize > 1000 THEN 1000 ELSE @PageSize END;
    
    SELECT @TotalCount = COUNT(*)
    FROM [log].[LogEntries] le
    WHERE le.[Timestamp] >= @StartDate AND le.[Timestamp] < @EndDate
      AND (@JobId IS NULL OR le.[JobId] = @JobId)
      AND (@JobExecutionId IS NULL OR le.[JobExecutionId] = @JobExecutionId)
      AND (@ServerName IS NULL OR le.[ServerName] = @ServerName)
      AND (@MinLevel IS NULL OR le.[Level] >= @MinLevel)
      AND (@MaxLevel IS NULL OR le.[Level] <= @MaxLevel)
      AND (@SearchText IS NULL OR le.[Message] LIKE '%' + @SearchText + '%')
      AND (@ExceptionType IS NULL OR le.[ExceptionType] LIKE '%' + @ExceptionType + '%')
      AND (@CorrelationId IS NULL OR le.[CorrelationId] = @CorrelationId)
      AND (@HasException IS NULL OR (@HasException = 1 AND le.[ExceptionType] IS NOT NULL) OR (@HasException = 0 AND le.[ExceptionType] IS NULL))
      AND (@Tags IS NULL OR le.[Tags] LIKE '%' + @Tags + '%');
    
    SELECT le.[Id], le.[Timestamp], le.[Level], ll.[Name] AS LevelName, le.[Message],
           le.[MessageTemplate], le.[JobId], le.[JobExecutionId], le.[ServerName],
           le.[Category], le.[CorrelationId], le.[ExceptionType], le.[ExceptionMessage],
           le.[ExceptionStackTrace], le.[Properties], le.[Tags], le.[Environment], le.[ApplicationVersion]
    FROM [log].[LogEntries] le
    INNER JOIN [system].[LogLevels] ll ON le.[Level] = ll.[Id]
    WHERE le.[Timestamp] >= @StartDate AND le.[Timestamp] < @EndDate
      AND (@JobId IS NULL OR le.[JobId] = @JobId)
      AND (@JobExecutionId IS NULL OR le.[JobExecutionId] = @JobExecutionId)
      AND (@ServerName IS NULL OR le.[ServerName] = @ServerName)
      AND (@MinLevel IS NULL OR le.[Level] >= @MinLevel)
      AND (@MaxLevel IS NULL OR le.[Level] <= @MaxLevel)
      AND (@SearchText IS NULL OR le.[Message] LIKE '%' + @SearchText + '%')
      AND (@ExceptionType IS NULL OR le.[ExceptionType] LIKE '%' + @ExceptionType + '%')
      AND (@CorrelationId IS NULL OR le.[CorrelationId] = @CorrelationId)
      AND (@HasException IS NULL OR (@HasException = 1 AND le.[ExceptionType] IS NOT NULL) OR (@HasException = 0 AND le.[ExceptionType] IS NULL))
      AND (@Tags IS NULL OR le.[Tags] LIKE '%' + @Tags + '%')
    ORDER BY 
        CASE WHEN @SortDirection = 'ASC' AND @SortColumn = 'Timestamp' THEN le.[Timestamp] END ASC,
        CASE WHEN @SortDirection = 'DESC' AND @SortColumn = 'Timestamp' THEN le.[Timestamp] END DESC,
        CASE WHEN @SortDirection = 'ASC' AND @SortColumn = 'Level' THEN le.[Level] END ASC,
        CASE WHEN @SortDirection = 'DESC' AND @SortColumn = 'Level' THEN le.[Level] END DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

PRINT 'Log operation stored procedures created.';
GO

-- ============================================================================
-- SECTION 6: Stored Procedures - Job Operations
-- ============================================================================
PRINT 'Creating job operation stored procedures...';

-- Upsert Job
CREATE OR ALTER PROCEDURE [job].[usp_UpsertJob]
    @JobId NVARCHAR(100), @DisplayName NVARCHAR(200),
    @Description NVARCHAR(1000) = NULL, @Category NVARCHAR(100) = NULL,
    @Tags NVARCHAR(500) = NULL, @JobType INT = 0,
    @ServerName NVARCHAR(100) = NULL, @ExecutablePath NVARCHAR(500) = NULL,
    @Schedule NVARCHAR(200) = NULL, @ScheduleDescription NVARCHAR(500) = NULL,
    @IsCritical BIT = 0, @Configuration NVARCHAR(MAX) = NULL,
    @ExpectedDurationMs BIGINT = NULL, @MaxDurationMs BIGINT = NULL,
    @UpdatedBy NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS (SELECT 1 FROM [job].[Jobs] WHERE [JobId] = @JobId)
        UPDATE [job].[Jobs] SET 
            [DisplayName] = @DisplayName, [Description] = @Description, [Category] = @Category,
            [Tags] = @Tags, [JobType] = @JobType, [ServerName] = @ServerName,
            [ExecutablePath] = @ExecutablePath, [Schedule] = @Schedule,
            [ScheduleDescription] = @ScheduleDescription, [IsCritical] = @IsCritical,
            [Configuration] = @Configuration, [ExpectedDurationMs] = @ExpectedDurationMs,
            [MaxDurationMs] = @MaxDurationMs, [UpdatedAt] = SYSUTCDATETIME(), [UpdatedBy] = @UpdatedBy
        WHERE [JobId] = @JobId;
    ELSE
        INSERT INTO [job].[Jobs] ([JobId], [DisplayName], [Description], [Category], [Tags],
            [JobType], [ServerName], [ExecutablePath], [Schedule], [ScheduleDescription],
            [IsCritical], [Configuration], [ExpectedDurationMs], [MaxDurationMs], [CreatedBy], [UpdatedBy])
        VALUES (@JobId, @DisplayName, @Description, @Category, @Tags, @JobType, @ServerName,
            @ExecutablePath, @Schedule, @ScheduleDescription, @IsCritical, @Configuration,
            @ExpectedDurationMs, @MaxDurationMs, @UpdatedBy, @UpdatedBy);
    
    SELECT * FROM [job].[vw_JobOverview] WHERE [JobId] = @JobId;
END
GO

-- Start Job Execution
CREATE OR ALTER PROCEDURE [job].[usp_StartJobExecution]
    @JobId NVARCHAR(100), @ServerName NVARCHAR(100),
    @TriggerType NVARCHAR(50) = 'Manual', @TriggeredBy NVARCHAR(100) = NULL,
    @CorrelationId NVARCHAR(100) = NULL, @Parameters NVARCHAR(MAX) = NULL,
    @ExecutionId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    SET @ExecutionId = NEWSEQUENTIALID();
    SET @CorrelationId = ISNULL(@CorrelationId, LEFT(REPLACE(CAST(NEWID() AS NVARCHAR(50)), '-', ''), 12));
    
    IF NOT EXISTS (SELECT 1 FROM [job].[Jobs] WHERE [JobId] = @JobId)
        INSERT INTO [job].[Jobs] ([JobId], [DisplayName], [ServerName]) VALUES (@JobId, @JobId, @ServerName);
    
    IF NOT EXISTS (SELECT 1 FROM [job].[Servers] WHERE [ServerName] = @ServerName)
        INSERT INTO [job].[Servers] ([ServerName], [DisplayName], [Status]) VALUES (@ServerName, @ServerName, 1);
    
    INSERT INTO [job].[JobExecutions] ([Id], [JobId], [StartedAt], [Status], [ServerName],
        [TriggerType], [TriggeredBy], [CorrelationId], [Parameters])
    VALUES (@ExecutionId, @JobId, SYSUTCDATETIME(), 1, @ServerName, @TriggerType, @TriggeredBy, @CorrelationId, @Parameters);
    
    UPDATE [job].[Jobs] SET 
        [LastExecutionId] = @ExecutionId, [LastExecutionAt] = SYSUTCDATETIME(),
        [LastStatus] = 1, [TotalExecutions] = [TotalExecutions] + 1, [UpdatedAt] = SYSUTCDATETIME()
    WHERE [JobId] = @JobId;
    
    SELECT @ExecutionId AS ExecutionId, @CorrelationId AS CorrelationId, @JobId AS JobId, @ServerName AS ServerName;
END
GO

-- Complete Job Execution
CREATE OR ALTER PROCEDURE [job].[usp_CompleteJobExecution]
    @ExecutionId UNIQUEIDENTIFIER, @Status INT,
    @ResultSummary NVARCHAR(MAX) = NULL, @ResultCode INT = NULL,
    @ErrorMessage NVARCHAR(MAX) = NULL, @ErrorCategory NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @JobId NVARCHAR(100), @StartedAt DATETIME2(3), @DurationMs BIGINT;
    SELECT @JobId = [JobId], @StartedAt = [StartedAt] FROM [job].[JobExecutions] WHERE [Id] = @ExecutionId;
    
    IF @JobId IS NULL BEGIN RAISERROR('Execution not found: %s', 16, 1, @ExecutionId); RETURN; END
    
    SET @DurationMs = DATEDIFF(MILLISECOND, @StartedAt, SYSUTCDATETIME());
    
    UPDATE [job].[JobExecutions] SET 
        [CompletedAt] = SYSUTCDATETIME(), [Status] = @Status, [ResultSummary] = @ResultSummary,
        [ResultCode] = @ResultCode, [ErrorMessage] = @ErrorMessage, [ErrorCategory] = @ErrorCategory
    WHERE [Id] = @ExecutionId;
    
    UPDATE [job].[Jobs] SET 
        [LastStatus] = @Status, [LastDurationMs] = @DurationMs,
        [SuccessCount] = CASE WHEN @Status = 2 THEN [SuccessCount] + 1 ELSE [SuccessCount] END,
        [FailureCount] = CASE WHEN @Status IN (3, 5) THEN [FailureCount] + 1 ELSE [FailureCount] END,
        [AvgDurationMs] = (ISNULL([AvgDurationMs], 0) * ([TotalExecutions] - 1) + @DurationMs) / [TotalExecutions],
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [JobId] = @JobId;
    
    SELECT @ExecutionId AS ExecutionId, @JobId AS JobId, @Status AS Status, @DurationMs AS DurationMs,
           [system].[fn_FormatDuration](@DurationMs) AS DurationFormatted;
END
GO

-- Update Server Heartbeat
CREATE OR ALTER PROCEDURE [job].[usp_UpdateServerHeartbeat]
    @ServerName NVARCHAR(100), @IpAddress NVARCHAR(45) = NULL,
    @AgentVersion NVARCHAR(20) = NULL, @AgentType NVARCHAR(50) = NULL,
    @Metadata NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS (SELECT 1 FROM [job].[Servers] WHERE [ServerName] = @ServerName)
        UPDATE [job].[Servers] SET 
            [LastHeartbeat] = SYSUTCDATETIME(), [Status] = 1,
            [IpAddress] = ISNULL(@IpAddress, [IpAddress]),
            [AgentVersion] = ISNULL(@AgentVersion, [AgentVersion]),
            [AgentType] = ISNULL(@AgentType, [AgentType]),
            [Metadata] = ISNULL(@Metadata, [Metadata]),
            [UpdatedAt] = SYSUTCDATETIME()
        WHERE [ServerName] = @ServerName;
    ELSE
        INSERT INTO [job].[Servers] ([ServerName], [DisplayName], [IpAddress], [Status], [LastHeartbeat], [AgentVersion], [AgentType], [Metadata])
        VALUES (@ServerName, @ServerName, @IpAddress, 1, SYSUTCDATETIME(), @AgentVersion, @AgentType, @Metadata);
END
GO

PRINT 'Job operation stored procedures created.';
GO

-- ============================================================================
-- SECTION 7: Stored Procedures - Alert Operations
-- ============================================================================
PRINT 'Creating alert operation stored procedures...';

-- Acknowledge Alert
CREATE OR ALTER PROCEDURE [alert].[usp_AcknowledgeAlert]
    @InstanceId UNIQUEIDENTIFIER, @AcknowledgedBy NVARCHAR(100), @Note NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [alert].[AlertInstances] SET 
        [Status] = 1, [AcknowledgedAt] = SYSUTCDATETIME(),
        [AcknowledgedBy] = @AcknowledgedBy, [AcknowledgeNote] = @Note
    WHERE [Id] = @InstanceId AND [Status] = 0;
    SELECT @@ROWCOUNT AS AffectedRows;
END
GO

-- Resolve Alert
CREATE OR ALTER PROCEDURE [alert].[usp_ResolveAlert]
    @InstanceId UNIQUEIDENTIFIER, @ResolvedBy NVARCHAR(100), @Note NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [alert].[AlertInstances] SET 
        [Status] = 2, [ResolvedAt] = SYSUTCDATETIME(),
        [ResolvedBy] = @ResolvedBy, [ResolutionNote] = @Note
    WHERE [Id] = @InstanceId AND [Status] IN (0, 1);
    SELECT @@ROWCOUNT AS AffectedRows;
END
GO

PRINT 'Alert operation stored procedures created.';
GO

-- ============================================================================
-- SECTION 8: Stored Procedures - Maintenance
-- ============================================================================
PRINT 'Creating maintenance stored procedures...';

-- Apply Retention Policy
CREATE OR ALTER PROCEDURE [log].[usp_ApplyRetentionPolicy]
    @DryRun BIT = 0, @BatchSize INT = 10000, @DeletedCount BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @DefaultRetentionDays INT = [system].[fn_GetConfigInt]('Retention.DefaultDays', 90);
    DECLARE @ErrorRetentionDays INT = [system].[fn_GetConfigInt]('Retention.ErrorDays', 180);
    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @TraceDebugCutoff DATETIME2(3) = DATEADD(DAY, -7, @Now);
    DECLARE @InfoCutoff DATETIME2(3) = DATEADD(DAY, -@DefaultRetentionDays, @Now);
    DECLARE @WarningCutoff DATETIME2(3) = DATEADD(DAY, -@ErrorRetentionDays, @Now);
    
    SET @DeletedCount = 0;
    
    IF @DryRun = 1 BEGIN
        SELECT 'Trace/Debug (>7 days)' AS Category, COUNT(*) AS RecordsToDelete
        FROM [log].[LogEntries] WHERE [Level] <= 1 AND [Timestamp] < @TraceDebugCutoff
        UNION ALL SELECT 'Information (>' + CAST(@DefaultRetentionDays AS VARCHAR) + ' days)', COUNT(*)
        FROM [log].[LogEntries] WHERE [Level] = 2 AND [Timestamp] < @InfoCutoff
        UNION ALL SELECT 'Warning/Error (>' + CAST(@ErrorRetentionDays AS VARCHAR) + ' days)', COUNT(*)
        FROM [log].[LogEntries] WHERE [Level] IN (3, 4) AND [Timestamp] < @WarningCutoff;
        RETURN;
    END
    
    DECLARE @RowsDeleted INT = 1;
    WHILE @RowsDeleted > 0 BEGIN
        DELETE TOP (@BatchSize) FROM [log].[LogEntries] WHERE [Level] <= 1 AND [Timestamp] < @TraceDebugCutoff;
        SET @RowsDeleted = @@ROWCOUNT; SET @DeletedCount = @DeletedCount + @RowsDeleted;
        IF @RowsDeleted > 0 WAITFOR DELAY '00:00:00.100';
    END
    
    SET @RowsDeleted = 1;
    WHILE @RowsDeleted > 0 BEGIN
        DELETE TOP (@BatchSize) FROM [log].[LogEntries] WHERE [Level] = 2 AND [Timestamp] < @InfoCutoff;
        SET @RowsDeleted = @@ROWCOUNT; SET @DeletedCount = @DeletedCount + @RowsDeleted;
        IF @RowsDeleted > 0 WAITFOR DELAY '00:00:00.100';
    END
    
    SET @RowsDeleted = 1;
    WHILE @RowsDeleted > 0 BEGIN
        DELETE TOP (@BatchSize) FROM [log].[LogEntries] WHERE [Level] IN (3, 4) AND [Timestamp] < @WarningCutoff;
        SET @RowsDeleted = @@ROWCOUNT; SET @DeletedCount = @DeletedCount + @RowsDeleted;
        IF @RowsDeleted > 0 WAITFOR DELAY '00:00:00.100';
    END
    
    INSERT INTO [system].[AuditLog] ([Action], [EntityType], [NewValues])
    VALUES ('RetentionCleanup', 'LogEntries', (SELECT @DeletedCount AS deletedCount, @Now AS executedAt FOR JSON PATH, WITHOUT_ARRAY_WRAPPER));
END
GO

-- Update Dashboard Cache
CREATE OR ALTER PROCEDURE [system].[usp_UpdateDashboardCache]
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @CacheTtl INT = [system].[fn_GetConfigInt]('Dashboard.StatsCacheTtlSeconds', 30);
    DECLARE @ExpiresAt DATETIME2(3) = DATEADD(SECOND, @CacheTtl, @Now);
    DECLARE @SummaryJson NVARCHAR(MAX), @TrendJson NVARCHAR(MAX), @ExceptionsJson NVARCHAR(MAX);
    
    SELECT @SummaryJson = (SELECT * FROM [system].[vw_DashboardSummary] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    
    MERGE [system].[DashboardCache] AS t USING (SELECT 'DashboardSummary' AS [Key]) AS s ON t.[Key] = s.[Key]
    WHEN MATCHED THEN UPDATE SET [Value] = @SummaryJson, [ComputedAt] = @Now, [ExpiresAt] = @ExpiresAt
    WHEN NOT MATCHED THEN INSERT ([Key], [Value], [ComputedAt], [ExpiresAt]) VALUES ('DashboardSummary', @SummaryJson, @Now, @ExpiresAt);
    
    SELECT @TrendJson = (SELECT * FROM [log].[fn_GetHourlyLogTrend](DATEADD(HOUR, -24, @Now), @Now, NULL) ORDER BY HourBucket FOR JSON PATH);
    
    MERGE [system].[DashboardCache] AS t USING (SELECT 'HourlyTrend24h' AS [Key]) AS s ON t.[Key] = s.[Key]
    WHEN MATCHED THEN UPDATE SET [Value] = ISNULL(@TrendJson, '[]'), [ComputedAt] = @Now, [ExpiresAt] = @ExpiresAt
    WHEN NOT MATCHED THEN INSERT ([Key], [Value], [ComputedAt], [ExpiresAt]) VALUES ('HourlyTrend24h', ISNULL(@TrendJson, '[]'), @Now, @ExpiresAt);
    
    SELECT @ExceptionsJson = (SELECT * FROM [log].[fn_GetTopExceptions](DATEADD(HOUR, -24, @Now), @Now, 10) FOR JSON PATH);
    
    MERGE [system].[DashboardCache] AS t USING (SELECT 'TopExceptions24h' AS [Key]) AS s ON t.[Key] = s.[Key]
    WHEN MATCHED THEN UPDATE SET [Value] = ISNULL(@ExceptionsJson, '[]'), [ComputedAt] = @Now, [ExpiresAt] = @ExpiresAt
    WHEN NOT MATCHED THEN INSERT ([Key], [Value], [ComputedAt], [ExpiresAt]) VALUES ('TopExceptions24h', ISNULL(@ExceptionsJson, '[]'), @Now, @ExpiresAt);
END
GO

-- Update Server Statuses
CREATE OR ALTER PROCEDURE [system].[usp_UpdateServerStatuses]
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @TimeoutSeconds INT = [system].[fn_GetConfigInt]('Server.HeartbeatTimeoutSeconds', 180);
    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();
    
    UPDATE [job].[Servers] SET [Status] = 2, [UpdatedAt] = @Now
    WHERE [IsActive] = 1 AND [Status] <> 2 AND [LastHeartbeat] < DATEADD(SECOND, -@TimeoutSeconds, @Now);
    
    UPDATE [job].[Servers] SET [Status] = 3, [UpdatedAt] = @Now
    WHERE [IsActive] = 1 AND [Status] = 1
      AND [LastHeartbeat] < DATEADD(SECOND, -(@TimeoutSeconds / 2), @Now)
      AND [LastHeartbeat] >= DATEADD(SECOND, -@TimeoutSeconds, @Now);
END
GO

-- Daily Maintenance
CREATE OR ALTER PROCEDURE [system].[usp_RunDailyMaintenance]
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @StartTime DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @DeletedLogs BIGINT, @DeletedAlerts INT, @DeletedAudit INT, @DeletedTokens INT;
    
    BEGIN TRY
        EXEC [log].[usp_ApplyRetentionPolicy] @DryRun = 0, @DeletedCount = @DeletedLogs OUTPUT;
        
        DELETE FROM [alert].[AlertInstances] WHERE [Status] = 2 AND [ResolvedAt] < DATEADD(DAY, -90, SYSUTCDATETIME());
        SET @DeletedAlerts = @@ROWCOUNT;
        
        DELETE FROM [system].[AuditLog] WHERE [Timestamp] < DATEADD(DAY, -180, SYSUTCDATETIME());
        SET @DeletedAudit = @@ROWCOUNT;
        
        DELETE FROM [auth].[RefreshTokens] WHERE [ExpiresAt] < SYSUTCDATETIME() OR [RevokedAt] < DATEADD(DAY, -30, SYSUTCDATETIME());
        SET @DeletedTokens = @@ROWCOUNT;
        
        EXEC [system].[usp_UpdateDashboardCache];
        EXEC [system].[usp_UpdateServerStatuses];
        
        INSERT INTO [system].[AuditLog] ([Action], [EntityType], [NewValues])
        VALUES ('DailyMaintenance', 'System', (
            SELECT @DeletedLogs AS deletedLogs, @DeletedAlerts AS deletedAlerts,
                   @DeletedAudit AS deletedAuditRecords, @DeletedTokens AS deletedTokens,
                   DATEDIFF(SECOND, @StartTime, SYSUTCDATETIME()) AS durationSeconds
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER));
    END TRY
    BEGIN CATCH
        INSERT INTO [system].[AuditLog] ([Action], [EntityType], [NewValues])
        VALUES ('DailyMaintenanceError', 'System', (
            SELECT ERROR_MESSAGE() AS error, ERROR_NUMBER() AS errorNumber,
                   DATEDIFF(SECOND, @StartTime, SYSUTCDATETIME()) AS durationSeconds
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER));
        THROW;
    END CATCH
END
GO

PRINT 'Maintenance stored procedures created.';
GO

-- ============================================================================
-- SECTION 9: Update Schema Version
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM [system].[SchemaVersion] WHERE [Version] = 2)
BEGIN
    INSERT INTO [system].[SchemaVersion] ([Version], [Description], [AppliedBy], [Script])
    VALUES (2, 'Supporting objects (views, procedures, functions)', SYSTEM_USER, '002_SupportingObjects.sql');
END
GO

-- ============================================================================
-- COMPLETION
-- ============================================================================
PRINT '============================================================';
PRINT 'FMS Log Nexus - Supporting Objects Creation Complete';
PRINT 'Completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================================';
GO
