namespace FMSLogNexus.Client;

/// <summary>
/// Configuration options for FMS Log Nexus client.
/// </summary>
public class FMSLogNexusOptions
{
    /// <summary>
    /// The base URL of the FMS Log Nexus API.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// The server name to use for this agent.
    /// </summary>
    public string ServerName { get; set; } = Environment.MachineName;

    /// <summary>
    /// The API key for authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// JWT access token for authentication.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// The agent version to report.
    /// </summary>
    public string AgentVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Interval in seconds between heartbeats.
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Interval in seconds between log flushes.
    /// </summary>
    public int LogFlushIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Maximum number of logs to batch before sending.
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Timeout in seconds for HTTP requests.
    /// </summary>
    public int HttpTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts for failed requests.
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay in milliseconds between retry attempts.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Enable real-time updates via SignalR.
    /// </summary>
    public bool EnableRealTimeUpdates { get; set; } = false;

    /// <summary>
    /// Enable automatic heartbeat sending.
    /// </summary>
    public bool EnableAutoHeartbeat { get; set; } = true;

    /// <summary>
    /// Enable buffered log sending.
    /// </summary>
    public bool EnableLogBuffering { get; set; } = true;
}

/// <summary>
/// Log level enumeration matching server-side levels.
/// </summary>
public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5
}

/// <summary>
/// Execution status enumeration.
/// </summary>
public enum ExecutionStatus
{
    Pending,
    Running,
    Success,
    Failed,
    Cancelled,
    TimedOut
}

/// <summary>
/// Server status enumeration.
/// </summary>
public enum ServerStatus
{
    Unknown,
    Online,
    Offline,
    Maintenance,
    Error
}

/// <summary>
/// Job priority enumeration.
/// </summary>
public enum JobPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// Trigger type for job executions.
/// </summary>
public enum TriggerType
{
    Manual,
    Scheduled,
    Triggered,
    Retry
}
