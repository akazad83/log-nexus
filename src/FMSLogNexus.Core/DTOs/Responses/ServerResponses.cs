using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.DTOs.Responses;

/// <summary>
/// Server response DTO.
/// </summary>
public class ServerResponse
{
    /// <summary>
    /// Server name (unique identifier).
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Server description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Server status.
    /// </summary>
    public ServerStatus Status { get; set; }

    /// <summary>
    /// Status name for display.
    /// </summary>
    public string StatusName => Status.ToString();

    /// <summary>
    /// Whether the server is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// IP address.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Agent version.
    /// </summary>
    public string? AgentVersion { get; set; }

    /// <summary>
    /// Agent type.
    /// </summary>
    public string? AgentType { get; set; }

    /// <summary>
    /// Last heartbeat timestamp.
    /// </summary>
    public DateTime? LastHeartbeat { get; set; }

    /// <summary>
    /// Heartbeat interval in seconds.
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; }

    /// <summary>
    /// Server metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Detailed server response with related data.
/// </summary>
public class ServerDetailResponse
{
    /// <summary>
    /// Server information.
    /// </summary>
    public ServerResponse Server { get; set; } = new();

    /// <summary>
    /// Jobs on this server.
    /// </summary>
    public List<ServerJobInfo> Jobs { get; set; } = new();

    /// <summary>
    /// Currently running executions.
    /// </summary>
    public List<RunningExecutionInfo> RunningExecutions { get; set; } = new();
}

/// <summary>
/// Server job information.
/// </summary>
public class ServerJobInfo
{
    /// <summary>
    /// Job ID.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Last execution status.
    /// </summary>
    public JobStatus? LastStatus { get; set; }

    /// <summary>
    /// Last execution timestamp.
    /// </summary>
    public DateTime? LastExecutionAt { get; set; }

    /// <summary>
    /// Whether the job is active.
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Running execution information.
/// </summary>
public class RunningExecutionInfo
{
    /// <summary>
    /// Execution ID.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Job ID.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Job display name.
    /// </summary>
    public string? JobDisplayName { get; set; }

    /// <summary>
    /// Start timestamp.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Running duration in milliseconds.
    /// </summary>
    public long RunningMs { get; set; }
}

/// <summary>
/// Server status summary for dashboard.
/// </summary>
public class ServerStatusSummary
{
    /// <summary>
    /// Total server count.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Online server count.
    /// </summary>
    public int Online { get; set; }

    /// <summary>
    /// Offline server count.
    /// </summary>
    public int Offline { get; set; }

    /// <summary>
    /// Degraded server count.
    /// </summary>
    public int Degraded { get; set; }

    /// <summary>
    /// Unknown status server count.
    /// </summary>
    public int Unknown { get; set; }

    /// <summary>
    /// Maintenance mode server count.
    /// </summary>
    public int Maintenance { get; set; }
}

/// <summary>
/// Audit log response.
/// </summary>
public class AuditLogResponse
{
    /// <summary>
    /// Audit log ID.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// User ID who performed the action.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Username who performed the action.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Action performed.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Entity type affected.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Entity ID affected.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Old values before change.
    /// </summary>
    public Dictionary<string, object>? OldValues { get; set; }

    /// <summary>
    /// New values after change.
    /// </summary>
    public Dictionary<string, object>? NewValues { get; set; }

    /// <summary>
    /// IP address of the client.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string.
    /// </summary>
    public string? UserAgent { get; set; }
}

/// <summary>
/// Configuration item response.
/// </summary>
public class ConfigurationItemResponse
{
    /// <summary>
    /// Configuration key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Configuration value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Configuration category.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Data type.
    /// </summary>
    public string? DataType { get; set; }

    /// <summary>
    /// Whether the value is encrypted.
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// Last updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// User who last updated.
    /// </summary>
    public string? UpdatedBy { get; set; }
}
