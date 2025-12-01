using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Interfaces.Services;

/// <summary>
/// Service interface for log operations.
/// Handles log ingestion, searching, and statistics.
/// </summary>
public interface ILogService
{
    #region Ingestion

    /// <summary>
    /// Creates a single log entry.
    /// </summary>
    /// <param name="request">Log creation request.</param>
    /// <param name="clientIp">Client IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Log ingestion result with ID.</returns>
    Task<LogIngestionResult> CreateLogAsync(
        CreateLogRequest request,
        string? clientIp = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple log entries in a batch.
    /// </summary>
    /// <param name="request">Batch log request.</param>
    /// <param name="clientIp">Client IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Batch result with accepted/rejected counts.</returns>
    Task<BatchLogResult> CreateBatchAsync(
        BatchLogRequest request,
        string? clientIp = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingests logs from a stream (for large batches).
    /// </summary>
    /// <param name="logs">Stream of log requests.</param>
    /// <param name="clientIp">Client IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total logs ingested.</returns>
    Task<int> IngestStreamAsync(
        IAsyncEnumerable<CreateLogRequest> logs,
        string? clientIp = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Query

    /// <summary>
    /// Gets a log entry by ID.
    /// </summary>
    /// <param name="id">Log entry ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Log entry response or null.</returns>
    Task<LogEntryResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a log entry with related logs (same correlation).
    /// </summary>
    /// <param name="id">Log entry ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed log entry or null.</returns>
    Task<LogEntryDetailResponse?> GetDetailAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches logs with filtering and pagination.
    /// </summary>
    /// <param name="request">Search request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated log entries.</returns>
    Task<PaginatedResult<LogEntryResponse>> SearchAsync(
        LogSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets logs by correlation ID.
    /// </summary>
    /// <param name="correlationId">Correlation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Related log entries.</returns>
    Task<IReadOnlyList<LogEntryResponse>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets logs by trace ID.
    /// </summary>
    /// <param name="traceId">Trace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Related log entries.</returns>
    Task<IReadOnlyList<LogEntryResponse>> GetByTraceIdAsync(
        string traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets logs for a job execution.
    /// </summary>
    /// <param name="jobExecutionId">Job execution ID.</param>
    /// <param name="minLevel">Minimum log level.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Log entries for the execution.</returns>
    Task<IReadOnlyList<LogEntryResponse>> GetByJobExecutionAsync(
        Guid jobExecutionId,
        LogLevel? minLevel = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent error logs.
    /// </summary>
    /// <param name="count">Number of errors.</param>
    /// <param name="serverName">Optional server filter.</param>
    /// <param name="jobId">Optional job filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent error logs.</returns>
    Task<IReadOnlyList<RecentErrorResponse>> GetRecentErrorsAsync(
        int count = 50,
        string? serverName = null,
        string? jobId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets log statistics for a time range.
    /// </summary>
    /// <param name="request">Statistics request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Log statistics response.</returns>
    Task<LogStatisticsResponse> GetStatisticsAsync(
        LogStatisticsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets log volume trend data.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="interval">Aggregation interval.</param>
    /// <param name="serverName">Optional server filter.</param>
    /// <param name="jobId">Optional job filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Trend data points.</returns>
    Task<IReadOnlyList<LogTrendPoint>> GetTrendAsync(
        DateTime startDate,
        DateTime endDate,
        TrendInterval interval,
        string? serverName = null,
        string? jobId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets log counts by level.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="serverName">Optional server filter.</param>
    /// <param name="jobId">Optional job filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Counts by level.</returns>
    Task<Dictionary<LogLevel, long>> GetCountsByLevelAsync(
        DateTime startDate,
        DateTime endDate,
        string? serverName = null,
        string? jobId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Lookups

    /// <summary>
    /// Gets distinct server names from logs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server names.</returns>
    Task<IReadOnlyList<string>> GetDistinctServersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets distinct job IDs from logs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job IDs.</returns>
    Task<IReadOnlyList<string>> GetDistinctJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets distinct categories from logs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Categories.</returns>
    Task<IReadOnlyList<string>> GetDistinctCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets distinct tags from logs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tags.</returns>
    Task<IReadOnlyList<string>> GetDistinctTagsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Export

    /// <summary>
    /// Exports logs matching the filter.
    /// </summary>
    /// <param name="request">Export request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of log entries.</returns>
    IAsyncEnumerable<LogEntryResponse> ExportAsync(
        LogExportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports logs to a file.
    /// </summary>
    /// <param name="request">Export request.</param>
    /// <param name="outputStream">Output stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of logs exported.</returns>
    Task<int> ExportToStreamAsync(
        LogExportRequest request,
        Stream outputStream,
        CancellationToken cancellationToken = default);

    #endregion
}
