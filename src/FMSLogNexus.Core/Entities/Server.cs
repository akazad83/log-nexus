using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.Entities;

/// <summary>
/// Represents a server/machine that runs jobs and sends logs.
/// Maps to [job].[Servers] table.
/// </summary>
public class Server : IEntity<string>, IModificationAuditable, IActivatable
{
    /// <summary>
    /// Server machine name (primary key).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Alias for Id to maintain naming consistency.
    /// </summary>
    public string ServerName
    {
        get => Id;
        set => Id = value;
    }

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Server description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// IP address of the server.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Current server status.
    /// </summary>
    public ServerStatus Status { get; set; } = ServerStatus.Unknown;

    /// <summary>
    /// Last heartbeat timestamp from the server.
    /// </summary>
    public DateTime? LastHeartbeat { get; set; }

    /// <summary>
    /// Expected heartbeat interval in seconds.
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Version of the log client/agent installed.
    /// </summary>
    public string? AgentVersion { get; set; }

    /// <summary>
    /// Type of agent (LogClient, FileCollector, EventCollector).
    /// </summary>
    public string? AgentType { get; set; }

    /// <summary>
    /// Whether the server is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }

    // -------------------------------------------------------------------------
    // Audit Fields
    // -------------------------------------------------------------------------

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }

    // -------------------------------------------------------------------------
    // Navigation Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// Jobs configured to run on this server.
    /// </summary>
    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();

    /// <summary>
    /// Job executions that ran on this server.
    /// </summary>
    public virtual ICollection<JobExecution> Executions { get; set; } = new List<JobExecution>();

    /// <summary>
    /// Log entries from this server.
    /// </summary>
    public virtual ICollection<LogEntry> LogEntries { get; set; } = new List<LogEntry>();

    /// <summary>
    /// API keys associated with this server.
    /// </summary>
    public virtual ICollection<UserApiKey> ApiKeys { get; set; } = new List<UserApiKey>();

    /// <summary>
    /// Alerts configured for this server.
    /// </summary>
    public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();

    // -------------------------------------------------------------------------
    // Computed Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// Seconds since last heartbeat.
    /// </summary>
    public int? SecondsSinceHeartbeat => LastHeartbeat.HasValue
        ? (int)(DateTime.UtcNow - LastHeartbeat.Value).TotalSeconds
        : null;

    /// <summary>
    /// Calculated status based on heartbeat.
    /// </summary>
    public ServerStatus CalculatedStatus
    {
        get
        {
            if (!LastHeartbeat.HasValue)
                return ServerStatus.Unknown;

            var secondsSince = SecondsSinceHeartbeat!.Value;
            var timeout = HeartbeatIntervalSeconds * 3;
            var warningThreshold = HeartbeatIntervalSeconds * 2;

            if (secondsSince > timeout)
                return ServerStatus.Offline;

            if (secondsSince > warningThreshold)
                return ServerStatus.Degraded;

            return ServerStatus.Online;
        }
    }

    /// <summary>
    /// Indicates if the server is considered online.
    /// </summary>
    public bool IsOnline => Status == ServerStatus.Online || CalculatedStatus == ServerStatus.Online;

    /// <summary>
    /// Indicates if the server is considered offline.
    /// </summary>
    public bool IsOffline => Status == ServerStatus.Offline || CalculatedStatus == ServerStatus.Offline;

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Updates the heartbeat timestamp and status.
    /// </summary>
    public void UpdateHeartbeat(string? ipAddress = null, string? agentVersion = null, string? agentType = null)
    {
        LastHeartbeat = DateTime.UtcNow;
        Status = ServerStatus.Online;
        UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(ipAddress))
            IpAddress = ipAddress;

        if (!string.IsNullOrEmpty(agentVersion))
            AgentVersion = agentVersion;

        if (!string.IsNullOrEmpty(agentType))
            AgentType = agentType;
    }

    /// <summary>
    /// Marks the server as offline.
    /// </summary>
    public void MarkOffline()
    {
        Status = ServerStatus.Offline;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets metadata as a dictionary.
    /// </summary>
    public Dictionary<string, object>? GetMetadataAsDictionary()
    {
        if (string.IsNullOrEmpty(Metadata))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets metadata from a dictionary.
    /// </summary>
    public void SetMetadata(Dictionary<string, object> metadata)
    {
        Metadata = System.Text.Json.JsonSerializer.Serialize(metadata);
    }
}
