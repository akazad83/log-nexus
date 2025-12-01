using Microsoft.Extensions.Logging;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Repositories;
using FMSLogNexus.Core.Interfaces.Services;
using System.Text.Json;

namespace FMSLogNexus.Infrastructure.Services;

/// <summary>
/// Service implementation for job operations.
/// </summary>
public class JobService : ServiceBase, IJobService
{
    private readonly IRealTimeService? _realTimeService;

    public JobService(
        IUnitOfWork unitOfWork,
        ILogger<JobService> logger,
        IRealTimeService? realTimeService = null)
        : base(unitOfWork, logger)
    {
        _realTimeService = realTimeService;
    }

    #region Job Management

    /// <inheritdoc />
    public async Task<JobDetailResponse> CreateJobAsync(
        CreateJobRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await UnitOfWork.Jobs.GetByIdAsync(request.JobId, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"Job with ID '{request.JobId}' already exists.");
        }

        var job = new Job
        {
            Id = request.JobId,
            DisplayName = request.DisplayName,
            Description = request.Description,
            JobType = request.JobType,
            Category = request.Category,
            Tags = request.Tags != null ? string.Join(",", request.Tags) : null,
            ServerName = request.ServerName,
            ExecutablePath = request.ExecutablePath,
            Schedule = request.Schedule,
            ScheduleDescription = request.ScheduleDescription,
            ExpectedDurationMs = request.ExpectedDurationMs,
            MaxDurationMs = request.MaxDurationMs,
            IsCritical = request.IsCritical,
            IsActive = true,
            Configuration = request.Configuration != null ? JsonSerializer.Serialize(request.Configuration) : null
        };

        await UnitOfWork.Jobs.AddAsync(job, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Created job {JobId}", job.Id);

        return MapToDetailResponse(job);
    }

