using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for log entry operations.
/// Optimized for high-volume log ingestion and querying.
/// </summary>
public interface ILogRepository : IRepository<LogEntry>
{
    #region High-Performance Ingestion

    /// <summary>
    /// Bulk inserts log entries for high-throughput scenarios.
    /// Uses table-valued parameters or bulk copy for optimal performance.
    /// </summary>
    /// <param name="logs">Log entries to insert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of logs inserted.</returns>
    Task<int> BulkInsertAsync(IEnumerable<LogEntry> logs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts logs using streaming for very large batches.
    /// </summary>
    /// <param name="logs">Log entries to insert.</param>
    /// <param name="batchSize">Batch size for streaming.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total logs inserted.</returns>
    Task<int> StreamInsertAsync(
        IAsyncEnumerable<LogEntry> logs,
        int batchSize = 1000,
        CancellationToken cancellationToken = default);

    #endregion

    #region Search Operations

    /// <summary>
    /// Searches logs with comprehensive filtering.
    /// </summary>
    /// <param name="request">Search request with filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated log entries.</returns>
    Task<PaginatedResult<LogEntry>> SearchAsync(
        LogSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Full-text searches log messages.
    /// </summary>
    /// <param name="searchText">Text to search for.</param>
    /// <param name="startDate">Start date filter.</param>
    /// <param name="endDate">End date filter.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching log entries.</returns>
    Task<PaginatedResult<LogEntry>> FullTextSearchAsync(
        string searchText,
        DateTime? startDate,
        DateTime? endDate,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets logs by correlation ID for distributed tracing.
    /// </summary>
    /// <param name="correlationId">Correlation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Related log entries ordered by timestamp.</returns>
    Task<IReadOnlyList<LogEntry>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets logs by trace ID for distributed tracing.
    /// </summary>
    /// <param name="traceId">Trace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Related log entries.</returns>
    Task<IReadOnlyList<LogEntry>> GetByTraceIdAsync(
        string traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets logs for a specific job execution.
    /// </summary>
    /// <param name="jobExecutionId">Job execution ID.</param>
    /// <param name="minLevel">Minimum log level (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Log entries for the execution.</returns>
    Task<IReadOnlyList<LogEntry>> GetByJobExecutionAsync(
        Guid jobExecutionId,
        LogLevel? minLevel = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets logs for a job within a time range.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="minLevel">Minimum log level.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated log entries.</returns>
    Task<PaginatedResult<LogEntry>> GetByJobAsync(
        string jobId,
        DateTime startDate,
        DateTime endDate,
        LogLevel? minLevel = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent error logs.
    /// </summary>
    /// <param name="count">Number of errors to retrieve.</param>
    /// <param name="serverName">Optional server filter.</param>
    /// <param name="jobId">Optional job filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent error log entries.</returns>
    Task<IReadOnlyList<LogEntry>> GetRecentErrorsAsync(
        int count = 50,
        string? serverName = null,
        string? jobId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Statistics and Aggregations

    /// <summary>
    /// Gets log count by level for a time range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="serverName">Optional server filter.</param>
    /// <param name="jobId">Optional job filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Counts by log level.</returns>
    Task<Dictionary<LogLevel, long>> GetCountsByLevelAsync(
        DateTime startDate,
        DateTime endDate,
        string? serverName = null,
        string? jobId = null,
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
    /// <returns>Time series data points.</returns>
    Task<IReadOnlyList<LogTrendPoint>> GetTrendDataAsync(
        DateTime startDate,
        DateTime endDate,
        TrendInterval interval,
        string? serverName = null,
        string? jobId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top exception types.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="top">Number of top exceptions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exception type summaries.</returns>
    Task<IReadOnlyList<ExceptionSummary>> GetTopExceptionsAsync(
        DateTime startDate,
        DateTime endDate,
        int top = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets log summary statistics.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="serverName">Optional server filter.</param>
    /// <param name="jobId">Optional job filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Log summary statistics.</returns>
    Task<LogSummary> GetSummaryAsync(
        DateTime startDate,
        DateTime endDate,
        string? serverName = null,
        string? jobId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets log counts grouped by job.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="top">Number of top jobs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job log summaries.</returns>
    Task<IReadOnlyList<JobLogSummary>> GetLogsByJobAsync(
        DateTime startDate,
        DateTime endDate,
        int top = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts errors in a time window (for alerting).
    /// </summary>
    /// <param name="windowMinutes">Time window in minutes.</param>
    /// <param name="minLevel">Minimum log level.</param>
    /// <param name="serverName">Optional server filter.</param>
    /// <param name="jobId">Optional job filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Error count.</returns>
    Task<int> CountErrorsInWindowAsync(
        int windowMinutes,
        LogLevel minLevel = LogLevel.Error,
        string? serverName = null,
        string? jobId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Lookup Operations

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

    #region Maintenance Operations

    /// <summary>
    /// Deletes logs older than the specified date.
    /// </summary>
    /// <param name="olderThan">Delete logs before this date.</param>
    /// <param name="batchSize">Batch size for deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of logs deleted.</returns>
    Task<long> DeleteOldLogsAsync(
        DateTime olderThan,
        int batchSize = 10000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives logs to a separate table/storage.
    /// </summary>
    /// <param name="olderThan">Archive logs before this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of logs archived.</returns>
    Task<long> ArchiveLogsAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets partition information for maintenance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Partition statistics.</returns>
    Task<IReadOnlyList<PartitionInfo>> GetPartitionInfoAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Export

    /// <summary>
    /// Exports logs matching the filter to a stream.
    /// </summary>
    /// <param name="request">Export request with filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of log entries.</returns>
    IAsyncEnumerable<LogEntry> ExportAsync(
        LogExportRequest request,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Partition information for maintenance operations.
/// </summary>
public class PartitionInfo
{
    public int PartitionNumber { get; set; }
    public DateTime? MinTimestamp { get; set; }
    public DateTime? MaxTimestamp { get; set; }
    public long RowCount { get; set; }
    public long SizeBytes { get; set; }
    public string FileGroup { get; set; } = string.Empty;
}
