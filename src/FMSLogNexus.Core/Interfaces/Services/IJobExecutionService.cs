using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Interfaces.Services;

/// <summary>
/// Service interface for job execution operations.
/// </summary>
public interface IJobExecutionService
{
    #region Lifecycle

    /// <summary>
    /// Starts a new job execution.
    /// </summary>
    /// <param name="request">The start execution request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created execution response.</returns>
    Task<JobExecutionResponse> StartExecutionAsync(
        StartExecutionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes an existing job execution.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="request">The completion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated execution response, or null if not found.</returns>
    Task<JobExecutionResponse?> CompleteExecutionAsync(
        Guid executionId,
        CompleteExecutionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an execution.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="reason">Optional cancellation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated execution response, or null if not found.</returns>
    Task<JobExecutionResponse?> CancelExecutionAsync(
        Guid executionId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Query

    /// <summary>
    /// Gets an execution by ID.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution response, or null if not found.</returns>
    Task<JobExecutionResponse?> GetExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed execution information including job details and logs.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The detailed execution response, or null if not found.</returns>
    Task<JobExecutionDetailResponse?> GetExecutionDetailAsync(
        Guid executionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches executions with filtering and pagination.
    /// </summary>
    /// <param name="request">The search request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated execution results.</returns>
    Task<PaginatedResult<JobExecutionResponse>> SearchAsync(
        ExecutionSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets executions for a specific job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated execution results.</returns>
    Task<PaginatedResult<JobExecutionResponse>> GetByJobAsync(
        string jobId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent executions for a specific job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="count">Number of recent executions to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recent executions.</returns>
    Task<IReadOnlyList<JobExecutionResponse>> GetRecentByJobAsync(
        string jobId,
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last execution for a specific job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The last execution response, or null if none found.</returns>
    Task<JobExecutionResponse?> GetLastByJobAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Running Executions

    /// <summary>
    /// Gets all currently running executions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of running executions.</returns>
    Task<IReadOnlyList<JobExecutionResponse>> GetRunningAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets running executions for a specific server.
    /// </summary>
    /// <param name="serverName">The server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of running executions on the server.</returns>
    Task<IReadOnlyList<JobExecutionResponse>> GetRunningByServerAsync(
        string serverName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a job is currently running.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the job is running, false otherwise.</returns>
    Task<bool> IsJobRunningAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets executions that have exceeded their timeout.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of overdue executions.</returns>
    Task<IReadOnlyList<JobExecutionResponse>> GetOverdueAsync(
        CancellationToken cancellationToken = default);

    #endregion

    #region Failures

    /// <summary>
    /// Gets recent failed executions.
    /// </summary>
    /// <param name="count">Number of failures to retrieve.</param>
    /// <param name="serverName">Optional server name filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recent failed executions.</returns>
    Task<IReadOnlyList<JobExecutionResponse>> GetRecentFailuresAsync(
        int count = 20,
        string? serverName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of consecutive failures for a job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of consecutive failures.</returns>
    Task<int> GetConsecutiveFailureCountAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets execution statistics for a job within a date range.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution statistics.</returns>
    Task<ExecutionStatistics> GetStatisticsAsync(
        string jobId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets execution trend data for charting.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="interval">Trend interval (hourly, daily, weekly).</param>
    /// <param name="jobId">Optional job ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of trend data points.</returns>
    Task<IReadOnlyList<ExecutionTrendPoint>> GetTrendAsync(
        DateTime startDate,
        DateTime endDate,
        TrendInterval interval,
        string? jobId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets execution counts grouped by status.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="jobId">Optional job ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of status to count.</returns>
    Task<Dictionary<JobStatus, int>> GetCountsByStatusAsync(
        DateTime startDate,
        DateTime endDate,
        string? jobId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Maintenance

    /// <summary>
    /// Times out stuck executions that have exceeded their timeout.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of executions timed out.</returns>
    Task<int> TimeoutStuckExecutionsAsync(
        CancellationToken cancellationToken = default);

    #endregion
}
