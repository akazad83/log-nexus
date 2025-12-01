using Microsoft.Extensions.Logging;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Repositories;
using FMSLogNexus.Core.Interfaces.Services;
using System.Text.Json;
using DtoExecutionStatistics = FMSLogNexus.Core.DTOs.Responses.ExecutionStatistics;
using DtoExecutionTrendPoint = FMSLogNexus.Core.DTOs.Responses.ExecutionTrendPoint;
using ExecutionListItem = FMSLogNexus.Core.DTOs.Responses.ExecutionListItem;

namespace FMSLogNexus.Infrastructure.Services;

/// <summary>
/// Service implementation for job execution operations.
/// </summary>
public class JobExecutionService : ServiceBase, IJobExecutionService
{
    private readonly IRealTimeService? _realTimeService;
    private readonly IAlertService? _alertService;

    public JobExecutionService(
        IUnitOfWork unitOfWork,
        ILogger<JobExecutionService> logger,
        IRealTimeService? realTimeService = null,
        IAlertService? alertService = null)
        : base(unitOfWork, logger)
    {
        _realTimeService = realTimeService;
        _alertService = alertService;
    }

    #region Lifecycle

    /// <inheritdoc />
    public async Task<JobExecutionResponse> StartExecutionAsync(
        StartExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Verify job exists
        var job = await UnitOfWork.Jobs.GetByIdAsync(request.JobId, cancellationToken);
        if (job == null)
        {
            throw new KeyNotFoundException($"Job '{request.JobId}' not found.");
        }

        // Check if job is already running (prevent duplicate executions by default)
        var isRunning = await UnitOfWork.JobExecutions.IsJobRunningAsync(request.JobId, cancellationToken);
        if (isRunning)
        {
            throw new InvalidOperationException($"Job '{request.JobId}' is already running.");
        }

        var execution = new JobExecution
        {
            JobId = request.JobId,
            ServerName = request.ServerName ?? job.ServerName ?? "Unknown",
            TriggerType = request.TriggerType ?? "Manual",
            TriggeredBy = request.TriggeredBy,
            CorrelationId = request.CorrelationId ?? Guid.NewGuid().ToString("N"),
            Parameters = request.Parameters != null ? JsonSerializer.Serialize(request.Parameters) : null,
            Status = JobStatus.Running,
            StartedAt = DateTime.UtcNow
        };

        await UnitOfWork.JobExecutions.StartAsync(execution, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Started execution {ExecutionId} for job {JobId}", execution.Id, request.JobId);

        // Notify real-time clients
        if (_realTimeService != null)
        {
            var executionItem = new ExecutionListItem
            {
                Id = execution.Id,
                JobId = execution.JobId,
                JobDisplayName = job.DisplayName,
                StartedAt = execution.StartedAt,
                Status = execution.Status,
                ServerName = execution.ServerName,
                TriggerType = execution.TriggerType
            };
            await _realTimeService.BroadcastExecutionStartedAsync(executionItem, cancellationToken);
        }

        return MapToResponse(execution);
    }

    /// <inheritdoc />
    public async Task<JobExecutionResponse?> CompleteExecutionAsync(
        Guid executionId,
        CompleteExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var execution = await UnitOfWork.JobExecutions.CompleteAsync(
            executionId,
            request.Status,
            request.ResultSummary,
            request.ResultCode,
            request.ErrorMessage,
            request.ErrorCategory,
            cancellationToken);

        if (execution == null)
            return null;

        // Update job statistics
        await UnitOfWork.Jobs.UpdateStatisticsAsync(execution.JobId, execution, cancellationToken);

        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation(
            "Completed execution {ExecutionId} for job {JobId} with status {Status}",
            executionId, execution.JobId, request.Status);

        // Notify real-time clients
        if (_realTimeService != null)
        {
            var executionItem = new ExecutionListItem
            {
                Id = execution.Id,
                JobId = execution.JobId,
                JobDisplayName = execution.Job?.DisplayName,
                StartedAt = execution.StartedAt,
                CompletedAt = execution.CompletedAt,
                Status = execution.Status,
                DurationMs = execution.DurationMs,
                ServerName = execution.ServerName,
                TriggerType = execution.TriggerType,
                ErrorCount = execution.ErrorCount,
                WarningCount = execution.WarningCount
            };
            await _realTimeService.BroadcastExecutionCompletedAsync(executionItem, cancellationToken);
        }

        // TODO: Check for alerts on failure - need to implement alert evaluation
        // The IAlertService.EvaluateJobFailureAsync method doesn't exist yet
        // May need to use IAlertService.EvaluateAlertsAsync or implement specific alert logic
        if (_alertService != null && (request.Status == JobStatus.Failed || request.Status == JobStatus.Timeout))
        {
            // For now, trigger the general alert evaluation
            await _alertService.EvaluateAlertsAsync(cancellationToken);
        }

        return MapToResponse(execution);
    }

    /// <inheritdoc />
    public async Task<JobExecutionResponse?> CancelExecutionAsync(
        Guid executionId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var execution = await UnitOfWork.JobExecutions.CompleteAsync(
            executionId,
            JobStatus.Cancelled,
            null,
            null,
            reason ?? "Cancelled by user",
            "UserCancellation",
            cancellationToken);

        if (execution == null)
            return null;

        // Update job statistics
        await UnitOfWork.Jobs.UpdateStatisticsAsync(execution.JobId, execution, cancellationToken);

        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Cancelled execution {ExecutionId} for job {JobId}", executionId, execution.JobId);

        return MapToResponse(execution);
    }

    #endregion

    #region Query

    /// <inheritdoc />
    public async Task<JobExecutionResponse?> GetExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        var execution = await UnitOfWork.JobExecutions.GetByIdAsync(executionId, cancellationToken);
        return execution == null ? null : MapToResponse(execution);
    }

