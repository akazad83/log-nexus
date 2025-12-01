using System.Collections.Concurrent;
using FMSLogNexus.Client.Models;

namespace FMSLogNexus.Client;

/// <summary>
/// Client for sending log entries to FMS Log Nexus.
/// </summary>
public class LogClient
{
    private readonly FMSLogNexusClient _client;
    private readonly ConcurrentQueue<CreateLogRequest> _buffer = new();
    private readonly object _flushLock = new();
    private bool _isDisposed;

    internal LogClient(FMSLogNexusClient client)
    {
        _client = client;
    }

    #region Direct Methods

    /// <summary>
    /// Creates a single log entry immediately.
    /// </summary>
    public async Task<LogResponse?> CreateAsync(CreateLogRequest request, CancellationToken cancellationToken = default)
    {
        request.ServerName ??= _client.ServerName;
        return await _client.PostAsync<LogResponse>("api/logs", request, cancellationToken);
    }

    /// <summary>
    /// Creates multiple log entries in a batch.
    /// </summary>
    public async Task<BatchLogResponse?> CreateBatchAsync(IEnumerable<CreateLogRequest> requests, CancellationToken cancellationToken = default)
    {
        var logs = requests.ToList();
        foreach (var log in logs)
        {
            log.ServerName ??= _client.ServerName;
        }
        return await _client.PostAsync<BatchLogResponse>("api/logs/batch", logs, cancellationToken);
    }

    /// <summary>
    /// Gets a log entry by ID.
    /// </summary>
    public async Task<LogResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _client.GetAsync<LogResponse>($"api/logs/{id}", cancellationToken);
    }

    #endregion

    #region Convenience Methods

    /// <summary>
    /// Logs a message at the specified level.
    /// </summary>
    public async Task LogAsync(LogLevel level, string message, string? jobId = null, Guid? executionId = null,
        Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        var request = new CreateLogRequest
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = message,
            JobId = jobId,
            ExecutionId = executionId,
            Properties = properties
        };

        await CreateAsync(request, cancellationToken);
    }

    /// <summary>
    /// Logs a trace message.
    /// </summary>
    public Task TraceAsync(string message, string? jobId = null, Guid? executionId = null,
        CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Trace, message, jobId, executionId, cancellationToken: cancellationToken);

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    public Task DebugAsync(string message, string? jobId = null, Guid? executionId = null,
        CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Debug, message, jobId, executionId, cancellationToken: cancellationToken);

    /// <summary>
    /// Logs an information message.
    /// </summary>
    public Task InfoAsync(string message, string? jobId = null, Guid? executionId = null,
        CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Information, message, jobId, executionId, cancellationToken: cancellationToken);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public Task WarnAsync(string message, string? jobId = null, Guid? executionId = null,
        CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Warning, message, jobId, executionId, cancellationToken: cancellationToken);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    public Task ErrorAsync(string message, Exception? exception = null, string? jobId = null, Guid? executionId = null,
        CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Error, message, jobId, executionId, 
            exception != null ? new Dictionary<string, object> { ["Exception"] = exception.ToString() } : null,
            cancellationToken);

    /// <summary>
    /// Logs a critical message.
    /// </summary>
    public Task CriticalAsync(string message, Exception? exception = null, string? jobId = null, Guid? executionId = null,
        CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Critical, message, jobId, executionId,
            exception != null ? new Dictionary<string, object> { ["Exception"] = exception.ToString() } : null,
            cancellationToken);

    #endregion

    #region Buffered Methods

    /// <summary>
    /// Enqueues a log entry for buffered sending.
    /// </summary>
    public void Enqueue(CreateLogRequest request)
    {
        request.ServerName ??= _client.ServerName;
        _buffer.Enqueue(request);
    }

    /// <summary>
    /// Enqueues a log message for buffered sending.
    /// </summary>
    public void Enqueue(LogLevel level, string message, string? jobId = null, Guid? executionId = null,
        Dictionary<string, object>? properties = null)
    {
        Enqueue(new CreateLogRequest
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = message,
            JobId = jobId,
            ExecutionId = executionId,
            Properties = properties
        });
    }

    /// <summary>
    /// Gets the number of buffered log entries.
    /// </summary>
    public int BufferCount => _buffer.Count;

    /// <summary>
    /// Flushes all buffered log entries.
    /// </summary>
    public async Task<BatchLogResponse?> FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_buffer.IsEmpty) return null;

        List<CreateLogRequest> logsToSend;

        lock (_flushLock)
        {
            if (_buffer.IsEmpty) return null;

            logsToSend = new List<CreateLogRequest>();
            while (_buffer.TryDequeue(out var log))
            {
                logsToSend.Add(log);
            }
        }

        if (logsToSend.Count == 0) return null;

        return await CreateBatchAsync(logsToSend, cancellationToken);
    }

    #endregion
}

/// <summary>
/// Client for server operations.
/// </summary>
public class ServerClient
{
    private readonly FMSLogNexusClient _client;

    internal ServerClient(FMSLogNexusClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Sends a heartbeat to the server.
    /// </summary>
    public async Task HeartbeatAsync(ServerStatus status = ServerStatus.Online, 
        SystemInfo? systemInfo = null, CancellationToken cancellationToken = default)
    {
        var request = new HeartbeatRequest
        {
            ServerName = _client.ServerName,
            Status = status,
            AgentVersion = _client.AgentVersion,
            SystemInfo = systemInfo
        };

        await _client.PostAsync("api/servers/heartbeat", request, cancellationToken);
    }

    /// <summary>
    /// Gets server details.
    /// </summary>
    public async Task<ServerResponse?> GetAsync(string? serverName = null, CancellationToken cancellationToken = default)
    {
        serverName ??= _client.ServerName;
        return await _client.GetAsync<ServerResponse>($"api/servers/{serverName}", cancellationToken);
    }

    /// <summary>
    /// Gets all servers.
    /// </summary>
    public async Task<List<ServerResponse>?> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _client.GetAsync<List<ServerResponse>>("api/servers", cancellationToken);
    }

