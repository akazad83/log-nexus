using System.ComponentModel.DataAnnotations;
using FMSLogNexus.Core.DTOs;

namespace FMSLogNexus.Core.DTOs.Requests;

/// <summary>
/// Request to update server heartbeat.
/// </summary>
public class ServerHeartbeatRequest
{
    /// <summary>
    /// Server name.
    /// </summary>
    [Required(ErrorMessage = "Server name is required")]
    [StringLength(100)]
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// Agent version.
    /// </summary>
    [StringLength(50)]
    public string? AgentVersion { get; set; }

    /// <summary>
    /// Agent type.
    /// </summary>
    [StringLength(50)]
    public string? AgentType { get; set; }

    /// <summary>
    /// Server metadata (OS version, CPU, memory, etc.).
    /// </summary>
    public ServerMetadataRequest? Metadata { get; set; }
}

/// <summary>
/// Server metadata for heartbeat.
/// </summary>
public class ServerMetadataRequest
{
    /// <summary>
    /// Operating system version.
    /// </summary>
    [StringLength(200)]
    public string? OsVersion { get; set; }

    /// <summary>
    /// CPU usage percentage.
    /// </summary>
    [Range(0, 100)]
    public decimal? CpuUsage { get; set; }

    /// <summary>
    /// Memory usage percentage.
    /// </summary>
    [Range(0, 100)]
    public decimal? MemoryUsage { get; set; }

    /// <summary>
    /// Disk usage percentage.
    /// </summary>
    [Range(0, 100)]
    public decimal? DiskUsage { get; set; }

    /// <summary>
    /// Number of running jobs.
    /// </summary>
    [Range(0, 1000)]
    public int? RunningJobs { get; set; }

    /// <summary>
    /// Custom properties.
    /// </summary>
    public Dictionary<string, object>? Custom { get; set; }
}

/// <summary>
/// Request to create/update a server.
/// </summary>
public class CreateServerRequest
{
    /// <summary>
    /// Server name.
    /// </summary>
    [Required(ErrorMessage = "Server name is required")]
    [StringLength(100)]
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    [StringLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Description.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// IP address.
    /// </summary>
    [StringLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Heartbeat interval in seconds.
    /// </summary>
    [Range(10, 300)]
    public int? HeartbeatIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Server metadata.
    /// </summary>
    public ServerMetadataRequest? Metadata { get; set; }
}

/// <summary>
/// Request to update a server.
/// </summary>
public class UpdateServerRequest
{
    /// <summary>
    /// Display name.
    /// </summary>
    [StringLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Description.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether the server is active.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Heartbeat interval in seconds.
    /// </summary>
    [Range(10, 300)]
    public int? HeartbeatIntervalSeconds { get; set; }

    /// <summary>
    /// IP address.
    /// </summary>
    [StringLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Server metadata.
    /// </summary>
    public ServerMetadataRequest? Metadata { get; set; }
}

/// <summary>
/// Heartbeat request for server health check.
/// </summary>
public class HeartbeatRequest
{
    /// <summary>
    /// Server name.
    /// </summary>
    [Required(ErrorMessage = "Server name is required")]
    [StringLength(100)]
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// IP address.
    /// </summary>
    [StringLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Agent version.
    /// </summary>
    [StringLength(50)]
    public string? AgentVersion { get; set; }

    /// <summary>
    /// Server metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Request to search audit logs.
/// </summary>
public class AuditSearchRequest : PaginationRequest
{
    /// <summary>
    /// Filter by start date.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Filter by end date.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by user ID.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Filter by username.
    /// </summary>
    [StringLength(100)]
    public string? Username { get; set; }

    /// <summary>
    /// Filter by action.
    /// </summary>
    [StringLength(100)]
    public string? Action { get; set; }

    /// <summary>
    /// Filter by entity type.
    /// </summary>
    [StringLength(100)]
    public string? EntityType { get; set; }

    /// <summary>
    /// Filter by entity ID.
    /// </summary>
    [StringLength(100)]
    public string? EntityId { get; set; }
}

/// <summary>
/// Request to create a configuration item.
/// </summary>
public class CreateConfigurationRequest
{
    /// <summary>
    /// Configuration key.
    /// </summary>
    [Required(ErrorMessage = "Key is required")]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Configuration value.
    /// </summary>
    [Required(ErrorMessage = "Value is required")]
    public object Value { get; set; } = string.Empty;

    /// <summary>
    /// Data type.
    /// </summary>
    [StringLength(50)]
    public string DataType { get; set; } = "string";

    /// <summary>
    /// Category for grouping.
    /// </summary>
    [StringLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Description.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether the value is encrypted.
    /// </summary>
    public bool IsEncrypted { get; set; }
}

/// <summary>
/// Request to update system configuration.
/// </summary>
public class UpdateConfigurationRequest
{
    /// <summary>
    /// Configuration values to update.
    /// </summary>
    [Required]
    public Dictionary<string, object> Values { get; set; } = new();
}

/// <summary>
/// Request for dashboard data.
/// </summary>
public class DashboardRequest
{
    /// <summary>
    /// Time period for data.
    /// </summary>
    public Enums.TimePeriod Period { get; set; } = Enums.TimePeriod.Last24Hours;

    /// <summary>
    /// Trend interval.
    /// </summary>
    public Enums.TrendInterval Interval { get; set; } = Enums.TrendInterval.Hour;

    /// <summary>
    /// Filter by server name.
    /// </summary>
    public string? ServerName { get; set; }
}

/// <summary>
/// Request to update user preferences.
/// </summary>
public class UpdatePreferencesRequest
{
    /// <summary>
    /// UI theme (light/dark).
    /// </summary>
    [StringLength(20)]
    public string? Theme { get; set; }

    /// <summary>
    /// Timezone.
    /// </summary>
    [StringLength(50)]
    public string? Timezone { get; set; }

    /// <summary>
    /// Dashboard refresh interval in seconds.
    /// </summary>
    [Range(5, 300)]
    public int? RefreshInterval { get; set; }

    /// <summary>
    /// Default page size.
    /// </summary>
    [Range(10, 200)]
    public int? DefaultPageSize { get; set; }

    /// <summary>
    /// Default log level filter.
    /// </summary>
    public Enums.LogLevel? DefaultLogLevel { get; set; }

    /// <summary>
    /// Email notifications enabled.
    /// </summary>
    public bool? EmailNotifications { get; set; }
}
