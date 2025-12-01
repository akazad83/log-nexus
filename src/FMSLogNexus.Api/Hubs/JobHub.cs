using Microsoft.AspNetCore.SignalR;

namespace FMSLogNexus.Api.Hubs;

/// <summary>
/// Hub for real-time job execution updates.
/// </summary>
public class JobHub : BaseHub<IJobHubClient>
{
    private const string HubName = "JobHub";
    private const string AllJobsGroup = "jobs:all";
    private const string AllExecutionsGroup = "executions:all";

    public JobHub(
        ILogger<JobHub> logger,
        IConnectionManager connectionManager)
        : base(logger, connectionManager)
    {
    }

    protected override string GetHubName() => HubName;

    #region Subscription Methods

    /// <summary>
    /// Subscribes to all job updates.
    /// </summary>
    public async Task SubscribeToAllJobs()
    {
        await JoinGroupAsync(AllJobsGroup);
        _logger.LogInformation("User {UserId} subscribed to all jobs", CurrentUserId);
    }

    /// <summary>
    /// Unsubscribes from all job updates.
    /// </summary>
    public async Task UnsubscribeFromAllJobs()
    {
        await LeaveGroupAsync(AllJobsGroup);
        _logger.LogInformation("User {UserId} unsubscribed from all jobs", CurrentUserId);
    }

    /// <summary>
    /// Subscribes to all execution updates.
    /// </summary>
    public async Task SubscribeToAllExecutions()
    {
        await JoinGroupAsync(AllExecutionsGroup);
        _logger.LogInformation("User {UserId} subscribed to all executions", CurrentUserId);
    }

    /// <summary>
    /// Unsubscribes from all execution updates.
    /// </summary>
    public async Task UnsubscribeFromAllExecutions()
    {
        await LeaveGroupAsync(AllExecutionsGroup);
        _logger.LogInformation("User {UserId} unsubscribed from all executions", CurrentUserId);
    }

    /// <summary>
    /// Subscribes to updates for a specific job.
    /// </summary>
    public async Task SubscribeToJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new HubException("Job ID is required.");

        var groupName = GetJobGroup(jobId);
        await JoinGroupAsync(groupName);
        _logger.LogInformation("User {UserId} subscribed to job {Job}", CurrentUserId, jobId);
    }

    /// <summary>
    /// Unsubscribes from updates for a specific job.
    /// </summary>
    public async Task UnsubscribeFromJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new HubException("Job ID is required.");

        var groupName = GetJobGroup(jobId);
        await LeaveGroupAsync(groupName);
        _logger.LogInformation("User {UserId} unsubscribed from job {Job}", CurrentUserId, jobId);
    }

    /// <summary>
    /// Subscribes to updates for a specific execution.
    /// </summary>
    public async Task SubscribeToExecution(Guid executionId)
    {
        var groupName = GetExecutionGroup(executionId);
        await JoinGroupAsync(groupName);
        _logger.LogInformation("User {UserId} subscribed to execution {Execution}", CurrentUserId, executionId);
    }

    /// <summary>
    /// Unsubscribes from updates for a specific execution.
    /// </summary>
    public async Task UnsubscribeFromExecution(Guid executionId)
    {
        var groupName = GetExecutionGroup(executionId);
        await LeaveGroupAsync(groupName);
        _logger.LogInformation("User {UserId} unsubscribed from execution {Execution}", CurrentUserId, executionId);
    }

    /// <summary>
    /// Subscribes to jobs for a specific server.
    /// </summary>
    public async Task SubscribeToServer(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            throw new HubException("Server name is required.");

        var groupName = GetServerGroup(serverName);
        await JoinGroupAsync(groupName);
        _logger.LogInformation("User {UserId} subscribed to server {Server} jobs", CurrentUserId, serverName);
    }

    /// <summary>
    /// Unsubscribes from jobs for a specific server.
    /// </summary>
    public async Task UnsubscribeFromServer(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            throw new HubException("Server name is required.");

        var groupName = GetServerGroup(serverName);
        await LeaveGroupAsync(groupName);
        _logger.LogInformation("User {UserId} unsubscribed from server {Server} jobs", CurrentUserId, serverName);
    }

    /// <summary>
    /// Subscribes to critical job updates.
    /// </summary>
    public async Task SubscribeToCriticalJobs()
    {
        await JoinGroupAsync("jobs:critical");
        _logger.LogInformation("User {UserId} subscribed to critical jobs", CurrentUserId);
    }

    /// <summary>
    /// Unsubscribes from critical job updates.
    /// </summary>
    public async Task UnsubscribeFromCriticalJobs()
    {
        await LeaveGroupAsync("jobs:critical");
        _logger.LogInformation("User {UserId} unsubscribed from critical jobs", CurrentUserId);
    }

    /// <summary>
    /// Subscribes to job health updates.
    /// </summary>
    public async Task SubscribeToHealthUpdates()
    {
        await JoinGroupAsync("jobs:health");
        _logger.LogInformation("User {UserId} subscribed to job health updates", CurrentUserId);
    }

    /// <summary>
    /// Unsubscribes from job health updates.
    /// </summary>
    public async Task UnsubscribeFromHealthUpdates()
    {
        await LeaveGroupAsync("jobs:health");
        _logger.LogInformation("User {UserId} unsubscribed from job health updates", CurrentUserId);
    }

    #endregion

    #region Group Name Helpers

    public static string GetJobGroup(string jobId) => $"jobs:job:{jobId.ToLowerInvariant()}";
    public static string GetExecutionGroup(Guid executionId) => $"jobs:execution:{executionId}";
    public static string GetServerGroup(string serverName) => $"jobs:server:{serverName.ToLowerInvariant()}";

    #endregion
}

