using System.Text.Json.Serialization;

namespace FMSLogNexus.Client.Models;

#region Log Models

/// <summary>
/// Request model for creating a log entry.
/// </summary>
public class CreateLogRequest
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("level")]
    public LogLevel Level { get; set; } = LogLevel.Information;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("serverName")]
    public string? ServerName { get; set; }

    [JsonPropertyName("jobId")]
    public string? JobId { get; set; }

    [JsonPropertyName("executionId")]
    public Guid? ExecutionId { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("exception")]
    public string? Exception { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, object>? Properties { get; set; }
}

/// <summary>
/// Response model for a log entry.
/// </summary>
public class LogResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("level")]
    public LogLevel Level { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("serverName")]
    public string? ServerName { get; set; }

    [JsonPropertyName("jobId")]
    public string? JobId { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response model for batch log creation.
/// </summary>
public class BatchLogResponse
{
    [JsonPropertyName("totalReceived")]
    public int TotalReceived { get; set; }

    [JsonPropertyName("successCount")]
    public int SuccessCount { get; set; }

    [JsonPropertyName("failedCount")]
    public int FailedCount { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
}

#endregion

#region Server Models

/// <summary>
/// Request model for server heartbeat.
/// </summary>
public class HeartbeatRequest
{
    [JsonPropertyName("serverName")]
    public string ServerName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public ServerStatus Status { get; set; } = ServerStatus.Online;

    [JsonPropertyName("agentVersion")]
    public string? AgentVersion { get; set; }

    [JsonPropertyName("systemInfo")]
    public SystemInfo? SystemInfo { get; set; }
}

/// <summary>
/// System information for heartbeat.
/// </summary>
public class SystemInfo
{
    [JsonPropertyName("os")]
    public string? Os { get; set; }

    [JsonPropertyName("cpuUsage")]
    public double? CpuUsage { get; set; }

    [JsonPropertyName("memoryUsage")]
    public double? MemoryUsage { get; set; }

    [JsonPropertyName("diskUsage")]
    public double? DiskUsage { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, object>? Properties { get; set; }
}

/// <summary>
/// Response model for server details.
/// </summary>
public class ServerResponse
{
    [JsonPropertyName("serverName")]
    public string ServerName { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("status")]
    public ServerStatus Status { get; set; }

    [JsonPropertyName("agentVersion")]
    public string? AgentVersion { get; set; }

    [JsonPropertyName("lastHeartbeat")]
    public DateTime? LastHeartbeat { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}

#endregion

#region Job Models

/// <summary>
/// Request model for registering a job.
/// </summary>
public class RegisterJobRequest
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("serverName")]
    public string ServerName { get; set; } = string.Empty;

    [JsonPropertyName("schedule")]
    public string? Schedule { get; set; }

    [JsonPropertyName("priority")]
    public JobPriority Priority { get; set; } = JobPriority.Normal;

    [JsonPropertyName("timeoutMinutes")]
    public int? TimeoutMinutes { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Response model for job details.
/// </summary>
public class JobResponse
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("serverName")]
    public string ServerName { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public JobPriority Priority { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("lastExecutionAt")]
    public DateTime? LastExecutionAt { get; set; }

    [JsonPropertyName("lastExecutionStatus")]
    public ExecutionStatus? LastExecutionStatus { get; set; }
}

#endregion

#region Execution Models

/// <summary>
/// Request model for starting an execution.
/// </summary>
public class StartExecutionRequest
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("serverName")]
    public string ServerName { get; set; } = string.Empty;

    [JsonPropertyName("triggerType")]
    public TriggerType TriggerType { get; set; } = TriggerType.Manual;

    [JsonPropertyName("triggeredBy")]
    public string? TriggeredBy { get; set; }

    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }
}

/// <summary>
/// Request model for completing an execution.
/// </summary>
public class CompleteExecutionRequest
{
    [JsonPropertyName("status")]
    public ExecutionStatus Status { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("outputMessage")]
    public string? OutputMessage { get; set; }
}

/// <summary>
/// Response model for execution details.
/// </summary>
public class ExecutionResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("serverName")]
    public string ServerName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public ExecutionStatus Status { get; set; }

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }

    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [JsonPropertyName("durationMs")]
    public long? DurationMs { get; set; }

    [JsonPropertyName("triggerType")]
    public TriggerType TriggerType { get; set; }
}

#endregion

#region Auth Models

/// <summary>
/// Request model for login.
/// </summary>
public class LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Response model for authentication.
/// </summary>
public class AuthResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }
}

#endregion

#region Common Models

/// <summary>
/// Paginated response wrapper.
/// </summary>
public class PagedResponse<T>
{
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = new();

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
}

/// <summary>
/// API error response.
/// </summary>
public class ApiError
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; set; }
}

#endregion
