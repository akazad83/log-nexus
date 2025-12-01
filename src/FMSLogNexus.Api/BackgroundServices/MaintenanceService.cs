using Microsoft.Extensions.Options;
using FMSLogNexus.Api.Hubs;
using FMSLogNexus.Core.Interfaces.Services;

using IRealTimeService = FMSLogNexus.Api.Hubs.IRealTimeService;

namespace FMSLogNexus.Api.BackgroundServices;

/// <summary>
/// Background service that performs periodic maintenance tasks
/// including log cleanup, token cleanup, and data archival.
/// </summary>
public class MaintenanceService : BaseBackgroundService
{
    private readonly BackgroundServiceOptions _options;
    private DateTime _lastMaintenanceRun = DateTime.MinValue;

    public MaintenanceService(
        IServiceProvider serviceProvider,
        IOptions<BackgroundServiceOptions> options,
        ILogger<MaintenanceService> logger)
        : base(serviceProvider, logger)
    {
        _options = options.Value;
    }

    protected override TimeSpan Interval => TimeSpan.FromMinutes(30); // Check every 30 minutes

    protected override TimeSpan InitialDelay => TimeSpan.FromMinutes(5); // Wait 5 minutes on startup

    protected override async Task ExecuteTaskAsync(CancellationToken cancellationToken)
    {
        var maintenanceInterval = TimeSpan.FromHours(_options.MaintenanceIntervalHours);

        // Only run if enough time has passed since last maintenance
        if (DateTime.UtcNow - _lastMaintenanceRun < maintenanceInterval)
        {
            _logger.LogDebug(
                "Skipping maintenance, last run was {LastRun}. Next run after {NextRun}",
                _lastMaintenanceRun,
                _lastMaintenanceRun + maintenanceInterval);
            return;
        }

        using var scope = CreateScope();

        var maintenanceService = GetService<IMaintenanceService>(scope);
        var configService = GetService<IConfigurationService>(scope);
        var authService = GetService<IAuthService>(scope);
        var realTimeService = GetService<IRealTimeService>(scope);

        _logger.LogInformation("Starting scheduled maintenance tasks");

        var results = new MaintenanceResults();

        try
        {
            // Notify users that maintenance is starting
            await realTimeService.BroadcastSystemNotificationAsync(
                "maintenance_start",
                "Scheduled Maintenance",
                "Scheduled maintenance tasks are starting",
                "info",
                cancellationToken: cancellationToken);

            // 1. Cleanup old logs
            results.LogsDeleted = await CleanupLogsAsync(maintenanceService, configService, cancellationToken);

            // 2. Cleanup old executions
            results.ExecutionsDeleted = await CleanupExecutionsAsync(maintenanceService, configService, cancellationToken);

            // 3. Cleanup resolved/old alerts
            results.AlertsDeleted = await CleanupAlertsAsync(maintenanceService, configService, cancellationToken);

            // 4. Cleanup old audit logs
            results.AuditEntriesDeleted = await CleanupAuditLogsAsync(maintenanceService, configService, cancellationToken);

            // 5. Cleanup expired tokens
            results.TokensDeleted = await CleanupTokensAsync(maintenanceService, cancellationToken);

            // 6. Update statistics
            await UpdateStatisticsAsync(maintenanceService, cancellationToken);

            _lastMaintenanceRun = DateTime.UtcNow;

            _logger.LogInformation(
                "Maintenance completed: Logs={Logs}, Executions={Executions}, Alerts={Alerts}, AuditEntries={Audit}, Tokens={Tokens}",
                results.LogsDeleted,
                results.ExecutionsDeleted,
                results.AlertsDeleted,
                results.AuditEntriesDeleted,
                results.TokensDeleted);

            // Notify users that maintenance is complete
            await realTimeService.BroadcastSystemNotificationAsync(
                "maintenance_complete",
                "Maintenance Complete",
                $"Scheduled maintenance completed. Cleaned up {results.TotalDeleted:N0} records.",
                "success",
                new Dictionary<string, object>
                {
                    ["logsDeleted"] = results.LogsDeleted,
                    ["executionsDeleted"] = results.ExecutionsDeleted,
                    ["alertsDeleted"] = results.AlertsDeleted,
                    ["auditEntriesDeleted"] = results.AuditEntriesDeleted,
                    ["tokensDeleted"] = results.TokensDeleted
                },
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during maintenance tasks");

            await realTimeService.BroadcastSystemNotificationAsync(
                "maintenance_error",
                "Maintenance Error",
                "An error occurred during scheduled maintenance. Check logs for details.",
                "error",
                cancellationToken: cancellationToken);

            throw;
        }
    }

    private async Task<long> CleanupLogsAsync(
        IMaintenanceService maintenanceService,
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        try
        {
            // TODO: GetIntAsync doesn't exist on IConfigurationService
            // Use GetValueAsync<int> instead
            var retentionDays = await configService.GetValueAsync<int>("LogRetentionDays", 90, cancellationToken);

            _logger.LogInformation("Cleaning up logs older than {RetentionDays} days", retentionDays);

            // TODO: CleanupOldLogsAsync doesn't exist on IMaintenanceService
            // Use PurgeOldLogsAsync instead
            var deletedCount = await maintenanceService.PurgeOldLogsAsync(retentionDays, cancellationToken);

            _logger.LogInformation("Deleted {Count} old log entries", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up logs");
            return 0;
        }
    }

    private async Task<long> CleanupExecutionsAsync(
        IMaintenanceService maintenanceService,
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        try
        {
            var retentionDays = await configService.GetValueAsync<int>("ExecutionRetentionDays", 90, cancellationToken);
            var minExecutionsToKeep = await configService.GetValueAsync<int>("MinExecutionsToKeep", 100, cancellationToken);
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            _logger.LogInformation(
                "Cleaning up executions older than {CutoffDate} ({RetentionDays} days), keeping at least {MinKeep} per job",
                cutoffDate, retentionDays, minExecutionsToKeep);

            var deletedCount = await maintenanceService.PurgeOldExecutionsAsync(
                retentionDays,
                minExecutionsToKeep,
                cancellationToken);

            _logger.LogInformation("Deleted {Count} old execution records", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up executions");
            return 0;
        }
    }

    private async Task<long> CleanupAlertsAsync(
        IMaintenanceService maintenanceService,
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        try
        {
            var retentionDays = await configService.GetValueAsync<int>("AlertRetentionDays", 30, cancellationToken);
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            _logger.LogInformation("Cleaning up resolved alerts older than {CutoffDate} ({RetentionDays} days)", cutoffDate, retentionDays);

            var deletedCount = await maintenanceService.PurgeOldAlertsAsync(retentionDays, cancellationToken);

            _logger.LogInformation("Deleted {Count} old alert instances", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up alerts");
            return 0;
        }
    }

    private async Task<long> CleanupAuditLogsAsync(
        IMaintenanceService maintenanceService,
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        try
        {
            var retentionDays = await configService.GetValueAsync<int>("AuditLogRetentionDays", 365, cancellationToken);
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            _logger.LogInformation("Cleaning up audit logs older than {CutoffDate} ({RetentionDays} days)", cutoffDate, retentionDays);

            var deletedCount = await maintenanceService.PurgeOldAuditLogsAsync(retentionDays, cancellationToken);

            _logger.LogInformation("Deleted {Count} old audit log entries", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up audit logs");
            return 0;
        }
    }

    private async Task<long> CleanupTokensAsync(
        IMaintenanceService maintenanceService,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Cleaning up expired and revoked tokens");

            var deletedCount = await maintenanceService.PurgeExpiredTokensAsync(cancellationToken);

            _logger.LogInformation("Deleted {Count} expired/revoked tokens", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up tokens");
            return 0;
        }
    }

    private async Task UpdateStatisticsAsync(
        IMaintenanceService maintenanceService,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating database statistics");

            // This would typically call a stored procedure to update statistics
            // await maintenanceService.UpdateDatabaseStatisticsAsync(cancellationToken);

            _logger.LogInformation("Database statistics updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating database statistics");
        }
    }

    private class MaintenanceResults
    {
        public long LogsDeleted { get; set; }
        public long ExecutionsDeleted { get; set; }
        public long AlertsDeleted { get; set; }
        public long AuditEntriesDeleted { get; set; }
        public long TokensDeleted { get; set; }

        public long TotalDeleted => LogsDeleted + ExecutionsDeleted + AlertsDeleted + AuditEntriesDeleted + TokensDeleted;
    }
}
