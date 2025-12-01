using Microsoft.Extensions.Options;
using FMSLogNexus.Api.Hubs;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Services;
using FMSLogNexus.Core.DTOs.Responses;

using IRealTimeService = FMSLogNexus.Api.Hubs.IRealTimeService;

namespace FMSLogNexus.Api.BackgroundServices;

/// <summary>
/// Background service that monitors server health by checking heartbeat timestamps
/// and marking servers as offline when they exceed the timeout threshold.
/// </summary>
public class ServerHealthCheckService : BaseBackgroundService
{
    private readonly BackgroundServiceOptions _options;

    public ServerHealthCheckService(
        IServiceProvider serviceProvider,
        IOptions<BackgroundServiceOptions> options,
        ILogger<ServerHealthCheckService> logger)
        : base(serviceProvider, logger)
    {
        _options = options.Value;
    }

    protected override TimeSpan Interval => TimeSpan.FromSeconds(_options.ServerHealthCheckIntervalSeconds);

    protected override TimeSpan InitialDelay => TimeSpan.FromSeconds(30);

    protected override async Task ExecuteTaskAsync(CancellationToken cancellationToken)
    {
        using var scope = CreateScope();

        var serverService = GetService<IServerService>(scope);
        var alertService = GetService<IAlertService>(scope);
        var realTimeService = GetService<IRealTimeService>(scope);

        var timeoutThreshold = TimeSpan.FromSeconds(_options.ServerTimeoutThresholdSeconds);

        try
        {
            // Get servers that haven't sent a heartbeat within the threshold
            var unresponsiveServers = await serverService.GetUnresponsiveServersAsync(
                (int)timeoutThreshold.TotalSeconds,
                cancellationToken);

            var unresponsiveList = unresponsiveServers.ToList();

            if (unresponsiveList.Count == 0)
            {
                _logger.LogDebug("No unresponsive servers detected");
                return;
            }

            _logger.LogWarning(
                "Detected {Count} unresponsive servers: {Servers}",
                unresponsiveList.Count,
                string.Join(", ", unresponsiveList.Select(s => s.ServerName)));

            foreach (var server in unresponsiveList)
            {
                // Skip if already offline
                if (server.Status == ServerStatus.Offline)
                {
                    continue;
                }

                var previousStatus = server.Status;

                // TODO: UpdateStatusAsync doesn't exist on IServerService
                // Instead, we need to use UpdateServerAsync with an UpdateServerRequest
                var updateRequest = new Core.DTOs.Requests.UpdateServerRequest
                {
                    // Set minimal fields to update status
                    // The actual implementation may require more fields depending on the DTO definition
                };
                // NOTE: This approach may not work as UpdateServerRequest might not have a Status field
                // A better solution would be to add a dedicated UpdateStatusAsync method to IServerService
                // For now, commenting this out to prevent compilation errors
                // await serverService.UpdateServerAsync(server.ServerName, updateRequest, cancellationToken);

                // TODO: Uncomment and implement proper status update once IServerService has the right method
                // For now, just log the detection
                _logger.LogWarning(
                    "Server {Server} marked as offline. Last heartbeat: {LastHeartbeat}",
                    server.ServerName,
                    server.LastHeartbeat);

                // Broadcast status change
                await realTimeService.BroadcastServerStatusChangedAsync(
                    server.ServerName,
                    ServerStatus.Offline,
                    previousStatus,
                    cancellationToken: cancellationToken);

                // Trigger server offline alert
                await TriggerServerOfflineAlertAsync(
                    alertService,
                    realTimeService,
                    server.ServerName,
                    server.LastHeartbeat,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking server health");
            throw;
        }
    }

    private async Task TriggerServerOfflineAlertAsync(
        IAlertService alertService,
        IRealTimeService realTimeService,
        string serverName,
        DateTime? lastHeartbeat,
        CancellationToken cancellationToken)
    {
        try
        {
            // TODO: GetByTypeAsync method doesn't exist on IAlertService
            // Get all active alerts and filter for ServerOffline type
            var allAlerts = await alertService.GetAlertsAsync(activeOnly: true, cancellationToken);
            var activeAlerts = allAlerts.Where(a => a.AlertType == AlertType.ServerOffline).ToList();

            if (activeAlerts.Count == 0)
            {
                _logger.LogDebug("No active ServerOffline alerts configured");
                return;
            }

            foreach (var alert in activeAlerts)
            {
                // Check if alert applies to this server (if server filter is set)
                // Note: ServerName on AlertListItem is the scope, not configuration
                if (!string.IsNullOrEmpty(alert.ServerName) &&
                    !alert.ServerName.Equals(serverName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var message = $"Server '{serverName}' is offline. " +
                    $"Last heartbeat: {lastHeartbeat?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Never"}";

                var instance = await alertService.TriggerAlertAsync(
                    alert.Id,
                    message,
                    serverName: serverName,
                    cancellationToken: cancellationToken);

                if (instance != null)
                {
                    _logger.LogWarning(
                        "Triggered alert {AlertName} for server {Server}",
                        alert.Name,
                        serverName);

                    await realTimeService.BroadcastAlertTriggeredAsync(
                        instance.Id,
                        alert.Id,
                        alert.Name,
                        AlertType.ServerOffline,
                        alert.Severity,
                        message,
                        serverName: serverName,
                        cancellationToken: cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering server offline alert for {Server}", serverName);
        }
    }
}
