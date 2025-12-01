using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Repositories;
using System.Text.Json;

namespace FMSLogNexus.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for job execution operations.
/// </summary>
public class JobExecutionRepository : RepositoryBase<JobExecution>, IJobExecutionRepository
{
    public JobExecutionRepository(FMSLogNexusDbContext context) : base(context)
    {
    }

    #region Query Operations

    /// <inheritdoc />
    public async Task<PaginatedResult<JobExecution>> SearchAsync(
        ExecutionSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.JobId))
            query = query.Where(e => e.JobId == request.JobId);

        if (!string.IsNullOrWhiteSpace(request.ServerName))
            query = query.Where(e => e.ServerName == request.ServerName);

        if (request.Status.HasValue)
            query = query.Where(e => e.Status == request.Status.Value);

        if (request.StartDate.HasValue)
            query = query.Where(e => e.StartedAt >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(e => e.StartedAt <= request.EndDate.Value);

        if (!string.IsNullOrWhiteSpace(request.TriggerType))
            query = query.Where(e => e.TriggerType == request.TriggerType);

        if (request.IsRunning == true)
            query = query.Where(e => e.CompletedAt == null);

        if (request.IsFailed == true)
            query = query.Where(e => e.Status == FMSLogNexus.Core.Enums.JobStatus.Failed);

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = query.OrderByDescending(e => e.StartedAt);

        // Apply pagination
        var page = request.Page;
        var pageSize = request.PageSize;

        var items = await query
            .Include(e => e.Job)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PaginatedResult<JobExecution>.Create(items, totalCount, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<JobExecution?> GetWithJobAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.Job)
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<JobExecution?> GetWithLogCountsAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<JobExecution>> GetByJobAsync(
        string jobId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Where(e => e.JobId == jobId);

        if (startDate.HasValue)
            query = query.Where(e => e.StartedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.StartedAt <= endDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PaginatedResult<JobExecution>.Create(items, totalCount, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobExecution>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(e => e.CorrelationId == correlationId)
            .OrderBy(e => e.StartedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobExecution>> GetRecentByJobAsync(
        string jobId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(e => e.JobId == jobId)
            .OrderByDescending(e => e.StartedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<JobExecution?> GetLastByJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(e => e.JobId == jobId)
            .OrderByDescending(e => e.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    #endregion

    #region Running Executions

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobExecution>> GetRunningAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(e => e.Status == JobStatus.Running || e.Status == JobStatus.Pending)
            .Include(e => e.Job)
            .OrderBy(e => e.StartedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobExecution>> GetRunningByJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(e => e.JobId == jobId)
            .Where(e => e.Status == JobStatus.Running || e.Status == JobStatus.Pending)
            .OrderBy(e => e.StartedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobExecution>> GetRunningByServerAsync(
        string serverName,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(e => e.ServerName == serverName)
            .Where(e => e.Status == JobStatus.Running || e.Status == JobStatus.Pending)
            .Include(e => e.Job)
            .OrderBy(e => e.StartedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobExecution>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await DbSet.AsNoTracking()
            .Where(e => e.Status == JobStatus.Running)
            .Include(e => e.Job)
            .Where(e => e.Job != null && e.Job.MaxDurationMs.HasValue)
            .Where(e => EF.Functions.DateDiffMillisecond(e.StartedAt, now) > e.Job!.MaxDurationMs!.Value)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsJobRunningAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            e => e.JobId == jobId && (e.Status == JobStatus.Running || e.Status == JobStatus.Pending),
            cancellationToken);
    }

    #endregion

    #region Recent Failures

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobExecution>> GetRecentFailuresAsync(
        int count = 20,
        string? serverName = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Where(e => e.Status == JobStatus.Failed || e.Status == JobStatus.Timeout);

        if (!string.IsNullOrWhiteSpace(serverName))
            query = query.Where(e => e.ServerName == serverName);

        return await query
            .Include(e => e.Job)
            .OrderByDescending(e => e.StartedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetConsecutiveFailureCountAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        var recentExecutions = await DbSet.AsNoTracking()
            .Where(e => e.JobId == jobId)
            .Where(e => e.Status != JobStatus.Running && e.Status != JobStatus.Pending)
            .OrderByDescending(e => e.StartedAt)
            .Take(100)
            .Select(e => e.Status)
            .ToListAsync(cancellationToken);

        var consecutiveFailures = 0;
        foreach (var status in recentExecutions)
        {
            if (status == JobStatus.Failed || status == JobStatus.Timeout)
                consecutiveFailures++;
            else
                break;
        }

        return consecutiveFailures;
    }

    #endregion

    #region Statistics

    /// <inheritdoc />
    public async Task<Dictionary<JobStatus, int>> GetCountsByStatusAsync(
        DateTime startDate,
        DateTime endDate,
        string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Where(e => e.StartedAt >= startDate && e.StartedAt <= endDate);

        if (!string.IsNullOrWhiteSpace(jobId))
            query = query.Where(e => e.JobId == jobId);

        var counts = await query
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.Status, x => x.Count);
    }

    /// <inheritdoc />
    public async Task<ExecutionStatistics> GetStatisticsAsync(
        string jobId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var executions = await DbSet.AsNoTracking()
            .Where(e => e.JobId == jobId)
            .Where(e => e.StartedAt >= startDate && e.StartedAt <= endDate)
            .ToListAsync(cancellationToken);

        if (executions.Count == 0)
        {
            return new ExecutionStatistics();
        }

        var completed = executions.Where(e => e.DurationMs.HasValue).ToList();

        return new ExecutionStatistics
        {
            TotalExecutions = executions.Count,
            SuccessCount = executions.Count(e => e.Status == JobStatus.Completed),
            FailureCount = executions.Count(e => e.Status == JobStatus.Failed),
            TimeoutCount = executions.Count(e => e.Status == JobStatus.Timeout),
            CancelledCount = executions.Count(e => e.Status == JobStatus.Cancelled),
            SuccessRate = executions.Count > 0
                ? (decimal)executions.Count(e => e.Status == JobStatus.Completed) / executions.Count * 100
                : 0,
            AvgDurationMs = completed.Count > 0 ? (long)completed.Average(e => e.DurationMs!.Value) : 0,
            MinDurationMs = completed.Count > 0 ? completed.Min(e => e.DurationMs!.Value) : 0,
            MaxDurationMs = completed.Count > 0 ? completed.Max(e => e.DurationMs!.Value) : 0,
            FirstExecution = executions.Min(e => e.StartedAt),
            LastExecution = executions.Max(e => e.StartedAt)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExecutionTrendPoint>> GetTrendDataAsync(
        DateTime startDate,
        DateTime endDate,
        TrendInterval interval,
        string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Where(e => e.StartedAt >= startDate && e.StartedAt <= endDate);

        if (!string.IsNullOrWhiteSpace(jobId))
            query = query.Where(e => e.JobId == jobId);

        // Group by time interval
        var groupedQuery = interval switch
        {
            TrendInterval.Hour => query.GroupBy(e => new { e.StartedAt.Year, e.StartedAt.Month, e.StartedAt.Day, e.StartedAt.Hour }),
            TrendInterval.Day => query.GroupBy(e => new { e.StartedAt.Year, e.StartedAt.Month, e.StartedAt.Day, Hour = 0 }),
            TrendInterval.Week => query.GroupBy(e => new { e.StartedAt.Year, e.StartedAt.Month, Day = e.StartedAt.Day / 7 * 7, Hour = 0 }),
            TrendInterval.Month => query.GroupBy(e => new { e.StartedAt.Year, e.StartedAt.Month, Day = 1, Hour = 0 }),
            _ => query.GroupBy(e => new { e.StartedAt.Year, e.StartedAt.Month, e.StartedAt.Day, e.StartedAt.Hour })
        };

        var results = await groupedQuery
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                g.Key.Hour,
                Total = g.Count(),
                Success = g.Count(e => e.Status == JobStatus.Completed),
                Failed = g.Count(e => e.Status == JobStatus.Failed || e.Status == JobStatus.Timeout),
                AvgDuration = g.Where(e => e.DurationMs.HasValue).Average(e => (long?)e.DurationMs) ?? 0
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ThenBy(x => x.Day)
            .ThenBy(x => x.Hour)
            .ToListAsync(cancellationToken);

        return results.Select(r => new ExecutionTrendPoint
        {
            Timestamp = new DateTime(r.Year, r.Month, r.Day, r.Hour, 0, 0, DateTimeKind.Utc),
            TotalCount = r.Total,
            SuccessCount = r.Success,
            FailureCount = r.Failed,
            AvgDurationMs = (long)r.AvgDuration
        }).ToList();
    }

    #endregion

    #region Lifecycle Operations

    /// <inheritdoc />
    public async Task<JobExecution> StartAsync(JobExecution execution, CancellationToken cancellationToken = default)
    {
        execution.Status = JobStatus.Running;
        execution.StartedAt = DateTime.UtcNow;

        await DbSet.AddAsync(execution, cancellationToken);
        return execution;
    }

    /// <inheritdoc />
    public async Task<JobExecution?> CompleteAsync(
        Guid executionId,
        JobStatus status,
        Dictionary<string, object>? resultSummary = null,
        int? resultCode = null,
        string? errorMessage = null,
        string? errorCategory = null,
        CancellationToken cancellationToken = default)
    {
        var execution = await DbSet.FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);
        if (execution == null)
            return null;

        execution.Status = status;
        execution.CompletedAt = DateTime.UtcNow;
        // DurationMs is a computed property based on StartedAt and CompletedAt
        execution.ResultCode = resultCode;
        execution.ErrorMessage = errorMessage;
        execution.ErrorCategory = errorCategory;

        if (resultSummary != null)
        {
            execution.ResultSummary = JsonSerializer.Serialize(resultSummary);
        }

        return execution;
    }

    /// <inheritdoc />
    public async Task IncrementLogCountAsync(
        Guid executionId,
        LogLevel level,
        CancellationToken cancellationToken = default)
    {
        var execution = await DbSet.FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);
        if (execution == null)
            return;

        execution.LogCount++;

        switch (level)
        {
            case LogLevel.Trace:
                execution.TraceCount++;
                break;
            case LogLevel.Debug:
                execution.DebugCount++;
                break;
            case LogLevel.Information:
                execution.InfoCount++;
                break;
            case LogLevel.Warning:
                execution.WarningCount++;
                break;
            case LogLevel.Error:
                execution.ErrorCount++;
                break;
            case LogLevel.Critical:
                execution.CriticalCount++;
                break;
        }
    }

    /// <inheritdoc />
    public async Task IncrementLogCountsAsync(
        Guid executionId,
        Dictionary<LogLevel, int> counts,
        CancellationToken cancellationToken = default)
    {
        var execution = await DbSet.FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);
        if (execution == null)
            return;

        foreach (var (level, count) in counts)
        {
            execution.LogCount += count;

            switch (level)
            {
                case LogLevel.Trace:
                    execution.TraceCount += count;
                    break;
                case LogLevel.Debug:
                    execution.DebugCount += count;
                    break;
                case LogLevel.Information:
                    execution.InfoCount += count;
                    break;
                case LogLevel.Warning:
                    execution.WarningCount += count;
                    break;
                case LogLevel.Error:
                    execution.ErrorCount += count;
                    break;
                case LogLevel.Critical:
                    execution.CriticalCount += count;
                    break;
            }
        }
    }

    /// <inheritdoc />
    public async Task<int> TimeoutStuckExecutionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Get running executions with their job's timeout
        var stuckExecutions = await DbSet
            .Where(e => e.Status == JobStatus.Running)
            .Include(e => e.Job)
            .Where(e => e.Job != null && e.Job.MaxDurationMs.HasValue)
            .ToListAsync(cancellationToken);

        var timedOutCount = 0;

        foreach (var execution in stuckExecutions)
        {
            var runningMs = (now - execution.StartedAt).TotalMilliseconds;
            if (runningMs > execution.Job!.MaxDurationMs!.Value)
            {
                execution.Status = JobStatus.Timeout;
                execution.CompletedAt = now;
                execution.ErrorMessage = "Execution timed out";
                timedOutCount++;
            }
        }

        return timedOutCount;
    }

    #endregion

    #region Maintenance

    /// <inheritdoc />
    public async Task<int> DeleteOldExecutionsAsync(
        DateTime olderThan,
        int keepMinimum = 100,
        CancellationToken cancellationToken = default)
    {
        // Delete old executions but keep a minimum per job
        var sql = @"
            WITH ExecutionRanks AS (
                SELECT Id, JobId,
                    ROW_NUMBER() OVER (PARTITION BY JobId ORDER BY StartedAt DESC) AS RowNum
                FROM [job].[JobExecutions]
            )
            DELETE FROM [job].[JobExecutions]
            WHERE Id IN (
                SELECT e.Id
                FROM [job].[JobExecutions] e
                INNER JOIN ExecutionRanks er ON e.Id = er.Id
                WHERE e.StartedAt < @OlderThan
                AND er.RowNum > @KeepMinimum
            )";

        return await Context.Database.ExecuteSqlRawAsync(
            sql,
            new SqlParameter("@OlderThan", olderThan),
            new SqlParameter("@KeepMinimum", keepMinimum));
    }

    #endregion
}
