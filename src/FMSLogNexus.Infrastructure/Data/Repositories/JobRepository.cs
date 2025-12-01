using Microsoft.EntityFrameworkCore;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Repositories;

namespace FMSLogNexus.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for job operations.
/// </summary>
public class JobRepository : RepositoryBase<Job, string>, IJobRepository
{
    public JobRepository(FMSLogNexusDbContext context) : base(context)
    {
    }

    #region Query Operations

    /// <inheritdoc />
    public async Task<PaginatedResult<Job>> SearchAsync(
        JobSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        // Apply filters
        if (request.IsActive.HasValue)
            query = query.Where(j => j.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.ServerName))
            query = query.Where(j => j.ServerName == request.ServerName);

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(j => j.Category == request.Category);

        if (request.JobType.HasValue)
            query = query.Where(j => j.JobType == request.JobType.Value);

        if (request.LastStatus.HasValue)
            query = query.Where(j => j.LastStatus == request.LastStatus.Value);

        if (request.IsCritical.HasValue)
            query = query.Where(j => j.IsCritical == request.IsCritical.Value);

        // Apply text search
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchPattern = $"%{request.Search}%";
            query = query.Where(j =>
                EF.Functions.Like(j.Id, searchPattern) ||
                EF.Functions.Like(j.DisplayName, searchPattern) ||
                (j.Description != null && EF.Functions.Like(j.Description, searchPattern)));
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        var isDescending = request.SortOrder == FMSLogNexus.Core.Enums.SortOrder.Descending;
        query = request.SortBy?.ToLower() switch
        {
            "name" or "displayname" => isDescending
                ? query.OrderByDescending(j => j.DisplayName)
                : query.OrderBy(j => j.DisplayName),
            "lastexecutionat" or "lastrun" => isDescending
                ? query.OrderByDescending(j => j.LastExecutionAt)
                : query.OrderBy(j => j.LastExecutionAt),
            "laststatus" => isDescending
                ? query.OrderByDescending(j => j.LastStatus)
                : query.OrderBy(j => j.LastStatus),
            "successrate" => isDescending
                ? query.OrderByDescending(j => j.SuccessRate)
                : query.OrderBy(j => j.SuccessRate),
            _ => query.OrderBy(j => j.DisplayName)
        };

