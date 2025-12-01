using Microsoft.AspNetCore.SignalR;
using FMSLogNexus.Core.Enums;
using FmsLogLevel = FMSLogNexus.Core.Enums.LogLevel;

namespace FMSLogNexus.Api.Hubs;

/// <summary>
/// Hub for real-time log streaming.
/// </summary>
public class LogHub : BaseHub<ILogHubClient>
{
    private const string HubName = "LogHub";
    private const string AllLogsGroup = "logs:all";

    public LogHub(
        ILogger<LogHub> logger,
        IConnectionManager connectionManager)
        : base(logger, connectionManager)
    {
    }

    protected override string GetHubName() => HubName;

    #region Subscription Methods

    /// <summary>
    /// Subscribes to all logs.
    /// </summary>
    public async Task SubscribeToAllLogs()
    {
        await JoinGroupAsync(AllLogsGroup);
        _logger.LogInformation("User {UserId} subscribed to all logs", CurrentUserId);
    }

    /// <summary>
    /// Unsubscribes from all logs.
    /// </summary>
    public async Task UnsubscribeFromAllLogs()
    {
        await LeaveGroupAsync(AllLogsGroup);
        _logger.LogInformation("User {UserId} unsubscribed from all logs", CurrentUserId);
    }

    /// <summary>
    /// Subscribes to logs for a specific server.
    /// </summary>
    public async Task SubscribeToServer(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            throw new HubException("Server name is required.");

        var groupName = GetServerGroup(serverName);
        await JoinGroupAsync(groupName);
        _logger.LogInformation("User {UserId} subscribed to server {Server} logs", CurrentUserId, serverName);
    }

    /// <summary>
    /// Unsubscribes from logs for a specific server.
    /// </summary>
    public async Task UnsubscribeFromServer(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            throw new HubException("Server name is required.");

        var groupName = GetServerGroup(serverName);
        await LeaveGroupAsync(groupName);
        _logger.LogInformation("User {UserId} unsubscribed from server {Server} logs", CurrentUserId, serverName);
    }

    /// <summary>
    /// Subscribes to logs for a specific job.
    /// </summary>
    public async Task SubscribeToJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new HubException("Job ID is required.");

        var groupName = GetJobGroup(jobId);
        await JoinGroupAsync(groupName);
        _logger.LogInformation("User {UserId} subscribed to job {Job} logs", CurrentUserId, jobId);
    }

    /// <summary>
    /// Unsubscribes from logs for a specific job.
    /// </summary>
    public async Task UnsubscribeFromJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new HubException("Job ID is required.");

        var groupName = GetJobGroup(jobId);
        await LeaveGroupAsync(groupName);
        _logger.LogInformation("User {UserId} unsubscribed from job {Job} logs", CurrentUserId, jobId);
    }

    /// <summary>
    /// Subscribes to logs for a specific execution.
    /// </summary>
    public async Task SubscribeToExecution(Guid executionId)
    {
        var groupName = GetExecutionGroup(executionId);
        await JoinGroupAsync(groupName);
        _logger.LogInformation("User {UserId} subscribed to execution {Execution} logs", CurrentUserId, executionId);
    }

    /// <summary>
    /// Unsubscribes from logs for a specific execution.
    /// </summary>
    public async Task UnsubscribeFromExecution(Guid executionId)
    {
        var groupName = GetExecutionGroup(executionId);
        await LeaveGroupAsync(groupName);
        _logger.LogInformation("User {UserId} unsubscribed from execution {Execution} logs", CurrentUserId, executionId);
    }

    /// <summary>
    /// Subscribes to error logs only.
    /// </summary>
    public async Task SubscribeToErrors()
    {
        await JoinGroupAsync("logs:errors");
        _logger.LogInformation("User {UserId} subscribed to error logs", CurrentUserId);
    }

    /// <summary>
    /// Unsubscribes from error logs.
    /// </summary>
    public async Task UnsubscribeFromErrors()
    {
        await LeaveGroupAsync("logs:errors");
        _logger.LogInformation("User {UserId} unsubscribed from error logs", CurrentUserId);
    }

    /// <summary>
    /// Subscribes with custom filter options.
    /// </summary>
    public async Task SubscribeWithFilter(LogSubscriptionOptions options)
    {
        // Subscribe to appropriate groups based on options
        if (!string.IsNullOrEmpty(options.ServerName))
        {
            await JoinGroupAsync(GetServerGroup(options.ServerName));
        }

        if (!string.IsNullOrEmpty(options.JobId))
        {
            await JoinGroupAsync(GetJobGroup(options.JobId));
        }

        if (options.ExecutionId.HasValue)
        {
            await JoinGroupAsync(GetExecutionGroup(options.ExecutionId.Value));
        }

        if (options.MinLevel.HasValue && options.MinLevel.Value >= FmsLogLevel.Error)
        {
            await JoinGroupAsync("logs:errors");
        }

        if (options.IncludeStatistics)
        {
            await JoinGroupAsync("logs:stats");
        }

        _logger.LogInformation("User {UserId} subscribed with custom filter", CurrentUserId);
    }

    #endregion

    #region Group Name Helpers

    public static string GetServerGroup(string serverName) => $"logs:server:{serverName.ToLowerInvariant()}";
    public static string GetJobGroup(string jobId) => $"logs:job:{jobId.ToLowerInvariant()}";
    public static string GetExecutionGroup(Guid executionId) => $"logs:execution:{executionId}";

    #endregion
}

