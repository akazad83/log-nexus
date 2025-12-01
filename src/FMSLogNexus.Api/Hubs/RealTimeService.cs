using FMSLogNexus.Core.Enums;
using FmsLogLevel = FMSLogNexus.Core.Enums.LogLevel;

namespace FMSLogNexus.Api.Hubs;

/// <summary>
/// Unified real-time service interface for the application.
/// This service is used by the application layer to broadcast events
/// without knowing about SignalR specifics.
/// </summary>
public interface IRealTimeService
{
    #region Log Events

    Task BroadcastLogAsync(
        Guid id,
        DateTime timestamp,
        FmsLogLevel level,
        string message,
        string? serverName = null,
        string? jobId = null,
        Guid? executionId = null,
        string? category = null,
        string? exceptionType = null,
        CancellationToken cancellationToken = default);

    Task BroadcastLogBatchAsync(
        IEnumerable<LogNotification> logs,
        CancellationToken cancellationToken = default);

    #endregion

    #region Job Events

    Task BroadcastExecutionStartedAsync(
        Guid executionId,
        string jobId,
        string jobDisplayName,
        string serverName,
        DateTime startedAt,
        string? triggerType = null,
        CancellationToken cancellationToken = default);

    Task BroadcastExecutionCompletedAsync(
        Guid executionId,
        string jobId,
        string jobDisplayName,
        string serverName,
        JobStatus status,
        DateTime startedAt,
        DateTime completedAt,
        long? durationMs = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    Task BroadcastJobHealthUpdatedAsync(
        string jobId,
        string displayName,
        int healthScore,
        int consecutiveFailures,
        double? successRate = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Server Events

    Task BroadcastServerStatusChangedAsync(
        string serverName,
        ServerStatus status,
        ServerStatus? previousStatus = null,
        bool isActive = true,
        bool inMaintenance = false,
        CancellationToken cancellationToken = default);

    Task BroadcastServerHeartbeatAsync(
        string serverName,
        ServerStatus status,
        string? agentVersion = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Alert Events

    Task BroadcastAlertTriggeredAsync(
        Guid instanceId,
        Guid alertId,
        string alertName,
        AlertType alertType,
        AlertSeverity severity,
        string message,
        string? jobId = null,
        string? serverName = null,
        CancellationToken cancellationToken = default);

    Task BroadcastAlertAcknowledgedAsync(
        Guid instanceId,
        Guid alertId,
        string alertName,
        string performedBy,
        string? note = null,
        CancellationToken cancellationToken = default);

    Task BroadcastAlertResolvedAsync(
        Guid instanceId,
        Guid alertId,
        string alertName,
        string performedBy,
        string? note = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Dashboard Events

    Task BroadcastDashboardUpdateAsync(
        DashboardNotification dashboard,
        CancellationToken cancellationToken = default);

    Task BroadcastSystemNotificationAsync(
        string type,
        string title,
        string message,
        string severity = "info",
        Dictionary<string, object>? data = null,
        CancellationToken cancellationToken = default);

    Task BroadcastToUserAsync(
        Guid userId,
        string type,
        string title,
        string message,
        string severity = "info",
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Implementation of the unified real-time service.
/// </summary>
public class RealTimeService : IRealTimeService
{
    private readonly ILogBroadcaster _logBroadcaster;
    private readonly IJobBroadcaster _jobBroadcaster;
    private readonly IServerBroadcaster _serverBroadcaster;
    private readonly IAlertBroadcaster _alertBroadcaster;
    private readonly IDashboardBroadcaster _dashboardBroadcaster;
    private readonly ILogger<RealTimeService> _logger;

    public RealTimeService(
        ILogBroadcaster logBroadcaster,
        IJobBroadcaster jobBroadcaster,
        IServerBroadcaster serverBroadcaster,
        IAlertBroadcaster alertBroadcaster,
        IDashboardBroadcaster dashboardBroadcaster,
        ILogger<RealTimeService> logger)
    {
        _logBroadcaster = logBroadcaster;
        _jobBroadcaster = jobBroadcaster;
        _serverBroadcaster = serverBroadcaster;
        _alertBroadcaster = alertBroadcaster;
        _dashboardBroadcaster = dashboardBroadcaster;
        _logger = logger;
    }

    #region Log Events

    public async Task BroadcastLogAsync(
        Guid id,
        DateTime timestamp,
        FmsLogLevel level,
        string message,
        string? serverName = null,
        string? jobId = null,
        Guid? executionId = null,
        string? category = null,
        string? exceptionType = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new LogNotification
        {
            Id = id,
            Timestamp = timestamp,
            Level = level,
            Message = message,
            ServerName = serverName,
            JobId = jobId,
            ExecutionId = executionId,
            Category = category,
            ExceptionType = exceptionType
        };

        await _logBroadcaster.BroadcastLogAsync(notification, cancellationToken);
    }

    public async Task BroadcastLogBatchAsync(
        IEnumerable<LogNotification> logs,
        CancellationToken cancellationToken = default)
    {
        await _logBroadcaster.BroadcastLogBatchAsync(logs, cancellationToken);
    }

    #endregion

    #region Job Events

    public async Task BroadcastExecutionStartedAsync(
        Guid executionId,
        string jobId,
        string jobDisplayName,
        string serverName,
        DateTime startedAt,
        string? triggerType = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new ExecutionNotification
        {
            Id = executionId,
            JobId = jobId,
            JobDisplayName = jobDisplayName,
            ServerName = serverName,
            Status = JobStatus.Running,
            StartedAt = startedAt,
            TriggerType = triggerType
        };

        await _jobBroadcaster.BroadcastExecutionStartedAsync(notification, cancellationToken);
    }

    public async Task BroadcastExecutionCompletedAsync(
        Guid executionId,
        string jobId,
        string jobDisplayName,
        string serverName,
        JobStatus status,
        DateTime startedAt,
        DateTime completedAt,
        long? durationMs = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new ExecutionNotification
        {
            Id = executionId,
            JobId = jobId,
            JobDisplayName = jobDisplayName,
            ServerName = serverName,
            Status = status,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            DurationMs = durationMs,
            ErrorMessage = errorMessage
        };

        await _jobBroadcaster.BroadcastExecutionCompletedAsync(notification, cancellationToken);
    }

    public async Task BroadcastJobHealthUpdatedAsync(
        string jobId,
        string displayName,
        int healthScore,
        int consecutiveFailures,
        double? successRate = null,
        CancellationToken cancellationToken = default)
    {
        var healthStatus = healthScore >= 80 ? "Healthy" :
                          healthScore >= 50 ? "Warning" : "Critical";

        var notification = new JobHealthNotification
        {
            JobId = jobId,
            DisplayName = displayName,
            HealthScore = healthScore,
            HealthStatus = healthStatus,
            ConsecutiveFailures = consecutiveFailures,
            SuccessRate = successRate
        };

        await _jobBroadcaster.BroadcastJobHealthUpdatedAsync(notification, cancellationToken);
    }

    #endregion

    #region Server Events

    public async Task BroadcastServerStatusChangedAsync(
        string serverName,
        ServerStatus status,
        ServerStatus? previousStatus = null,
        bool isActive = true,
        bool inMaintenance = false,
        CancellationToken cancellationToken = default)
    {
        var notification = new ServerStatusNotification
        {
            ServerName = serverName,
            Status = status,
            PreviousStatus = previousStatus,
            Timestamp = DateTime.UtcNow,
            IsActive = isActive,
            InMaintenance = inMaintenance
        };

        await _serverBroadcaster.BroadcastServerStatusChangedAsync(notification, cancellationToken);
    }

    public async Task BroadcastServerHeartbeatAsync(
        string serverName,
        ServerStatus status,
        string? agentVersion = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new ServerHeartbeatNotification
        {
            ServerName = serverName,
            Timestamp = DateTime.UtcNow,
            Status = status,
            AgentVersion = agentVersion
        };

        await _serverBroadcaster.BroadcastServerHeartbeatAsync(notification, cancellationToken);
    }

    #endregion

    #region Alert Events

    public async Task BroadcastAlertTriggeredAsync(
        Guid instanceId,
        Guid alertId,
        string alertName,
        AlertType alertType,
        AlertSeverity severity,
        string message,
        string? jobId = null,
        string? serverName = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new AlertNotification
        {
            InstanceId = instanceId,
            AlertId = alertId,
            AlertName = alertName,
            AlertType = alertType,
            Severity = severity,
            Message = message,
            TriggeredAt = DateTime.UtcNow,
            JobId = jobId,
            ServerName = serverName
        };

        await _alertBroadcaster.BroadcastAlertTriggeredAsync(notification, cancellationToken);
    }

    public async Task BroadcastAlertAcknowledgedAsync(
        Guid instanceId,
        Guid alertId,
        string alertName,
        string performedBy,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new AlertActionNotification
        {
            InstanceId = instanceId,
            AlertId = alertId,
            AlertName = alertName,
            Action = "Acknowledged",
            PerformedBy = performedBy,
            PerformedAt = DateTime.UtcNow,
            Note = note
        };

        await _alertBroadcaster.BroadcastAlertAcknowledgedAsync(notification, cancellationToken);
    }

    public async Task BroadcastAlertResolvedAsync(
        Guid instanceId,
        Guid alertId,
        string alertName,
        string performedBy,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new AlertActionNotification
        {
            InstanceId = instanceId,
            AlertId = alertId,
            AlertName = alertName,
            Action = "Resolved",
            PerformedBy = performedBy,
            PerformedAt = DateTime.UtcNow,
            Note = note
        };

        await _alertBroadcaster.BroadcastAlertResolvedAsync(notification, cancellationToken);
    }

    #endregion

    #region Dashboard Events

    public async Task BroadcastDashboardUpdateAsync(
        DashboardNotification dashboard,
        CancellationToken cancellationToken = default)
    {
        await _dashboardBroadcaster.BroadcastDashboardUpdateAsync(dashboard, cancellationToken);
    }

    public async Task BroadcastSystemNotificationAsync(
        string type,
        string title,
        string message,
        string severity = "info",
        Dictionary<string, object>? data = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new SystemNotificationMessage
        {
            Type = type,
            Title = title,
            Message = message,
            Severity = severity,
            Timestamp = DateTime.UtcNow,
            Data = data
        };

        await _dashboardBroadcaster.BroadcastSystemNotificationAsync(notification, cancellationToken);
    }

    public async Task BroadcastToUserAsync(
        Guid userId,
        string type,
        string title,
        string message,
        string severity = "info",
        CancellationToken cancellationToken = default)
    {
        var notification = new SystemNotificationMessage
        {
            Type = type,
            Title = title,
            Message = message,
            Severity = severity,
            Timestamp = DateTime.UtcNow
        };

        await _dashboardBroadcaster.BroadcastToUserAsync(userId, notification, cancellationToken);
    }

    #endregion
}
