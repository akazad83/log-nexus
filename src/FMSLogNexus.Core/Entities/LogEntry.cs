using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Entities;

/// <summary>
/// Represents a single log entry in the system.
/// Maps to [log].[LogEntries] table.
/// </summary>
public class LogEntry : EntityBase
{
    /// <summary>
    /// UTC timestamp when the log was generated at the source.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Log level (Trace=0, Debug=1, Information=2, Warning=3, Error=4, Critical=5).
    /// </summary>
    public LogLevel Level { get; set; } = LogLevel.Information;

    /// <summary>
    /// The log message text.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The message template with placeholders (e.g., "Processing {RecordCount} records").
    /// </summary>
    public string? MessageTemplate { get; set; }

    /// <summary>
    /// The unique identifier of the job that generated this log.
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// The execution instance ID if this log is part of a job execution.
    /// </summary>
    public Guid? JobExecutionId { get; set; }

    /// <summary>
    /// The server/machine name where the log originated.
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// The logging category (typically the class or namespace).
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// The source context (typically the full class name).
    /// </summary>
    public string? SourceContext { get; set; }

    /// <summary>
    /// Correlation ID for tracking related operations across services.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// W3C Trace ID for distributed tracing.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// W3C Span ID for distributed tracing.
    /// </summary>
    public string? SpanId { get; set; }

    /// <summary>
    /// Parent Span ID for distributed tracing.
    /// </summary>
    public string? ParentSpanId { get; set; }

    /// <summary>
    /// Exception type name if an exception was logged.
    /// </summary>
    public string? ExceptionType { get; set; }

    /// <summary>
    /// Exception message if an exception was logged.
    /// </summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>
    /// Exception stack trace if an exception was logged.
    /// </summary>
    public string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// Exception source if an exception was logged.
    /// </summary>
    public string? ExceptionSource { get; set; }

    /// <summary>
    /// Additional structured properties as JSON.
    /// </summary>
    public string? Properties { get; set; }

    /// <summary>
    /// Comma-separated tags for categorization.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Environment name (Development, Staging, Production).
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Application version that generated the log.
    /// </summary>
    public string? ApplicationVersion { get; set; }

    /// <summary>
    /// UTC timestamp when the log was received by the server.
    /// </summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address of the client that sent the log.
    /// </summary>
    public string? ClientIp { get; set; }

    // -------------------------------------------------------------------------
    // Computed Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// Indicates whether this log entry contains exception information.
    /// </summary>
    public bool HasException => !string.IsNullOrEmpty(ExceptionType);

    /// <summary>
    /// Gets the log level as a string name.
    /// </summary>
    public string LevelName => Level.ToString();

    /// <summary>
    /// Indicates whether this is an error-level log (Warning or above).
    /// </summary>
    public bool IsError => Level >= LogLevel.Warning;

    /// <summary>
    /// Indicates whether this is a critical log.
    /// </summary>
    public bool IsCritical => Level == LogLevel.Critical;

    // -------------------------------------------------------------------------
    // Navigation Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// The job that generated this log entry.
    /// </summary>
    public virtual Job? Job { get; set; }

    /// <summary>
    /// The job execution this log belongs to.
    /// </summary>
    public virtual JobExecution? JobExecution { get; set; }

    /// <summary>
    /// The server that generated this log entry.
    /// </summary>
    public virtual Server? Server { get; set; }

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets the properties as a dictionary.
    /// </summary>
    public Dictionary<string, object>? GetPropertiesAsDictionary()
    {
        if (string.IsNullOrEmpty(Properties))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Properties);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the tags as a list.
    /// </summary>
    public List<string> GetTagsList()
    {
        if (string.IsNullOrEmpty(Tags))
            return new List<string>();

        return Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .ToList();
    }

    /// <summary>
    /// Sets the exception details from an Exception object.
    /// </summary>
    public void SetException(Exception? exception)
    {
        if (exception == null)
        {
            ExceptionType = null;
            ExceptionMessage = null;
            ExceptionStackTrace = null;
            ExceptionSource = null;
            return;
        }

        ExceptionType = exception.GetType().FullName;
        ExceptionMessage = exception.Message;
        ExceptionStackTrace = exception.StackTrace;
        ExceptionSource = exception.Source;

        // Include inner exception info if present
        if (exception.InnerException != null)
        {
            ExceptionMessage += $" ---> {exception.InnerException.GetType().Name}: {exception.InnerException.Message}";
        }
    }
}
