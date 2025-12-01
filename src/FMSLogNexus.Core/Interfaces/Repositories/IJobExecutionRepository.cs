using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for job execution operations.
/// </summary>
public interface IJobExecutionRepository : IRepository<JobExecution>
{
    #region Query Operations

    /// <summary>
    /// Searches executions with filtering and pagination.
    /// </summary>
    /// <param name="request">Search request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated executions.</returns>
    Task<PaginatedResult<JobExecution>> SearchAsync(
        ExecutionSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an execution with its job details.
    /// </summary>
    /// <param name="executionId">Execution ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution with job.</returns>
    Task<JobExecution?> GetWithJobAsync(
        Guid executionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an execution with log count summary.
    /// </summary>
    /// <param name="executionId">Execution ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution with log counts.</returns>
    Task<JobExecution?> GetWithLogCountsAsync(
        Guid executionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets executions for a specific job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="startDate">Start date filter.</param>
    /// <param name="endDate">End date filter.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated executions.</returns>
    Task<PaginatedResult<JobExecution>> GetByJobAsync(
        string jobId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets executions by correlation ID.
    /// </summary>
    /// <param name="correlationId">Correlation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Related executions.</returns>
    Task<IReadOnlyList<JobExecution>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last N executions for a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="count">Number of executions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent executions.</returns>
    Task<IReadOnlyList<JobExecution>> GetRecentByJobAsync(
        string jobId,
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last execution for a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Last execution or null.</returns>
    Task<JobExecution?> GetLastByJobAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Running Executions

    /// <summary>
    /// Gets all currently running executions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Running executions.</returns>
    Task<IReadOnlyList<JobExecution>> GetRunningAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets running executions for a specific job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Running executions for the job.</returns>
    Task<IReadOnlyList<JobExecution>> GetRunningByJobAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets running executions on a specific server.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Running executions on server.</returns>
    Task<IReadOnlyList<JobExecution>> GetRunningByServerAsync(
        string serverName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets executions that have exceeded their max duration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Overdue executions.</returns>
    Task<IReadOnlyList<JobExecution>> GetOverdueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a job is currently running.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if running.</returns>
    Task<bool> IsJobRunningAsync(string jobId, CancellationToken cancellationToken = default);

    #endregion

    #region Recent Failures

    /// <summary>
    /// Gets recent failed executions.
    /// </summary>
    /// <param name="count">Number of failures to retrieve.</param>
    /// <param name="serverName">Optional server filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Failed executions.</returns>
    Task<IReadOnlyList<JobExecution>> GetRecentFailuresAsync(
        int count = 20,
        string? serverName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets consecutive failure count for a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Consecutive failure count.</returns>
    Task<int> GetConsecutiveFailureCountAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets execution counts by status for a time range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="jobId">Optional job filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Counts by status.</returns>
    Task<Dictionary<JobStatus, int>> GetCountsByStatusAsync(
        DateTime startDate,
        DateTime endDate,
        string? jobId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets execution statistics for a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
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
    /// Gets execution trend data.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="interval">Aggregation interval.</param>
    /// <param name="jobId">Optional job filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Trend data.</returns>
    Task<IReadOnlyList<ExecutionTrendPoint>> GetTrendDataAsync(
        DateTime startDate,
        DateTime endDate,
        TrendInterval interval,
        string? jobId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Lifecycle Operations

    /// <summary>
    /// Starts a new execution.
    /// </summary>
    /// <param name="execution">Execution to start.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Started execution.</returns>
    Task<JobExecution> StartAsync(JobExecution execution, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes an execution.
    /// </summary>
    /// <param name="executionId">Execution ID.</param>
    /// <param name="status">Final status.</param>
    /// <param name="resultSummary">Optional result summary.</param>
    /// <param name="resultCode">Optional result code.</param>
    /// <param name="errorMessage">Error message if failed.</param>
    /// <param name="errorCategory">Error category if failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated execution.</returns>
    Task<JobExecution?> CompleteAsync(
        Guid executionId,
        JobStatus status,
        Dictionary<string, object>? resultSummary = null,
        int? resultCode = null,
        string? errorMessage = null,
        string? errorCategory = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments log count for an execution.
    /// </summary>
    /// <param name="executionId">Execution ID.</param>
    /// <param name="level">Log level.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IncrementLogCountAsync(
        Guid executionId,
        LogLevel level,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk increments log counts (after batch insert).
    /// </summary>
    /// <param name="executionId">Execution ID.</param>
    /// <param name="counts">Counts by level.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IncrementLogCountsAsync(
        Guid executionId,
        Dictionary<LogLevel, int> counts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Times out stuck executions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of executions timed out.</returns>
    Task<int> TimeoutStuckExecutionsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Maintenance

    /// <summary>
    /// Deletes old completed executions.
    /// </summary>
    /// <param name="olderThan">Delete executions before this date.</param>
    /// <param name="keepMinimum">Minimum executions to keep per job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of executions deleted.</returns>
    Task<int> DeleteOldExecutionsAsync(
        DateTime olderThan,
        int keepMinimum = 100,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Execution statistics.
/// </summary>
public class ExecutionStatistics
{
    public int TotalExecutions { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int TimeoutCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal SuccessRate { get; set; }
    public long AvgDurationMs { get; set; }
    public long MinDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
    public DateTime? FirstExecution { get; set; }
    public DateTime? LastExecution { get; set; }
}

/// <summary>
/// Execution trend data point.
/// </summary>
public class ExecutionTrendPoint
{
    public DateTime Timestamp { get; set; }
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public long AvgDurationMs { get; set; }
}