/// <summary>
/// Service for broadcasting log events to connected clients.
/// </summary>
public interface ILogBroadcaster
{
    Task BroadcastLogAsync(LogNotification log, CancellationToken cancellationToken = default);
    Task BroadcastLogBatchAsync(IEnumerable<LogNotification> logs, CancellationToken cancellationToken = default);
    Task BroadcastLogStatsAsync(LogStatsNotification stats, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of log broadcaster using SignalR.
/// </summary>
public class LogBroadcaster : ILogBroadcaster
{
    private readonly IHubContext<LogHub, ILogHubClient> _hubContext;
    private readonly ILogger<LogBroadcaster> _logger;

    public LogBroadcaster(IHubContext<LogHub, ILogHubClient> hubContext, ILogger<LogBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastLogAsync(LogNotification log, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>();

        // Broadcast to all logs group
        tasks.Add(_hubContext.Clients.Group("logs:all").ReceiveLog(log));

        // Broadcast to server-specific group
        if (!string.IsNullOrEmpty(log.ServerName))
        {
            tasks.Add(_hubContext.Clients.Group(LogHub.GetServerGroup(log.ServerName)).ReceiveLog(log));
        }

        // Broadcast to job-specific group
        if (!string.IsNullOrEmpty(log.JobId))
        {
            tasks.Add(_hubContext.Clients.Group(LogHub.GetJobGroup(log.JobId)).ReceiveLog(log));
        }

        // Broadcast to execution-specific group
        if (log.ExecutionId.HasValue)
        {
            tasks.Add(_hubContext.Clients.Group(LogHub.GetExecutionGroup(log.ExecutionId.Value)).ReceiveLog(log));
        }

        // Broadcast to errors group if applicable
        if (log.Level >= FmsLogLevel.Error)
        {
            tasks.Add(_hubContext.Clients.Group("logs:errors").ReceiveLog(log));
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting log notification");
        }
    }

    public async Task BroadcastLogBatchAsync(IEnumerable<LogNotification> logs, CancellationToken cancellationToken = default)
    {
        var logList = logs.ToList();
        if (logList.Count == 0) return;

        try
        {
            // Broadcast entire batch to all logs group
            await _hubContext.Clients.Group("logs:all").ReceiveLogBatch(logList);

            // Group logs by server and broadcast
            var byServer = logList.Where(l => !string.IsNullOrEmpty(l.ServerName)).GroupBy(l => l.ServerName!);
            foreach (var group in byServer)
            {
                await _hubContext.Clients.Group(LogHub.GetServerGroup(group.Key)).ReceiveLogBatch(group);
            }

            // Group logs by job and broadcast
            var byJob = logList.Where(l => !string.IsNullOrEmpty(l.JobId)).GroupBy(l => l.JobId!);
            foreach (var group in byJob)
            {
                await _hubContext.Clients.Group(LogHub.GetJobGroup(group.Key)).ReceiveLogBatch(group);
            }

            // Broadcast errors
            var errors = logList.Where(l => l.Level >= FmsLogLevel.Error).ToList();
            if (errors.Count > 0)
            {
                await _hubContext.Clients.Group("logs:errors").ReceiveLogBatch(errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting log batch notification");
        }
    }

    public async Task BroadcastLogStatsAsync(LogStatsNotification stats, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group("logs:stats").ReceiveLogStats(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting log stats notification");
        }
    }
}