/// <summary>
/// Service for broadcasting job events to connected clients.
/// </summary>
public interface IJobBroadcaster
{
    Task BroadcastExecutionStartedAsync(ExecutionNotification execution, CancellationToken cancellationToken = default);
    Task BroadcastExecutionCompletedAsync(ExecutionNotification execution, CancellationToken cancellationToken = default);
    Task BroadcastExecutionProgressAsync(ExecutionProgressNotification progress, CancellationToken cancellationToken = default);
    Task BroadcastJobStatusChangedAsync(JobStatusNotification status, CancellationToken cancellationToken = default);
    Task BroadcastJobHealthUpdatedAsync(JobHealthNotification health, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of job broadcaster using SignalR.
/// </summary>
public class JobBroadcaster : IJobBroadcaster
{
    private readonly IHubContext<JobHub, IJobHubClient> _hubContext;
    private readonly ILogger<JobBroadcaster> _logger;

    public JobBroadcaster(IHubContext<JobHub, IJobHubClient> hubContext, ILogger<JobBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastExecutionStartedAsync(ExecutionNotification execution, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            // Broadcast to all executions group
            _hubContext.Clients.Group("executions:all").ExecutionStarted(execution),
            
            // Broadcast to all jobs group
            _hubContext.Clients.Group("jobs:all").ExecutionStarted(execution),
            
            // Broadcast to job-specific group
            _hubContext.Clients.Group(JobHub.GetJobGroup(execution.JobId)).ExecutionStarted(execution),
            
            // Broadcast to execution-specific group
            _hubContext.Clients.Group(JobHub.GetExecutionGroup(execution.Id)).ExecutionStarted(execution),
            
            // Broadcast to server-specific group
            _hubContext.Clients.Group(JobHub.GetServerGroup(execution.ServerName)).ExecutionStarted(execution)
        };

        try
        {
            await Task.WhenAll(tasks);
            _logger.LogDebug("Broadcasted execution started: {ExecutionId}", execution.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting execution started notification");
        }
    }

    public async Task BroadcastExecutionCompletedAsync(ExecutionNotification execution, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            _hubContext.Clients.Group("executions:all").ExecutionCompleted(execution),
            _hubContext.Clients.Group("jobs:all").ExecutionCompleted(execution),
            _hubContext.Clients.Group(JobHub.GetJobGroup(execution.JobId)).ExecutionCompleted(execution),
            _hubContext.Clients.Group(JobHub.GetExecutionGroup(execution.Id)).ExecutionCompleted(execution),
            _hubContext.Clients.Group(JobHub.GetServerGroup(execution.ServerName)).ExecutionCompleted(execution)
        };

        try
        {
            await Task.WhenAll(tasks);
            _logger.LogDebug("Broadcasted execution completed: {ExecutionId}, Status: {Status}", execution.Id, execution.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting execution completed notification");
        }
    }

    public async Task BroadcastExecutionProgressAsync(ExecutionProgressNotification progress, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            _hubContext.Clients.Group(JobHub.GetJobGroup(progress.JobId)).ExecutionProgress(progress),
            _hubContext.Clients.Group(JobHub.GetExecutionGroup(progress.ExecutionId)).ExecutionProgress(progress)
        };

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting execution progress notification");
        }
    }

    public async Task BroadcastJobStatusChangedAsync(JobStatusNotification status, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            _hubContext.Clients.Group("jobs:all").JobStatusChanged(status),
            _hubContext.Clients.Group(JobHub.GetJobGroup(status.JobId)).JobStatusChanged(status)
        };

        try
        {
            await Task.WhenAll(tasks);
            _logger.LogDebug("Broadcasted job status changed: {JobId}", status.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting job status changed notification");
        }
    }

    public async Task BroadcastJobHealthUpdatedAsync(JobHealthNotification health, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            _hubContext.Clients.Group("jobs:health").JobHealthUpdated(health),
            _hubContext.Clients.Group(JobHub.GetJobGroup(health.JobId)).JobHealthUpdated(health)
        };

        // If health is critical, also broadcast to critical jobs group
        if (health.HealthScore < 50)
        {
            tasks.Add(_hubContext.Clients.Group("jobs:critical").JobHealthUpdated(health));
        }

        try
        {
            await Task.WhenAll(tasks);
            _logger.LogDebug("Broadcasted job health updated: {JobId}, Score: {Score}", health.JobId, health.HealthScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting job health notification");
        }
    }
}
