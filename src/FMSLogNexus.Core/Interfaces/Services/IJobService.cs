using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Interfaces.Services;

/// <summary>
/// Service interface for job operations.
/// Handles job registration, execution tracking, and health monitoring.
/// </summary>
public interface IJobService
{
    #region Job Management

    /// <summary>
    /// Creates or registers a new job.
    /// </summary>
    /// <param name="request">Job creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job detail response.</returns>
    Task<JobDetailResponse> CreateJobAsync(
        CreateJobRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a job by ID.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job detail response or null.</returns>
    Task<JobDetailResponse?> GetJobAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="request">Update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated job response.</returns>
    Task<JobDetailResponse?> UpdateJobAsync(
        string jobId,
        UpdateJobRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a job (soft delete).
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted.</returns>
    Task<bool> DeleteJobAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches jobs with filtering and pagination.
    /// </summary>
    /// <param name="request">Search request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated job list.</returns>
    Task<PaginatedResult<JobListItem>> SearchJobsAsync(
        JobSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if activated.</returns>
    Task<bool> ActivateJobAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deactivated.</returns>
    Task<bool> DeactivateJobAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Execution Management

    /// <summary>
    /// Starts a new job execution.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="request">Start execution request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Start execution result.</returns>
    Task<StartExecutionResult> StartExecutionAsync(
        string jobId,
        StartExecutionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a job execution.
    /// </summary>
    /// <param name="executionId">Execution ID.</param>
    /// <param name="request">Complete execution request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution detail response.</returns>
    Task<ExecutionDetailResponse?> CompleteExecutionAsync(
        Guid executionId,
        CompleteExecutionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an execution by ID.
    /// </summary>
    /// <param name="executionId">Execution ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution detail response or null.</returns>
    Task<ExecutionDetailResponse?> GetExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches executions with filtering and pagination.
    /// </summary>
    /// <param name="request">Search request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated execution list.</returns>
    Task<PaginatedResult<ExecutionListItem>> SearchExecutionsAsync(
        ExecutionSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets executions for a specific job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated execution list.</returns>
    Task<PaginatedResult<ExecutionListItem>> GetJobExecutionsAsync(
        string jobId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets currently running executions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Running job information.</returns>
    Task<IReadOnlyList<RunningJobInfo>> GetRunningExecutionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent failed executions.
    /// </summary>
    /// <param name="count">Number of failures.</param>
    /// <param name="serverName">Optional server filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent failures.</returns>
    Task<IReadOnlyList<RecentFailure>> GetRecentFailuresAsync(
        int count = 20,
        string? serverName = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Health and Statistics

    /// <summary>
    /// Gets job health overview.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job health overview.</returns>
    Task<JobHealthOverviewResponse> GetHealthOverviewAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates health score for a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health score (0-100).</returns>
    Task<int> CalculateHealthScoreAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets job statistics.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job statistics.</returns>
    Task<JobStatistics> GetJobStatisticsAsync(
        string jobId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unhealthy jobs (for alerts/dashboard).
    /// </summary>
    /// <param name="top">Number of jobs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unhealthy jobs.</returns>
    Task<IReadOnlyList<JobListItem>> GetUnhealthyJobsAsync(
        int top = 10,
        CancellationToken cancellationToken = default);

    #endregion

    #region Lookups

    /// <summary>
    /// Gets job lookup items for dropdowns.
    /// </summary>
    /// <param name="activeOnly">Only active jobs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Lookup items.</returns>
    Task<IReadOnlyList<LookupItem>> GetJobLookupAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets distinct categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Categories.</returns>
    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets distinct tags.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tags.</returns>
    Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a job exists.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists.</returns>
    Task<bool> JobExistsAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    #endregion
}