        // Apply pagination
        var page = request.Page;
        var pageSize = request.PageSize;

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PaginatedResult<Job>.Create(items, totalCount, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<Job?> GetWithRecentExecutionsAsync(
        string jobId,
        int recentExecutionCount = 10,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(j => j.Executions
                .OrderByDescending(e => e.StartedAt)
                .Take(recentExecutionCount))
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Job>> GetByServerAsync(
        string serverName,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Where(j => j.ServerName == serverName);

        if (activeOnly)
            query = query.Where(j => j.IsActive);

        return await query
            .OrderBy(j => j.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Job>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(j => j.Category == category)
            .Where(j => j.IsActive)
            .OrderBy(j => j.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Job>> GetCriticalJobsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(j => j.IsCritical && j.IsActive)
            .OrderBy(j => j.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Job>> GetFailedJobsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(j => j.IsActive && j.LastStatus == JobStatus.Failed)
            .OrderByDescending(j => j.LastExecutionAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Job>> GetStaleJobsAsync(CancellationToken cancellationToken = default)
    {
        // Jobs that haven't run in 24 hours when they have a schedule
        var staleThreshold = DateTime.UtcNow.AddHours(-24);

        return await DbSet.AsNoTracking()
            .Where(j => j.IsActive)
            .Where(j => j.Schedule != null)
            .Where(j => j.LastExecutionAt == null || j.LastExecutionAt < staleThreshold)
            .OrderBy(j => j.LastExecutionAt)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Health and Statistics

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobHealthData>> GetJobHealthAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await DbSet.AsNoTracking()
            .Where(j => j.IsActive)
            .ToListAsync(cancellationToken);

        var result = new List<JobHealthData>();

        foreach (var job in jobs)
        {
            var healthScore = CalculateHealthScore(job);

            result.Add(new JobHealthData
            {
                JobId = job.Id,
                DisplayName = job.DisplayName,
                HealthScore = healthScore,
                LastStatus = job.LastStatus,
                LastExecutionAt = job.LastExecutionAt,
                SuccessRate = job.SuccessRate,
                RecentFailures = job.FailureCount
            });
        }

        return result.OrderBy(h => h.HealthScore).ToList();
    }

    /// <inheritdoc />
    public async Task<int> CalculateHealthScoreAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var job = await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        return job == null ? 0 : CalculateHealthScore(job);
    }

    private static int CalculateHealthScore(Job job)
    {
        var score = 100;

        // Deduct for failed last status
        if (job.LastStatus == JobStatus.Failed)
            score -= 40;
        else if (job.LastStatus == JobStatus.Timeout)
            score -= 30;
        else if (job.LastStatus == JobStatus.Cancelled)
            score -= 10;

        // Deduct for low success rate
        if (job.SuccessRate.HasValue)
        {
            if (job.SuccessRate < 50)
                score -= 30;
            else if (job.SuccessRate < 80)
                score -= 15;
            else if (job.SuccessRate < 95)
                score -= 5;
        }

        // Deduct for no recent executions
        if (job.LastExecutionAt.HasValue)
        {
            var hoursSinceLastRun = (DateTime.UtcNow - job.LastExecutionAt.Value).TotalHours;
            if (hoursSinceLastRun > 48)
                score -= 20;
            else if (hoursSinceLastRun > 24)
                score -= 10;
        }
        else if (job.Schedule != null)
        {
            // Scheduled job that has never run
            score -= 15;
        }

        // Deduct for high failure count
        if (job.FailureCount > 10)
            score -= 10;
        else if (job.FailureCount > 5)
            score -= 5;

        return Math.Max(0, Math.Min(100, score));
    }

    /// <inheritdoc />
    public async Task<JobStatisticsSummary> GetStatisticsSummaryAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await DbSet.AsNoTracking().ToListAsync(cancellationToken);

        var runningCount = await Context.JobExecutions
            .CountAsync(e => e.Status == JobStatus.Running, cancellationToken);

        var summary = new JobStatisticsSummary
        {
            TotalJobs = jobs.Count,
            ActiveJobs = jobs.Count(j => j.IsActive),
            CriticalJobs = jobs.Count(j => j.IsCritical && j.IsActive),
            FailedLastRun = jobs.Count(j => j.IsActive && j.LastStatus == JobStatus.Failed),
            RunningNow = runningCount
        };

        // Calculate health distribution
        foreach (var job in jobs.Where(j => j.IsActive))
        {
            var score = CalculateHealthScore(job);
            if (score >= 80)
                summary.HealthyJobs++;
            else if (score >= 50)
                summary.WarningJobs++;
            else
                summary.CriticalHealthJobs++;
        }

        return summary;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Job>> GetUnhealthyJobsAsync(
        int top = 10,
        CancellationToken cancellationToken = default)
    {
        var jobs = await DbSet.AsNoTracking()
            .Where(j => j.IsActive)
            .ToListAsync(cancellationToken);

        return jobs
            .Select(j => new { Job = j, HealthScore = CalculateHealthScore(j) })
            .OrderBy(x => x.HealthScore)
            .Take(top)
            .Select(x => x.Job)
            .ToList();
    }

    #endregion

    #region Lookups

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDistinctCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(j => j.Category != null)
            .Select(j => j.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDistinctTagsAsync(CancellationToken cancellationToken = default)
    {
        var allTags = await DbSet.AsNoTracking()
            .Where(j => j.Tags != null)
            .Select(j => j.Tags!)
            .ToListAsync(cancellationToken);

        return allTags
            .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct()
            .OrderBy(t => t)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LookupItem>> GetLookupItemsAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        if (activeOnly)
            query = query.Where(j => j.IsActive);

        return await query
            .OrderBy(j => j.DisplayName)
            .Select(j => new LookupItem
            {
                Value = j.Id,
                Text = j.DisplayName
            })
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Registration and Updates

    /// <inheritdoc />
    public async Task<bool> RegisterOrUpdateAsync(Job job, CancellationToken cancellationToken = default)
    {
        var existing = await DbSet.FirstOrDefaultAsync(j => j.Id == job.Id, cancellationToken);

        if (existing == null)
        {
            await DbSet.AddAsync(job, cancellationToken);
            return true;
        }

        // Update existing job
        existing.DisplayName = job.DisplayName;
        existing.Description = job.Description;
        existing.JobType = job.JobType;
        existing.Category = job.Category;
        existing.Tags = job.Tags;
        existing.ServerName = job.ServerName;
        existing.ExecutablePath = job.ExecutablePath;
        existing.Schedule = job.Schedule;
        existing.ScheduleDescription = job.ScheduleDescription;
        existing.ExpectedDurationMs = job.ExpectedDurationMs;
        existing.MaxDurationMs = job.MaxDurationMs;
        existing.IsCritical = job.IsCritical;
        existing.Configuration = job.Configuration;
        existing.UpdatedAt = DateTime.UtcNow;

        return false;
    }

    /// <inheritdoc />
    public async Task UpdateStatisticsAsync(
        string jobId,
        JobExecution execution,
        CancellationToken cancellationToken = default)
    {
        var job = await DbSet.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job == null)
            return;

        job.LastExecutionId = execution.Id;
        job.LastExecutionAt = execution.StartedAt;
        job.LastStatus = execution.Status;
        job.LastDurationMs = execution.DurationMs;
        job.TotalExecutions++;

        if (execution.Status == JobStatus.Completed)
            job.SuccessCount++;
        else if (execution.Status == JobStatus.Failed || execution.Status == JobStatus.Timeout)
            job.FailureCount++;

        // Recalculate average duration
        if (execution.DurationMs.HasValue)
        {
            if (job.AvgDurationMs.HasValue)
            {
                // Running average
                job.AvgDurationMs = (long)((job.AvgDurationMs.Value * (job.TotalExecutions - 1) + execution.DurationMs.Value) / job.TotalExecutions);
            }
            else
            {
                job.AvgDurationMs = execution.DurationMs;
            }
        }

        job.UpdatedAt = DateTime.UtcNow;
    }

    /// <inheritdoc />
    public async Task<bool> ActivateAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var job = await DbSet.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job == null)
            return false;

        job.IsActive = true;
        job.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var job = await DbSet.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job == null)
            return false;

        job.IsActive = false;
        job.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    #endregion
}
