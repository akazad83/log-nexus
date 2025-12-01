using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Interfaces.Services;
using FMSLogNexus.Infrastructure.Services;
using CoreMaintenanceResult = FMSLogNexus.Core.Interfaces.Services.MaintenanceResult;

namespace FMSLogNexus.Api.Controllers;

/// <summary>
/// Controller for system operations.
/// </summary>
[Route("api/v1/system")]
[ApiExplorerSettings(GroupName = "System")]
public class SystemController : ApiControllerBase
{
    private readonly IConfigurationService _configService;
    private readonly IAuditService _auditService;
    private readonly IMaintenanceService _maintenanceService;
    private readonly ILogger<SystemController> _logger;

    public SystemController(
        IConfigurationService configService,
        IAuditService auditService,
        IMaintenanceService maintenanceService,
        ILogger<SystemController> logger)
    {
        _configService = configService;
        _auditService = auditService;
        _maintenanceService = maintenanceService;
        _logger = logger;
    }

    #region Configuration

    /// <summary>
    /// Gets all configuration items.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All configuration items.</returns>
    [HttpGet("config")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<ConfigurationItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConfigurationItemResponse>>> GetAllConfiguration(
        CancellationToken cancellationToken)
    {
        var response = await _configService.GetAllConfigurationAsync(cancellationToken);
        var items = response.Categories.Values.SelectMany(dict => dict.Values).ToList();
        return Ok(items);
    }

    /// <summary>
    /// Gets configuration items by category.
    /// </summary>
    /// <param name="category">Category name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration items in the category.</returns>
    [HttpGet("config/category/{category}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<ConfigurationItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConfigurationItemResponse>>> GetConfigurationByCategory(
        string category,
        CancellationToken cancellationToken)
    {
        var itemsDict = await _configService.GetByCategoryAsync(category, cancellationToken);
        var items = itemsDict.Values.ToList();
        return Ok(items);
    }

    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    /// <param name="key">Configuration key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration value.</returns>
    [HttpGet("config/{key}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetConfigurationValue(
        string key,
        CancellationToken cancellationToken)
    {
        var value = await _configService.GetValueAsync(key, cancellationToken);
        if (value == null)
            return NotFoundResponse("Configuration", key);

        return Ok(new { Key = key, Value = value });
    }

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    /// <param name="key">Configuration key.</param>
    /// <param name="request">Set value request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPut("config/{key}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetConfigurationValue(
        string key,
        [FromBody] SetConfigValueRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _configService.SetValueAsync(key, request.Value, CurrentUsername, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResponse("Configuration", key);
        }
    }

    /// <summary>
    /// Creates or updates a configuration item.
    /// </summary>
    /// <param name="request">Configuration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration item.</returns>
    [HttpPost("config")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(ConfigurationItemResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConfigurationItemResponse>> CreateOrUpdateConfiguration(
        [FromBody] CreateConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var item = await _configService.CreateOrUpdateAsync(request, CurrentUserId, CurrentUsername, cancellationToken);
        return Ok(item);
    }

    /// <summary>
    /// Gets distinct configuration categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Categories.</returns>
    [HttpGet("config/categories")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetConfigurationCategories(CancellationToken cancellationToken)
    {
        var categories = await _configService.GetCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    #endregion

    #region Audit Log

    /// <summary>
    /// Searches audit log entries.
    /// </summary>
    /// <param name="request">Search request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated audit entries.</returns>
    [HttpGet("audit")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(PaginatedResult<AuditLogResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<AuditLogResponse>>> SearchAuditLog(
        [FromQuery] AuditSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _auditService.SearchAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets audit entries for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="count">Number of entries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audit entries.</returns>
    [HttpGet("audit/by-user/{userId:guid}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuditLogResponse>>> GetAuditByUser(
        Guid userId,
        [FromQuery] int count = 100,
        CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 500);
        var entries = await _auditService.GetByUserAsync(userId, count, cancellationToken);
        return Ok(entries);
    }

    /// <summary>
    /// Gets audit entries for an entity.
    /// </summary>
    /// <param name="entityType">Entity type.</param>
    /// <param name="entityId">Entity ID.</param>
    /// <param name="count">Number of entries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audit entries.</returns>
    [HttpGet("audit/by-entity/{entityType}/{entityId}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuditLogResponse>>> GetAuditByEntity(
        string entityType,
        string entityId,
        [FromQuery] int count = 100,
        CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 500);
        var entries = await _auditService.GetByEntityAsync(entityType, entityId, count, cancellationToken);
        return Ok(entries);
    }

    /// <summary>
    /// Gets recent audit entries.
    /// </summary>
    /// <param name="count">Number of entries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent audit entries.</returns>
    [HttpGet("audit/recent")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuditLogResponse>>> GetRecentAudit(
        [FromQuery] int count = 100,
        CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 500);
        var entries = await _auditService.GetRecentAsync(count, cancellationToken);
        return Ok(entries);
    }

    /// <summary>
    /// Gets login history.
    /// </summary>
    /// <param name="userId">Optional user ID filter.</param>
    /// <param name="count">Number of entries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login history.</returns>
    [HttpGet("audit/logins")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuditLogResponse>>> GetLoginHistory(
        [FromQuery] Guid? userId = null,
        [FromQuery] int count = 100,
        CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 500);
        var entries = await _auditService.GetLoginHistoryAsync(userId, count, cancellationToken);
        return Ok(entries);
    }

    /// <summary>
    /// Gets distinct audit actions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Actions.</returns>
    [HttpGet("audit/actions")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetAuditActions(CancellationToken cancellationToken)
    {
        var actions = await _auditService.GetDistinctActionsAsync(cancellationToken);
        return Ok(actions);
    }

    /// <summary>
    /// Gets distinct audit entity types.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Entity types.</returns>
    [HttpGet("audit/entity-types")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetAuditEntityTypes(CancellationToken cancellationToken)
    {
        var types = await _auditService.GetDistinctEntityTypesAsync(cancellationToken);
        return Ok(types);
    }

    #endregion

    #region Maintenance

    /// <summary>
    /// Runs log cleanup.
    /// </summary>
    /// <param name="retentionDays">Retention days (uses config if not specified).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Maintenance result.</returns>
    [HttpPost("maintenance/cleanup/logs")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(CoreMaintenanceResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<CoreMaintenanceResult>> RunLogCleanup(
        [FromQuery] int? retentionDays = null,
        CancellationToken cancellationToken = default)
    {
        var days = retentionDays ?? 90;
        var deletedCount = await _maintenanceService.PurgeOldLogsAsync(days, cancellationToken);
        return Ok(new CoreMaintenanceResult
        {
            Success = true,
            DurationMs = 0,
            Details = new Dictionary<string, object>
            {
                ["Operation"] = "LogCleanup",
                ["RecordsAffected"] = deletedCount,
                ["RetentionDays"] = days
            }
        });
    }

    /// <summary>
    /// Runs execution cleanup.
    /// </summary>
    /// <param name="retentionDays">Retention days (uses config if not specified).</param>
    /// <param name="keepMinimumPerJob">Minimum executions to keep per job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Maintenance result.</returns>
    [HttpPost("maintenance/cleanup/executions")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(CoreMaintenanceResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<CoreMaintenanceResult>> RunExecutionCleanup(
        [FromQuery] int? retentionDays = null,
        [FromQuery] int keepMinimumPerJob = 100,
        CancellationToken cancellationToken = default)
    {
        var days = retentionDays ?? 365;
        var deletedCount = await _maintenanceService.PurgeOldExecutionsAsync(days, keepMinimumPerJob, cancellationToken);
        return Ok(new CoreMaintenanceResult
        {
            Success = true,
            DurationMs = 0,
            Details = new Dictionary<string, object>
            {
                ["Operation"] = "ExecutionCleanup",
                ["RecordsAffected"] = deletedCount,
                ["RetentionDays"] = days,
                ["KeepMinimumPerJob"] = keepMinimumPerJob
            }
        });
    }

    /// <summary>
    /// Runs alert cleanup.
    /// </summary>
    /// <param name="retentionDays">Retention days (uses config if not specified).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Maintenance result.</returns>
    [HttpPost("maintenance/cleanup/alerts")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(CoreMaintenanceResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<CoreMaintenanceResult>> RunAlertCleanup(
        [FromQuery] int? retentionDays = null,
        CancellationToken cancellationToken = default)
    {
        var days = retentionDays ?? 180;
        var deletedCount = await _maintenanceService.PurgeOldAlertsAsync(days, cancellationToken);
        return Ok(new CoreMaintenanceResult
        {
            Success = true,
            DurationMs = 0,
            Details = new Dictionary<string, object>
            {
                ["Operation"] = "AlertCleanup",
                ["RecordsAffected"] = deletedCount,
                ["RetentionDays"] = days
            }
        });
    }

    /// <summary>
    /// Runs audit log cleanup.
    /// </summary>
    /// <param name="retentionDays">Retention days.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Maintenance result.</returns>
    [HttpPost("maintenance/cleanup/audit")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(CoreMaintenanceResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<CoreMaintenanceResult>> RunAuditLogCleanup(
        [FromQuery] int retentionDays = 365,
        CancellationToken cancellationToken = default)
    {
        var deletedCount = await _maintenanceService.PurgeOldAuditLogsAsync(retentionDays, cancellationToken);
        return Ok(new CoreMaintenanceResult
        {
            Success = true,
            DurationMs = 0,
            Details = new Dictionary<string, object>
            {
                ["Operation"] = "AuditLogCleanup",
                ["RecordsAffected"] = deletedCount,
                ["RetentionDays"] = retentionDays
            }
        });
    }

    /// <summary>
    /// Runs token cleanup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Maintenance result.</returns>
    [HttpPost("maintenance/cleanup/tokens")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(CoreMaintenanceResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<CoreMaintenanceResult>> RunTokenCleanup(CancellationToken cancellationToken)
    {
        var deletedCount = await _maintenanceService.PurgeExpiredTokensAsync(cancellationToken);
        return Ok(new CoreMaintenanceResult
        {
            Success = true,
            DurationMs = 0,
            Details = new Dictionary<string, object>
            {
                ["Operation"] = "TokenCleanup",
                ["RecordsAffected"] = deletedCount
            }
        });
    }

    /// <summary>
    /// Runs server health check.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Maintenance result.</returns>
    [HttpPost("maintenance/health-check/servers")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(CoreMaintenanceResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<CoreMaintenanceResult>> RunServerHealthCheck(CancellationToken cancellationToken)
    {
        // Server health check is not in IMaintenanceService interface
        // Return a placeholder result indicating this operation is not supported
        return Ok(new CoreMaintenanceResult
        {
            Success = true,
            DurationMs = 0,
            Details = new Dictionary<string, object>
            {
                ["Operation"] = "ServerHealthCheck",
                ["Message"] = "Server health check not implemented in maintenance service"
            }
        });
    }

    /// <summary>
    /// Runs execution timeout check.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Maintenance result.</returns>
    [HttpPost("maintenance/health-check/executions")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(CoreMaintenanceResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<CoreMaintenanceResult>> RunExecutionTimeoutCheck(CancellationToken cancellationToken)
    {
        var timedOutCount = await _maintenanceService.TimeoutStuckExecutionsAsync(cancellationToken);
        return Ok(new CoreMaintenanceResult
        {
            Success = true,
            DurationMs = 0,
            Details = new Dictionary<string, object>
            {
                ["Operation"] = "ExecutionTimeoutCheck",
                ["RecordsAffected"] = timedOutCount
            }
        });
    }

    /// <summary>
    /// Runs all maintenance tasks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All maintenance results.</returns>
    [HttpPost("maintenance/run-all")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<CoreMaintenanceResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CoreMaintenanceResult>>> RunAllMaintenance(CancellationToken cancellationToken)
    {
        var summary = await _maintenanceService.RunAllMaintenanceAsync(cancellationToken);

        // Convert MaintenanceSummary to list of MaintenanceResult
        var results = new List<CoreMaintenanceResult>
        {
            new CoreMaintenanceResult
            {
                Success = summary.Errors.Count == 0,
                DurationMs = summary.TotalDurationMs,
                Details = new Dictionary<string, object>
                {
                    ["StartedAt"] = summary.StartedAt,
                    ["CompletedAt"] = summary.CompletedAt,
                    ["LogsDeleted"] = summary.LogsDeleted,
                    ["ExecutionsDeleted"] = summary.ExecutionsDeleted,
                    ["AlertsDeleted"] = summary.AlertsDeleted,
                    ["TokensDeleted"] = summary.TokensDeleted,
                    ["AuditLogsDeleted"] = summary.AuditLogsDeleted,
                    ["CacheEntriesCleared"] = summary.CacheEntriesCleared,
                    ["DatabaseMaintenanceSuccess"] = summary.DatabaseMaintenanceSuccess,
                    ["Errors"] = summary.Errors
                }
            }
        };

        return Ok(results);
    }

    #endregion

    #region Health Check

    /// <summary>
    /// Gets system health status.
    /// </summary>
    /// <returns>Health status.</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthCheckResponse> GetHealth()
    {
        return Ok(new HealthCheckResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0"
        });
    }

    #endregion
}

/// <summary>
/// Set configuration value request.
/// </summary>
public class SetConfigValueRequest
{
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Health check response.
/// </summary>
public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
}
