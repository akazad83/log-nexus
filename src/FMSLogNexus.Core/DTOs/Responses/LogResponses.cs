using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.DTOs.Responses;

/// <summary>
/// Response for a single log entry.
/// </summary>
public class LogEntryResponse
{
    /// <summary>
    /// Log entry ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// UTC timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Log level.
    /// </summary>
    public LogLevel Level { get; set; }

    /// <summary>
    /// Log level name.
    /// </summary>
    public string LevelName => Level.ToString();

    /// <summary>
    /// Log message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Message template.
    /// </summary>
    public string? MessageTemplate { get; set; }

    /// <summary>
    /// Job ID.
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// Job display name.
    /// </summary>
    public string? JobDisplayName { get; set; }

    /// <summary>
    /// Job execution ID.
    /// </summary>
    public Guid? JobExecutionId { get; set; }

    /// <summary>
    /// Server name.
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// Category.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Source context.
    /// </summary>
    public string? SourceContext { get; set; }

    /// <summary>
    /// Correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Trace ID.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Span ID.
    /// </summary>
    public string? SpanId { get; set; }

    /// <summary>
    /// Whether this log has exception info.
    /// </summary>
    public bool HasException { get; set; }

    /// <summary>
    /// Exception information (if any).
    /// </summary>
    public ExceptionResponse? Exception { get; set; }

    /// <summary>
    /// Structured properties.
    /// </summary>
    public Dictionary<string, object>? Properties { get; set; }

    /// <summary>
    /// Tags.
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Environment.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Application version.
    /// </summary>
    public string? ApplicationVersion { get; set; }
}

/// <summary>
/// Exception information response.
/// </summary>
public class ExceptionResponse
{
    /// <summary>
    /// Exception type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Exception message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Stack trace.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Source.
    /// </summary>
    public string? Source { get; set; }
}

/// <summary>
/// Detailed log entry response (includes related logs).
/// </summary>
public class LogEntryDetailResponse : LogEntryResponse
{
    /// <summary>
    /// Related logs (same correlation ID).
    /// </summary>
    public List<LogEntryResponse> RelatedLogs { get; set; } = new();

    /// <summary>
    /// Server received timestamp.
    /// </summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// Client IP address.
    /// </summary>
    public string? ClientIp { get; set; }
}

/// <summary>
/// Log ingestion result.
/// </summary>
public class LogIngestionResult
{
    /// <summary>
    /// Log entry ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Server received timestamp.
    /// </summary>
    public DateTime ReceivedAt { get; set; }
}

/// <summary>
/// Batch log ingestion result.
/// </summary>
public class BatchLogResult
{
    /// <summary>
    /// Number of logs accepted.
    /// </summary>
    public int Accepted { get; set; }

    /// <summary>
    /// Number of logs rejected.
    /// </summary>
    public int Rejected { get; set; }

    /// <summary>
    /// Errors by index.
    /// </summary>
    public Dictionary<int, string>? Errors { get; set; }
}

/// <summary>
/// Log statistics response.
/// </summary>
public class LogStatisticsResponse
{
    /// <summary>
    /// Summary counts.
    /// </summary>
    public LogSummary Summary { get; set; } = new();

    /// <summary>
    /// Trend data.
    /// </summary>
    public List<LogTrendPoint> Trend { get; set; } = new();

    /// <summary>
    /// Top exception types.
    /// </summary>
    public List<ExceptionSummary> TopExceptions { get; set; } = new();

    /// <summary>
    /// Logs by job.
    /// </summary>
    public List<JobLogSummary> LogsByJob { get; set; } = new();
}

/// <summary>
/// Log count summary.
/// </summary>
public class LogSummary
{
    /// <summary>
    /// Total log count.
    /// </summary>
    public long TotalLogs { get; set; }

    /// <summary>
    /// Trace count.
    /// </summary>
    public long TraceCount { get; set; }

    /// <summary>
    /// Debug count.
    /// </summary>
    public long DebugCount { get; set; }

    /// <summary>
    /// Information count.
    /// </summary>
    public long InfoCount { get; set; }

    /// <summary>
    /// Warning count.
    /// </summary>
    public long WarningCount { get; set; }

    /// <summary>
    /// Error count.
    /// </summary>
    public long ErrorCount { get; set; }

    /// <summary>
    /// Critical count.
    /// </summary>
    public long CriticalCount { get; set; }

    /// <summary>
    /// Exception count.
    /// </summary>
    public long ExceptionCount { get; set; }

    /// <summary>
    /// Unique jobs.
    /// </summary>
    public int UniqueJobs { get; set; }

    /// <summary>
    /// Unique executions.
    /// </summary>
    public int UniqueExecutions { get; set; }

    /// <summary>
    /// First log timestamp.
    /// </summary>
    public DateTime? FirstLog { get; set; }

    /// <summary>
    /// Last log timestamp.
    /// </summary>
    public DateTime? LastLog { get; set; }
}

/// <summary>
/// Log trend data point.
/// </summary>
public class LogTrendPoint
{
    /// <summary>
    /// Time bucket.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Total count.
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// Error count.
    /// </summary>
    public long ErrorCount { get; set; }

    /// <summary>
    /// Warning count.
    /// </summary>
    public long WarningCount { get; set; }

    /// <summary>
    /// Info count.
    /// </summary>
    public long InfoCount { get; set; }
}

/// <summary>
/// Exception type summary.
/// </summary>
public class ExceptionSummary
{
    /// <summary>
    /// Exception type name.
    /// </summary>
    public string ExceptionType { get; set; } = string.Empty;

    /// <summary>
    /// Occurrence count.
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// Number of affected jobs.
    /// </summary>
    public int AffectedJobs { get; set; }

    /// <summary>
    /// Last occurrence.
    /// </summary>
    public DateTime LastOccurrence { get; set; }

    /// <summary>
    /// First occurrence.
    /// </summary>
    public DateTime FirstOccurrence { get; set; }
}

/// <summary>
/// Job log summary.
/// </summary>
public class JobLogSummary
{
    /// <summary>
    /// Job ID.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Job display name.
    /// </summary>
    public string JobDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Total logs.
    /// </summary>
    public long TotalLogs { get; set; }

    /// <summary>
    /// Error count.
    /// </summary>
    public long ErrorCount { get; set; }

    /// <summary>
    /// Warning count.
    /// </summary>
    public long WarningCount { get; set; }
}