    /// <inheritdoc />
    public async Task<JobDetailResponse?> GetJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await UnitOfWork.Jobs.GetByIdAsync(jobId, cancellationToken);
        return job == null ? null : MapToDetailResponse(job);
    }

    /// <inheritdoc />
    public async Task<JobDetailResponse?> UpdateJobAsync(
        string jobId,
        UpdateJobRequest request,
        CancellationToken cancellationToken = default)
    {
        var job = await UnitOfWork.Jobs.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
            return null;

        if (request.DisplayName != null) job.DisplayName = request.DisplayName;
        if (request.Description != null) job.Description = request.Description;
        if (request.Category != null) job.Category = request.Category;
        if (request.Tags != null) job.Tags = string.Join(",", request.Tags);
        if (request.ServerName != null) job.ServerName = request.ServerName;
        if (request.ExecutablePath != null) job.ExecutablePath = request.ExecutablePath;
        if (request.Schedule != null) job.Schedule = request.Schedule;
        if (request.ScheduleDescription != null) job.ScheduleDescription = request.ScheduleDescription;
        if (request.ExpectedDurationMs.HasValue) job.ExpectedDurationMs = request.ExpectedDurationMs;
        if (request.MaxDurationMs.HasValue) job.MaxDurationMs = request.MaxDurationMs;
        if (request.IsCritical.HasValue) job.IsCritical = request.IsCritical.Value;
        if (request.Configuration != null) job.Configuration = JsonSerializer.Serialize(request.Configuration);

        job.UpdatedAt = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Updated job {JobId}", jobId);

        return MapToDetailResponse(job);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await UnitOfWork.Jobs.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
            return false;

        job.IsActive = false;
        job.UpdatedAt = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Deactivated job {JobId}", jobId);

        return true;
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<JobListItem>> SearchJobsAsync(
        JobSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.Jobs.SearchAsync(request, cancellationToken);

        return PaginatedResult<JobListItem>.Create(
            result.Items.Select(MapToListItem),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }

    /// <inheritdoc />
    public async Task<bool> ActivateJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.Jobs.ActivateAsync(jobId, cancellationToken);
        if (result)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Activated job {JobId}", jobId);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.Jobs.DeactivateAsync(jobId, cancellationToken);
        if (result)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Deactivated job {JobId}", jobId);
        }
        return result;
    }

    #endregion

    #region Execution Management

    /// <inheritdoc />
    public async Task<StartExecutionResult> StartExecutionAsync(
        string jobId,
        StartExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var job = await UnitOfWork.Jobs.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
        {
            throw new InvalidOperationException($"Job '{jobId}' not found.");
        }

        if (!job.IsActive)
        {
            throw new InvalidOperationException($"Job '{jobId}' is not active.");
        }

        var execution = new JobExecution
        {
            JobId = jobId,
            ServerName = request.ServerName ?? job.ServerName ?? "unknown",
            TriggerType = request.TriggerType ?? "manual",
            TriggeredBy = request.TriggeredBy,
            CorrelationId = request.CorrelationId ?? Guid.NewGuid().ToString(),
            Status = JobStatus.Running,
            StartedAt = DateTime.UtcNow
        };

        await UnitOfWork.JobExecutions.AddAsync(execution, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Started execution {ExecutionId} for job {JobId}", execution.Id, jobId);

        if (_realTimeService != null)
        {
            var executionListItem = MapToExecutionListItem(execution);
            await _realTimeService.BroadcastExecutionStartedAsync(executionListItem, cancellationToken);
        }

        return new StartExecutionResult
        {
            ExecutionId = execution.Id,
            CorrelationId = execution.CorrelationId,
            StartedAt = execution.StartedAt
        };
    }

    /// <inheritdoc />
    public async Task<ExecutionDetailResponse?> CompleteExecutionAsync(
        Guid executionId,
        CompleteExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var execution = await UnitOfWork.JobExecutions.GetByIdAsync(executionId, cancellationToken);
        if (execution == null)
            return null;

        execution.Status = request.Status;
        execution.CompletedAt = DateTime.UtcNow;
        execution.ErrorMessage = request.ErrorMessage;
        execution.ErrorCategory = request.ErrorCategory;
        execution.ResultCode = request.ResultCode;
        execution.ResultSummary = request.ResultSummary != null ? JsonSerializer.Serialize(request.ResultSummary) : null;

        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Completed execution {ExecutionId} with status {Status}", executionId, execution.Status);

        if (_realTimeService != null)
        {
            var executionListItem = MapToExecutionListItem(execution);
            await _realTimeService.BroadcastExecutionCompletedAsync(executionListItem, cancellationToken);
        }

        return MapToExecutionDetailResponse(execution);
    }

    /// <inheritdoc />
    public async Task<ExecutionDetailResponse?> GetExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        var execution = await UnitOfWork.JobExecutions.GetWithJobAsync(executionId, cancellationToken);
        return execution == null ? null : MapToExecutionDetailResponse(execution);
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<ExecutionListItem>> SearchExecutionsAsync(
        ExecutionSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.JobExecutions.SearchAsync(request, cancellationToken);

        return PaginatedResult<ExecutionListItem>.Create(
            result.Items.Select(MapToExecutionListItem),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<ExecutionListItem>> GetJobExecutionsAsync(
        string jobId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var request = new ExecutionSearchRequest
        {
            JobId = jobId,
            Page = page,
            PageSize = pageSize
        };

        return await SearchExecutionsAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RunningJobInfo>> GetRunningExecutionsAsync(
        CancellationToken cancellationToken = default)
    {
        var executions = await UnitOfWork.JobExecutions.GetRunningAsync(cancellationToken);

        return executions.Select(e => new RunningJobInfo
        {
            ExecutionId = e.Id,
            JobId = e.JobId,
            JobDisplayName = e.Job?.DisplayName ?? e.JobId,
            ServerName = e.ServerName,
            StartedAt = e.StartedAt,
            RunningDurationMs = (long)(DateTime.UtcNow - e.StartedAt).TotalMilliseconds,
            MaxDurationMs = e.Job?.MaxDurationMs,
            IsOverdue = e.Job?.MaxDurationMs.HasValue == true &&
                       (DateTime.UtcNow - e.StartedAt).TotalMilliseconds > e.Job.MaxDurationMs.Value
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecentFailure>> GetRecentFailuresAsync(
        int count = 20,
        string? serverName = null,
        CancellationToken cancellationToken = default)
    {
        var failures = await UnitOfWork.JobExecutions.GetRecentFailuresAsync(count, serverName, cancellationToken);

        return failures.Select(e => new RecentFailure
        {
            ExecutionId = e.Id,
            JobId = e.JobId,
            JobDisplayName = e.Job?.DisplayName ?? e.JobId,
            FailedAt = e.CompletedAt ?? e.StartedAt,
            ErrorMessage = e.ErrorMessage,
            ErrorCategory = e.ErrorCategory
        }).ToList();
    }

    #endregion

    #region Health and Statistics

    /// <inheritdoc />
    public async Task<JobHealthOverviewResponse> GetHealthOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        var stats = await UnitOfWork.Jobs.GetStatisticsSummaryAsync(cancellationToken);
        var unhealthyJobs = await UnitOfWork.Jobs.GetUnhealthyJobsAsync(10, cancellationToken);
        var recentFailures = await UnitOfWork.JobExecutions.GetRecentFailuresAsync(10, null, cancellationToken);

        return new JobHealthOverviewResponse
        {
            Summary = new JobHealthSummary
            {
                Total = stats.TotalJobs,
                Healthy = stats.HealthyJobs,
                Warning = stats.WarningJobs,
                Critical = stats.CriticalHealthJobs,
                Unknown = 0
            },
            Jobs = unhealthyJobs.Select(MapToListItem).ToList(),
            RecentFailures = recentFailures.Select(e => new RecentFailure
            {
                ExecutionId = e.Id,
                JobId = e.JobId,
                JobDisplayName = e.Job?.DisplayName ?? e.JobId,
                FailedAt = e.CompletedAt ?? e.StartedAt,
                ErrorMessage = e.ErrorMessage,
                ErrorCategory = e.ErrorCategory
            }).ToList()
        };
    }

    /// <inheritdoc />
    public async Task<int> CalculateHealthScoreAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.Jobs.CalculateHealthScoreAsync(jobId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<JobStatistics> GetJobStatisticsAsync(
        string jobId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var repoStats = await UnitOfWork.JobExecutions.GetStatisticsAsync(jobId, startDate, endDate, cancellationToken);

        // Map repository ExecutionStatistics to DTO JobStatistics
        return new JobStatistics
        {
            TotalExecutions = repoStats.TotalExecutions,
            SuccessCount = repoStats.SuccessCount,
            FailureCount = repoStats.FailureCount,
            SuccessRate = repoStats.SuccessRate,
            AvgDurationMs = repoStats.AvgDurationMs,
            AvgDurationFormatted = FormatDuration(repoStats.AvgDurationMs),
            LastExecutionAt = repoStats.LastExecution,
            LastStatus = null, // Not available in statistics
            HealthScore = CalculateHealthScoreFromSuccessRate(repoStats.SuccessRate)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobListItem>> GetUnhealthyJobsAsync(
        int top = 10,
        CancellationToken cancellationToken = default)
    {
        var jobs = await UnitOfWork.Jobs.GetUnhealthyJobsAsync(top, cancellationToken);
        return jobs.Select(MapToListItem).ToList();
    }

    #endregion

    #region Lookups

    /// <inheritdoc />
    public async Task<IReadOnlyList<LookupItem>> GetJobLookupAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.Jobs.GetLookupItemsAsync(activeOnly, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.Jobs.GetDistinctCategoriesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetTagsAsync(
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.Jobs.GetDistinctTagsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> JobExistsAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await UnitOfWork.Jobs.GetByIdAsync(jobId, cancellationToken);
        return job != null;
    }

    #endregion

    #region Private Methods

    private static JobDetailResponse MapToDetailResponse(Job job)
    {
        return new JobDetailResponse
        {
            JobId = job.Id,
            DisplayName = job.DisplayName,
            Description = job.Description,
            JobType = job.JobType,
            Category = job.Category,
            Tags = !string.IsNullOrEmpty(job.Tags)
                ? job.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                : new List<string>(),
            ServerName = job.ServerName,
            Schedule = job.Schedule,
            ScheduleDescription = job.ScheduleDescription,
            ExpectedDurationMs = job.ExpectedDurationMs,
            MaxDurationMs = job.MaxDurationMs,
            IsCritical = job.IsCritical,
            IsActive = job.IsActive,
            Statistics = new JobStatistics
            {
                TotalExecutions = job.TotalExecutions,
                SuccessCount = job.SuccessCount,
                FailureCount = job.FailureCount,
                SuccessRate = job.SuccessRate,
                AvgDurationMs = job.AvgDurationMs,
                LastExecutionAt = job.LastExecutionAt,
                LastStatus = job.LastStatus,
                HealthScore = CalculateHealthScoreFromSuccessRate(job.SuccessRate)
            },
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt
        };
    }

    private static JobListItem MapToListItem(Job job)
    {
        return new JobListItem
        {
            JobId = job.Id,
            DisplayName = job.DisplayName,
            Category = job.Category,
            JobType = job.JobType,
            LastStatus = job.LastStatus,
            LastExecutionAt = job.LastExecutionAt,
            HealthScore = CalculateHealthScoreFromSuccessRate(job.SuccessRate),
            IsActive = job.IsActive,
            IsCritical = job.IsCritical
        };
    }

    private static ExecutionDetailResponse MapToExecutionDetailResponse(JobExecution execution)
    {
        return new ExecutionDetailResponse
        {
            Id = execution.Id,
            JobId = execution.JobId,
            JobDisplayName = execution.Job?.DisplayName,
            ServerName = execution.ServerName,
            Status = execution.Status,
            StartedAt = execution.StartedAt,
            CompletedAt = execution.CompletedAt,
            DurationMs = execution.DurationMs,
            TriggerType = execution.TriggerType,
            TriggeredBy = execution.TriggeredBy,
            CorrelationId = execution.CorrelationId,
            ErrorMessage = execution.ErrorMessage,
            ErrorCategory = execution.ErrorCategory,
            ResultCode = execution.ResultCode,
            LogCounts = new LogCountSummary
            {
                Total = execution.LogCount,
                Trace = execution.TraceCount,
                Debug = execution.DebugCount,
                Info = execution.InfoCount,
                Warning = execution.WarningCount,
                Error = execution.ErrorCount,
                Critical = execution.CriticalCount
            },
            ErrorCount = execution.ErrorCount,
            WarningCount = execution.WarningCount
        };
    }

    private static ExecutionListItem MapToExecutionListItem(JobExecution execution)
    {
        return new ExecutionListItem
        {
            Id = execution.Id,
            JobId = execution.JobId,
            JobDisplayName = execution.Job?.DisplayName,
            ServerName = execution.ServerName,
            Status = execution.Status,
            StartedAt = execution.StartedAt,
            CompletedAt = execution.CompletedAt,
            DurationMs = execution.DurationMs,
            TriggerType = execution.TriggerType,
            ErrorCount = execution.ErrorCount,
            WarningCount = execution.WarningCount
        };
    }

    /// <summary>
    /// Calculates health score from success rate.
    /// </summary>
    /// <param name="successRate">Success rate (0-100).</param>
    /// <returns>Health score (0-100) or null if success rate is null.</returns>
    private static int? CalculateHealthScoreFromSuccessRate(decimal? successRate)
    {
        if (!successRate.HasValue)
            return null;

        // Convert success rate to health score (same scale: 0-100)
        return (int)Math.Round(successRate.Value);
    }

    /// <summary>
    /// Formats duration in milliseconds to human-readable string.
    /// </summary>
    /// <param name="milliseconds">Duration in milliseconds.</param>
    /// <returns>Formatted duration string.</returns>
    private static string FormatDuration(long milliseconds)
    {
        if (milliseconds < 1000)
            return $"{milliseconds}ms";

        if (milliseconds < 60000)
            return $"{milliseconds / 1000.0:F1}s";

        if (milliseconds < 3600000)
            return $"{milliseconds / 60000}m {(milliseconds % 60000) / 1000}s";

        return $"{milliseconds / 3600000}h {(milliseconds % 3600000) / 60000}m";
    }

    #endregion
}
