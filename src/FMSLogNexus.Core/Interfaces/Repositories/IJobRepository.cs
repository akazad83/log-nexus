using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for job operations.
/// </summary>
public interface IJobRepository : IRepository<Job, string>
{
    #region Query Operations

    /// <summary>
    /// Searches jobs with filtering and pagination.
    /// </summary>
    /// <param name="request">Search request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated jobs.</returns>
    Task<PaginatedResult<Job>> SearchAsync(
        JobSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a job with its recent executions.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="recentExecutionCount">Number of recent executions to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job with executions.</returns>
    Task<Job?> GetWithRecentExecutionsAsync(
        string jobId,
        int recentExecutionCount = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets jobs by server.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="activeOnly">Only active jobs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Jobs on the server.</returns>
    Task<IReadOnlyList<Job>> GetByServerAsync(
        string serverName,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets jobs by category.
    /// </summary>
    /// <param name="category">Category name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Jobs in the category.</returns>
    Task<IReadOnlyList<Job>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets critical jobs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Critical jobs.</returns>
    Task<IReadOnlyList<Job>> GetCriticalJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets jobs with failed last execution.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Failed jobs.</returns>
    Task<IReadOnlyList<Job>> GetFailedJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets jobs that haven't run in expected time.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stale jobs.</returns>
    Task<IReadOnlyList<Job>> GetStaleJobsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Health and Statistics

    /// <summary>
    /// Gets job health scores.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job health data.</returns>
    Task<IReadOnlyList<JobHealthData>> GetJobHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates health score for a specific job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health score (0-100).</returns>
    Task<int> CalculateHealthScoreAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets job statistics summary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Statistics summary.</returns>
    Task<JobStatisticsSummary> GetStatisticsSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets jobs sorted by health score (worst first).
    /// </summary>
    /// <param name="top">Number of jobs to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unhealthy jobs.</returns>
    Task<IReadOnlyList<Job>> GetUnhealthyJobsAsync(
        int top = 10,
        CancellationToken cancellationToken = default);

    #endregion

    #region Lookups

    /// <summary>
    /// Gets distinct categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Categories.</returns>
    Task<IReadOnlyList<string>> GetDistinctCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets distinct tags.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tags.</returns>
    Task<IReadOnlyList<string>> GetDistinctTagsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets job lookup items for dropdowns.
    /// </summary>
    /// <param name="activeOnly">Only active jobs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Lookup items.</returns>
    Task<IReadOnlyList<LookupItem>> GetLookupItemsAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    #endregion

    #region Registration and Updates

    /// <summary>
    /// Registers or updates a job (upsert).
    /// </summary>
    /// <param name="job">Job to register.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if created, false if updated.</returns>
    Task<bool> RegisterOrUpdateAsync(Job job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates job statistics after an execution.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="execution">Completed execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateStatisticsAsync(
        string jobId,
        JobExecution execution,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if job was found and activated.</returns>
    Task<bool> ActivateAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if job was found and deactivated.</returns>
    Task<bool> DeactivateAsync(string jobId, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Job health data.
/// </summary>
public class JobHealthData
{
    public string JobId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int HealthScore { get; set; }
    public JobStatus? LastStatus { get; set; }
    public DateTime? LastExecutionAt { get; set; }
    public decimal? SuccessRate { get; set; }
    public int RecentFailures { get; set; }
}

/// <summary>
/// Job statistics summary.
/// </summary>
public class JobStatisticsSummary
{
    public int TotalJobs { get; set; }
    public int ActiveJobs { get; set; }
    public int CriticalJobs { get; set; }
    public int HealthyJobs { get; set; }
    public int WarningJobs { get; set; }
    public int CriticalHealthJobs { get; set; }
    public int FailedLastRun { get; set; }
    public int RunningNow { get; set; }
}
