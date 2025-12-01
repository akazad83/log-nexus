using Microsoft.Extensions.Options;
using FMSLogNexus.Api.Hubs;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Services;
using FMSLogNexus.Core.DTOs.Responses;

using IRealTimeService = FMSLogNexus.Api.Hubs.IRealTimeService;

namespace FMSLogNexus.Api.BackgroundServices;

/// <summary>
/// Background service that periodically updates dashboard statistics
/// and broadcasts them to connected clients via SignalR.
/// </summary>
public class DashboardUpdateService : BaseBackgroundService
{
    private readonly BackgroundServiceOptions _options;

    public DashboardUpdateService(
        IServiceProvider serviceProvider,
        IOptions<BackgroundServiceOptions> options,
        ILogger<DashboardUpdateService> logger)
        : base(serviceProvider, logger)
    {
        _options = options.Value;
    }

    protected override TimeSpan Interval => TimeSpan.FromSeconds(_options.DashboardUpdateIntervalSeconds);

    protected override bool RunOnStartup => true;

    protected override TimeSpan InitialDelay => TimeSpan.FromSeconds(5);

    protected override async Task ExecuteTaskAsync(CancellationToken cancellationToken)
    {
        using var scope = CreateScope();

        var logService = GetService<ILogService>(scope);
        var jobService = GetService<IJobService>(scope);
        var serverService = GetService<IServerService>(scope);
        var alertService = GetService<IAlertService>(scope);
        var executionService = GetService<IJobExecutionService>(scope);
        var realTimeService = GetService<IRealTimeService>(scope);

        try
        {
            var dashboard = await BuildDashboardNotificationAsync(
                logService,
                jobService,
                serverService,
                alertService,
                executionService,
                cancellationToken);

            await realTimeService.BroadcastDashboardUpdateAsync(
                new Hubs.DashboardNotification
                {
                    Timestamp = dashboard.LastUpdated,
                    LogStats = new Hubs.DashboardLogStats
                    {
                        TotalLogs24h = dashboard.Logs.Total,
                        ErrorCount24h = dashboard.Logs.Errors,
                        WarningCount24h = dashboard.Logs.Warnings,
                        ErrorRate = dashboard.Logs.Total > 0 ? (double)dashboard.Logs.Errors / dashboard.Logs.Total : 0
                    },
                    JobStats = new Hubs.DashboardJobStats
                    {
                        TotalJobs = dashboard.Jobs.Total,
                        ActiveJobs = dashboard.Jobs.Active,
                        RunningNow = dashboard.Jobs.Running,
                        FailedLastRun = dashboard.Jobs.Failed,
                        HealthyJobs = dashboard.Jobs.Healthy
                    },
                    ServerStats = new Hubs.DashboardServerStats
                    {
                        TotalServers = dashboard.Servers.Total,
                        OnlineServers = dashboard.Servers.Online,
                        OfflineServers = dashboard.Servers.Offline,
                        MaintenanceServers = 0
                    },
                    AlertStats = new Hubs.DashboardAlertStats
                    {
                        ActiveAlerts = dashboard.Alerts.Total,
                        NewAlerts = dashboard.Alerts.New,
                        CriticalAlerts = dashboard.Alerts.Critical
                    }
                }, cancellationToken);

            _logger.LogDebug(
                "Dashboard update broadcasted: Logs={Logs}, Errors={Errors}, Jobs={Jobs}, Alerts={Alerts}",
                dashboard.Logs.Total,
                dashboard.Logs.Errors,
                dashboard.Jobs.Running,
                dashboard.Alerts.Total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building dashboard update");
            throw;
        }
    }

    private async Task<DashboardSummaryResponse> BuildDashboardNotificationAsync(
        ILogService logService,
        IJobService jobService,
        IServerService serverService,
        IAlertService alertService,
        IJobExecutionService executionService,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var last24Hours = now.AddHours(-24);

        // Get log statistics
        var logStatsRequest = new Core.DTOs.Requests.LogStatisticsRequest
        {
            StartDate = last24Hours,
            EndDate = now
        };
        var logStats = await logService.GetStatisticsAsync(logStatsRequest, cancellationToken);

        // Get job statistics
        // TODO: GetAllAsync doesn't exist on IJobService - need to use SearchJobsAsync or GetJobLookupAsync
        // For now, use GetJobLookupAsync with activeOnly filter
        var jobLookup = await jobService.GetJobLookupAsync(activeOnly: true, cancellationToken);
        // Note: This returns lookup items which don't have all the properties needed
        // A better approach would be to use SearchJobsAsync with appropriate filters
        var runningExecutions = await executionService.GetRunningAsync(cancellationToken);
        var runningList = runningExecutions.ToList();

        // Use GetHealthOverviewAsync which provides summary stats
        var jobHealthOverview = await jobService.GetHealthOverviewAsync(cancellationToken);
        var healthyJobs = jobHealthOverview.Summary.Healthy;
        var warningJobs = jobHealthOverview.Summary.Warning;
        var criticalJobs = jobHealthOverview.Summary.Critical;
        var totalJobs = jobHealthOverview.Summary.Total;

        // Get server statistics
        var serverSummary = await serverService.GetStatusSummaryAsync(cancellationToken);

        // Get alert statistics
        // TODO: GetInstanceSummaryAsync doesn't exist - use GetSummaryAsync instead
        var alertSummary = await alertService.GetSummaryAsync(cancellationToken);

        // Get health check
        var dashboardService = GetService<IDashboardService>(CreateScope());
        var health = await dashboardService.GetSystemHealthAsync(cancellationToken);

        return new Core.DTOs.Responses.DashboardSummaryResponse
        {
            Logs = new Core.DTOs.Responses.DashboardLogStats
            {
                Total = logStats.Summary.TotalLogs,
                Errors = logStats.Summary.ErrorCount + logStats.Summary.CriticalCount,
                Warnings = logStats.Summary.WarningCount,
                Critical = logStats.Summary.CriticalCount,
                LogsPerMinute = logStats.Summary.TotalLogs / (decimal)(24 * 60)
            },
            Jobs = new Core.DTOs.Responses.DashboardJobStats
            {
                Total = totalJobs,
                Active = jobLookup.Count,
                Running = runningList.Count,
                Failed = jobHealthOverview.RecentFailures.Count,
                Healthy = healthyJobs,
                Warning = warningJobs,
                CriticalHealth = criticalJobs
            },
            Servers = new Core.DTOs.Responses.DashboardServerStats
            {
                Total = serverSummary.Total,
                Online = serverSummary.Online,
                Offline = serverSummary.Offline,
                Degraded = serverSummary.Degraded,
                Unknown = serverSummary.Unknown
            },
            Alerts = new Core.DTOs.Responses.DashboardAlertStats
            {
                Total = alertSummary.Total,
                New = alertSummary.New,
                Critical = alertSummary.Critical,
                High = alertSummary.High
            },
            Health = health,
            LastUpdated = now
        };
    }
}