    /// <summary>
    /// Collects current system information.
    /// </summary>
    public SystemInfo GetSystemInfo()
    {
        return new SystemInfo
        {
            Os = Environment.OSVersion.ToString(),
            CpuUsage = null, // Would need performance counters
            MemoryUsage = GC.GetTotalMemory(false) / (1024.0 * 1024.0),
            Properties = new Dictionary<string, object>
            {
                ["MachineName"] = Environment.MachineName,
                ["ProcessorCount"] = Environment.ProcessorCount,
                ["Is64BitOperatingSystem"] = Environment.Is64BitOperatingSystem,
                ["CLRVersion"] = Environment.Version.ToString()
            }
        };
    }
}

/// <summary>
/// Client for job operations.
/// </summary>
public class JobClient
{
    private readonly FMSLogNexusClient _client;

    internal JobClient(FMSLogNexusClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Registers a new job or updates an existing one.
    /// </summary>
    public async Task<JobResponse?> RegisterAsync(RegisterJobRequest request, CancellationToken cancellationToken = default)
    {
        request.ServerName = string.IsNullOrEmpty(request.ServerName) ? _client.ServerName : request.ServerName;
        return await _client.PostAsync<JobResponse>("api/jobs/register", request, cancellationToken);
    }

    /// <summary>
    /// Gets job details.
    /// </summary>
    public async Task<JobResponse?> GetAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return await _client.GetAsync<JobResponse>($"api/jobs/{jobId}", cancellationToken);
    }

    /// <summary>
    /// Gets all jobs for a server.
    /// </summary>
    public async Task<List<JobResponse>?> GetByServerAsync(string? serverName = null, CancellationToken cancellationToken = default)
    {
        serverName ??= _client.ServerName;
        return await _client.GetAsync<List<JobResponse>>($"api/jobs/server/{serverName}", cancellationToken);
    }

    /// <summary>
    /// Activates a job.
    /// </summary>
    public async Task ActivateAsync(string jobId, CancellationToken cancellationToken = default)
    {
        await _client.PostAsync($"api/jobs/{jobId}/activate", null, cancellationToken);
    }

    /// <summary>
    /// Deactivates a job.
    /// </summary>
    public async Task DeactivateAsync(string jobId, CancellationToken cancellationToken = default)
    {
        await _client.PostAsync($"api/jobs/{jobId}/deactivate", null, cancellationToken);
    }
}

/// <summary>
/// Client for execution operations.
/// </summary>
public class ExecutionClient
{
    private readonly FMSLogNexusClient _client;

    internal ExecutionClient(FMSLogNexusClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Starts a new job execution.
    /// </summary>
    public async Task<ExecutionResponse?> StartAsync(string jobId, TriggerType triggerType = TriggerType.Manual,
        string? triggeredBy = null, Dictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var request = new StartExecutionRequest
        {
            JobId = jobId,
            ServerName = _client.ServerName,
            TriggerType = triggerType,
            TriggeredBy = triggeredBy,
            Parameters = parameters
        };

        return await _client.PostAsync<ExecutionResponse>("api/executions", request, cancellationToken);
    }

    /// <summary>
    /// Completes a job execution with success.
    /// </summary>
    public async Task<ExecutionResponse?> CompleteSuccessAsync(Guid executionId, string? outputMessage = null,
        CancellationToken cancellationToken = default)
    {
        var request = new CompleteExecutionRequest
        {
            Status = ExecutionStatus.Success,
            OutputMessage = outputMessage
        };

        return await _client.PutAsync<ExecutionResponse>($"api/executions/{executionId}/complete", request, cancellationToken);
    }

    /// <summary>
    /// Completes a job execution with failure.
    /// </summary>
    public async Task<ExecutionResponse?> CompleteFailureAsync(Guid executionId, string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var request = new CompleteExecutionRequest
        {
            Status = ExecutionStatus.Failed,
            ErrorMessage = errorMessage
        };

        return await _client.PutAsync<ExecutionResponse>($"api/executions/{executionId}/complete", request, cancellationToken);
    }

    /// <summary>
    /// Cancels a running execution.
    /// </summary>
    public async Task CancelAsync(Guid executionId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        await _client.PostAsync($"api/executions/{executionId}/cancel", new { reason }, cancellationToken);
    }

    /// <summary>
    /// Gets execution details.
    /// </summary>
    public async Task<ExecutionResponse?> GetAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        return await _client.GetAsync<ExecutionResponse>($"api/executions/{executionId}", cancellationToken);
    }

    /// <summary>
    /// Gets running executions.
    /// </summary>
    public async Task<List<ExecutionResponse>?> GetRunningAsync(CancellationToken cancellationToken = default)
    {
        return await _client.GetAsync<List<ExecutionResponse>>("api/executions/running", cancellationToken);
    }

    /// <summary>
    /// Gets execution history for a job.
    /// </summary>
    public async Task<PagedResponse<ExecutionResponse>?> GetHistoryAsync(string jobId, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await _client.GetAsync<PagedResponse<ExecutionResponse>>(
            $"api/executions/job/{jobId}?page={page}&pageSize={pageSize}", cancellationToken);
    }
}