    /// <inheritdoc />
    public async Task<JobExecutionDetailResponse?> GetExecutionDetailAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        var execution = await UnitOfWork.JobExecutions.GetWithJobAsync(executionId, cancellationToken);
        if (execution == null)
            return null;

        return new JobExecutionDetailResponse
        {
            Execution = MapToResponse(execution),
            JobDisplayName = execution.Job?.DisplayName,
            JobCategory = execution.Job?.Category,
            Parameters = !string.IsNullOrEmpty(execution.Parameters)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(execution.Parameters)
                : null,
            ResultSummary = !string.IsNullOrEmpty(execution.ResultSummary)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(execution.ResultSummary)
                : null,
            LogCountByLevel = new Dictionary<string, int>
            {
                ["Trace"] = execution.TraceCount,
                ["Debug"] = execution.DebugCount,
                ["Info"] = execution.InfoCount,
                ["Warning"] = execution.WarningCount,
                ["Error"] = execution.ErrorCount,
                ["Critical"] = execution.CriticalCount
            }
        };
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<JobExecutionResponse>> SearchAsync(
        ExecutionSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.JobExecutions.SearchAsync(request, cancellationToken);

        return PaginatedResult<JobExecutionResponse>.Create(
            result.Items.Select(MapToResponse),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<JobExecutionResponse>> GetByJobAsync(
        string jobId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.JobExecutions.GetByJobAsync(
            jobId, startDate, endDate, page, pageSize, cancellationToken);

        return PaginatedResult<JobExecutionResponse>.Create(
            result.Items.Select(MapToResponse),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobExecutionResponse>> GetRecentByJobAsync(
        string jobId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var executions = await UnitOfWork.JobExecutions.GetRecentByJobAsync(jobId, count, cancellationToken);
        return executions.Select(MapToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<JobExecutionResponse?> GetLastByJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        var execution = await UnitOfWork.JobExecutions.GetLastByJobAsync(jobId, cancellationToken);
        return execution == null ? null : MapToResponse(execution);
    }

    #endregion

    #region Running Executions

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobExecutionResponse>> GetRunningAsync(
        CancellationToken cancellationToken = default)
    {
        var executions = await UnitOfWork.JobExecutions.GetRunningAsync(cancellationToken);
        return executions.Select(MapToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobExecutionResponse>> GetRunningByServerAsync(
        string serverName,
        CancellationToken cancellationToken = default)
    {
        var executions = await UnitOfWork.JobExecutions.GetRunningByServerAsync(serverName, cancellationToken);
        return executions.Select(MapToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> IsJobRunningAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.JobExecutions.IsJobRunningAsync(jobId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobExecutionResponse>> GetOverdueAsync(
        CancellationToken cancellationToken = default)
    {
        var executions = await UnitOfWork.JobExecutions.GetOverdueAsync(cancellationToken);
        return executions.Select(MapToResponse).ToList();
    }

    #endregion

    #region Failures

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobExecutionResponse>> GetRecentFailuresAsync(
        int count = 20,
        string? serverName = null,
        CancellationToken cancellationToken = default)
    {
        var executions = await UnitOfWork.JobExecutions.GetRecentFailuresAsync(count, serverName, cancellationToken);
        return executions.Select(MapToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<int> GetConsecutiveFailureCountAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.JobExecutions.GetConsecutiveFailureCountAsync(jobId, cancellationToken);
    }

    #endregion

    #region Statistics

    /// <inheritdoc />
    public async Task<DtoExecutionStatistics> GetStatisticsAsync(
        string jobId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var stats = await UnitOfWork.JobExecutions.GetStatisticsAsync(jobId, startDate, endDate, cancellationToken);
        return new DtoExecutionStatistics
        {
            JobId = jobId,
            StartDate = startDate,
            EndDate = endDate,
            TotalCount = stats.TotalExecutions,
            SuccessCount = stats.SuccessCount,
            FailedCount = stats.FailureCount,
            CancelledCount = stats.CancelledCount,
            TimeoutCount = stats.TimeoutCount,
            AvgDurationMs = stats.AvgDurationMs,
            MinDurationMs = stats.MinDurationMs,
            MaxDurationMs = stats.MaxDurationMs
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DtoExecutionTrendPoint>> GetTrendAsync(
        DateTime startDate,
        DateTime endDate,
        TrendInterval interval,
        string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        var trendData = await UnitOfWork.JobExecutions.GetTrendDataAsync(
            startDate, endDate, interval, jobId, cancellationToken);
        return trendData.Select(t => new DtoExecutionTrendPoint
        {
            Timestamp = t.Timestamp,
            Label = t.Timestamp.ToString("yyyy-MM-dd HH:mm"),
            TotalCount = t.TotalCount,
            SuccessCount = t.SuccessCount,
            FailedCount = t.FailureCount,
            AvgDurationMs = t.AvgDurationMs
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<Dictionary<JobStatus, int>> GetCountsByStatusAsync(
        DateTime startDate,
        DateTime endDate,
        string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.JobExecutions.GetCountsByStatusAsync(
            startDate, endDate, jobId, cancellationToken);
    }

    #endregion

    #region Maintenance

    /// <inheritdoc />
    public async Task<int> TimeoutStuckExecutionsAsync(
        CancellationToken cancellationToken = default)
    {
        var count = await UnitOfWork.JobExecutions.TimeoutStuckExecutionsAsync(cancellationToken);
        if (count > 0)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogWarning("Timed out {Count} stuck executions", count);
        }
        return count;
    }

    #endregion

    #region Private Methods

    private static JobExecutionResponse MapToResponse(JobExecution execution)
    {
        return new JobExecutionResponse
        {
            Id = execution.Id,
            JobId = execution.JobId,
            JobDisplayName = execution.Job?.DisplayName,
            StartedAt = execution.StartedAt,
            CompletedAt = execution.CompletedAt,
            Status = execution.Status,
            DurationMs = execution.DurationMs,
            ServerName = execution.ServerName,
            TriggerType = execution.TriggerType,
            TriggeredBy = execution.TriggeredBy,
            CorrelationId = execution.CorrelationId,
            ErrorMessage = execution.ErrorMessage,
            ErrorCategory = execution.ErrorCategory,
            ResultCode = execution.ResultCode,
            LogCount = execution.LogCount,
            TraceCount = execution.TraceCount,
            DebugCount = execution.DebugCount,
            InfoCount = execution.InfoCount,
            WarningCount = execution.WarningCount,
            ErrorCount = execution.ErrorCount,
            CriticalCount = execution.CriticalCount
        };
    }

    #endregion
}
