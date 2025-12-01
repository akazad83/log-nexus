using System.ComponentModel.DataAnnotations;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.DTOs.Requests;

/// <summary>
/// Request to create a single log entry.
/// </summary>
public class CreateLogRequest
{
    /// <summary>
    /// UTC timestamp when the log was generated.
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Log level.
    /// </summary>
    [Required]
    public LogLevel Level { get; set; } = LogLevel.Information;

    /// <summary>
    /// Log message.
    /// </summary>
    [Required(ErrorMessage = "Message is required")]
    [StringLength(8000, ErrorMessage = "Message cannot exceed 8000 characters")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Message template with placeholders.
    /// </summary>
    [StringLength(2000)]
    public string? MessageTemplate { get; set; }

    /// <summary>
    /// Job ID.
    /// </summary>
    [StringLength(100)]
    public string? JobId { get; set; }

    /// <summary>
    /// Job execution ID.
    /// </summary>
    public Guid? JobExecutionId { get; set; }

    /// <summary>
    /// Server name.
    /// </summary>
    [StringLength(100)]
    public string? ServerName { get; set; }

    /// <summary>
    /// Category (class/namespace).
    /// </summary>
    [StringLength(500)]
    public string? Category { get; set; }

    /// <summary>
    /// Source context.
    /// </summary>
    [StringLength(500)]
    public string? SourceContext { get; set; }

    /// <summary>
    /// Correlation ID for tracking.
    /// </summary>
    [StringLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Trace ID for distributed tracing.
    /// </summary>
    [StringLength(64)]
    public string? TraceId { get; set; }

    /// <summary>
    /// Span ID for distributed tracing.
    /// </summary>
    [StringLength(32)]
    public string? SpanId { get; set; }

    /// <summary>
    /// Parent span ID.
    /// </summary>
    [StringLength(32)]
    public string? ParentSpanId { get; set; }

    /// <summary>
    /// Exception information.
    /// </summary>
    public ExceptionInfo? Exception { get; set; }

    /// <summary>
    /// Additional structured properties.
    /// </summary>
    public Dictionary<string, object>? Properties { get; set; }

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Environment name.
    /// </summary>
    [StringLength(50)]
    public string? Environment { get; set; }

    /// <summary>
    /// Application version.
    /// </summary>
    [StringLength(50)]
    public string? ApplicationVersion { get; set; }
}

/// <summary>
/// Exception information for log entries.
/// </summary>
public class ExceptionInfo
{
    /// <summary>
    /// Exception type name.
    /// </summary>
    [StringLength(500)]
    public string? Type { get; set; }

    /// <summary>
    /// Exception message.
    /// </summary>
    [StringLength(4000)]
    public string? Message { get; set; }

    /// <summary>
    /// Stack trace.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Exception source.
    /// </summary>
    [StringLength(500)]
    public string? Source { get; set; }

    /// <summary>
    /// Inner exception.
    /// </summary>
    public ExceptionInfo? InnerException { get; set; }
}

/// <summary>
/// Request to create multiple log entries.
/// </summary>
public class BatchLogRequest
{
    /// <summary>
    /// Collection of log entries (max 1000).
    /// </summary>
    [Required]
    [MaxLength(1000, ErrorMessage = "Maximum 1000 logs per batch")]
    public List<CreateLogRequest> Logs { get; set; } = new();
}

/// <summary>
/// Request to search/filter logs.
/// </summary>
public class LogSearchRequest : PaginationRequest
{
    /// <summary>
    /// Start date for the search range.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for the search range.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by job ID.
    /// </summary>
    [StringLength(100)]
    public string? JobId { get; set; }

    /// <summary>
    /// Filter by job execution ID.
    /// </summary>
    public Guid? JobExecutionId { get; set; }

    /// <summary>
    /// Filter by server name.
    /// </summary>
    [StringLength(100)]
    public string? ServerName { get; set; }

    /// <summary>
    /// Minimum log level (inclusive).
    /// </summary>
    public LogLevel? MinLevel { get; set; }

    /// <summary>
    /// Maximum log level (inclusive).
    /// </summary>
    public LogLevel? MaxLevel { get; set; }

    /// <summary>
    /// Full-text search in message.
    /// </summary>
    [StringLength(500)]
    public string? Search { get; set; }

    /// <summary>
    /// Filter by exception type.
    /// </summary>
    [StringLength(500)]
    public string? ExceptionType { get; set; }

    /// <summary>
    /// Filter by correlation ID.
    /// </summary>
    [StringLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Filter to only logs with exceptions.
    /// </summary>
    public bool? HasException { get; set; }

    /// <summary>
    /// Filter by tags (any match).
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Filter by category.
    /// </summary>
    [StringLength(500)]
    public string? Category { get; set; }

    /// <summary>
    /// Filter by environment.
    /// </summary>
    [StringLength(50)]
    public string? Environment { get; set; }
}

/// <summary>
/// Request to export logs.
/// </summary>
public class LogExportRequest : LogSearchRequest
{
    /// <summary>
    /// Export format.
    /// </summary>
    public ExportFormat Format { get; set; } = ExportFormat.Json;

    /// <summary>
    /// Maximum number of records to export.
    /// </summary>
    [Range(1, 100000)]
    public int MaxRecords { get; set; } = 10000;

    /// <summary>
    /// Fields to include in export.
    /// </summary>
    public List<string>? Fields { get; set; }
}

/// <summary>
/// Request for log statistics.
/// </summary>
public class LogStatisticsRequest : DateRangeFilter
{
    /// <summary>
    /// Filter by job ID.
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// Filter by server name.
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// Interval for trend data.
    /// </summary>
    public TrendInterval Interval { get; set; } = TrendInterval.Hour;
}
