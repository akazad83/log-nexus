using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FMSLogNexus.Client;

/// <summary>
/// Background service for sending periodic heartbeats.
/// </summary>
public class HeartbeatService : BackgroundService
{
    private readonly FMSLogNexusClient _client;
    private readonly FMSLogNexusOptions _options;
    private readonly ILogger<HeartbeatService>? _logger;

    public HeartbeatService(
        FMSLogNexusClient client,
        IOptions<FMSLogNexusOptions> options,
        ILogger<HeartbeatService>? logger = null)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableAutoHeartbeat)
        {
            _logger?.LogInformation("Automatic heartbeat is disabled");
            return;
        }

        _logger?.LogInformation("Heartbeat service started. Interval: {Interval}s", 
            _options.HeartbeatIntervalSeconds);

        // Send initial heartbeat
        await SendHeartbeatAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.HeartbeatIntervalSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SendHeartbeatAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("Heartbeat service stopping");
        }
    }

    private async Task SendHeartbeatAsync(CancellationToken cancellationToken)
    {
        try
        {
            var systemInfo = _client.Servers.GetSystemInfo();
            await _client.Servers.HeartbeatAsync(ServerStatus.Online, systemInfo, cancellationToken);
            _logger?.LogDebug("Heartbeat sent successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to send heartbeat");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Send offline heartbeat
            await _client.Servers.HeartbeatAsync(ServerStatus.Offline, cancellationToken: cancellationToken);
            _logger?.LogInformation("Sent offline heartbeat");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to send offline heartbeat");
        }

        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Background service for flushing buffered logs.
/// </summary>
public class LogFlushService : BackgroundService
{
    private readonly FMSLogNexusClient _client;
    private readonly FMSLogNexusOptions _options;
    private readonly ILogger<LogFlushService>? _logger;

    public LogFlushService(
        FMSLogNexusClient client,
        IOptions<FMSLogNexusOptions> options,
        ILogger<LogFlushService>? logger = null)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableLogBuffering)
        {
            _logger?.LogInformation("Log buffering is disabled");
            return;
        }

        _logger?.LogInformation("Log flush service started. Interval: {Interval}s", 
            _options.LogFlushIntervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.LogFlushIntervalSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await FlushLogsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("Log flush service stopping");
        }
    }

    private async Task FlushLogsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var bufferCount = _client.Logs.BufferCount;
            if (bufferCount == 0) return;

            var result = await _client.Logs.FlushAsync(cancellationToken);
            
            if (result != null)
            {
                _logger?.LogDebug("Flushed {Count} logs. Success: {Success}, Failed: {Failed}",
                    result.TotalReceived, result.SuccessCount, result.FailedCount);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to flush logs");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Final flush on shutdown
        try
        {
            var result = await _client.Logs.FlushAsync(cancellationToken);
            if (result != null && result.TotalReceived > 0)
            {
                _logger?.LogInformation("Final flush: sent {Count} logs", result.TotalReceived);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to flush logs on shutdown");
        }

        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Combines heartbeat and log flush functionality.
/// </summary>
public class FMSAgentService : BackgroundService
{
    private readonly FMSLogNexusClient _client;
    private readonly FMSLogNexusOptions _options;
    private readonly ILogger<FMSAgentService>? _logger;

    public FMSAgentService(
        FMSLogNexusClient client,
        IOptions<FMSLogNexusOptions> options,
        ILogger<FMSAgentService>? logger = null)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger?.LogInformation("FMS Agent service started for server: {ServerName}", _options.ServerName);

        // Send initial heartbeat
        await SendHeartbeatAsync(stoppingToken);

        var heartbeatInterval = TimeSpan.FromSeconds(_options.HeartbeatIntervalSeconds);
        var flushInterval = TimeSpan.FromSeconds(_options.LogFlushIntervalSeconds);

        var lastHeartbeat = DateTime.UtcNow;
        var lastFlush = DateTime.UtcNow;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var now = DateTime.UtcNow;

                // Check for heartbeat
                if (_options.EnableAutoHeartbeat && (now - lastHeartbeat) >= heartbeatInterval)
                {
                    await SendHeartbeatAsync(stoppingToken);
                    lastHeartbeat = now;
                }

                // Check for log flush
                if (_options.EnableLogBuffering && (now - lastFlush) >= flushInterval)
                {
                    await FlushLogsAsync(stoppingToken);
                    lastFlush = now;
                }

                // Also flush if buffer is full
                if (_options.EnableLogBuffering && _client.Logs.BufferCount >= _options.MaxBatchSize)
                {
                    await FlushLogsAsync(stoppingToken);
                    lastFlush = now;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("FMS Agent service stopping");
        }
    }

    private async Task SendHeartbeatAsync(CancellationToken cancellationToken)
    {
        try
        {
            var systemInfo = _client.Servers.GetSystemInfo();
            await _client.Servers.HeartbeatAsync(ServerStatus.Online, systemInfo, cancellationToken);
            _logger?.LogDebug("Heartbeat sent");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Heartbeat failed");
        }
    }

    private async Task FlushLogsAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_client.Logs.BufferCount == 0) return;

            var result = await _client.Logs.FlushAsync(cancellationToken);
            if (result != null)
            {
                _logger?.LogDebug("Flushed {Count} logs", result.TotalReceived);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Log flush failed");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Final flush and offline heartbeat
        try
        {
            await _client.Logs.FlushAsync(cancellationToken);
            await _client.Servers.HeartbeatAsync(ServerStatus.Offline, cancellationToken: cancellationToken);
            _logger?.LogInformation("Agent shutdown complete");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Shutdown tasks failed");
        }

        await base.StopAsync(cancellationToken);
    }
}
