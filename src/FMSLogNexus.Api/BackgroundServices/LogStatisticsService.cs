using Microsoft.Extensions.Options;
using FMSLogNexus.Api.Hubs;
using FMSLogNexus.Core.Interfaces.Services;
using FMSLogNexus.Core.Enums;

using IRealTimeService = FMSLogNexus.Api.Hubs.IRealTimeService;

namespace FMSLogNexus.Api.BackgroundServices;

/// <summary>
/// Background service that periodically aggregates log statistics
/// and broadcasts them to connected clients.
/// </summary>
public class LogStatisticsService : BaseBackgroundService
{
    private readonly BackgroundServiceOptions _options;

    public LogStatisticsService(
        IServiceProvider serviceProvider,
        IOptions<BackgroundServiceOptions> options,
        ILogger<LogStatisticsService> logger)
        : base(serviceProvider, logger)
    {
        _options = options.Value;
    }

    protected override TimeSpan Interval => TimeSpan.FromSeconds(_options.LogStatisticsIntervalSeconds);

    protected override TimeSpan InitialDelay => TimeSpan.FromSeconds(25);

    protected override async Task ExecuteTaskAsync(CancellationToken cancellationToken)
    {
        using var scope = CreateScope();

        var logService = GetService<ILogService>(scope);
        var serverService = GetService<IServerService>(scope);
        var jobService = GetService<IJobService>(scope);
        var realTimeService = GetService<IRealTimeService>(scope);

        try
        {
            var now = DateTime.UtcNow;
            var lastHour = now.AddHours(-1);

            // Get overall statistics for the last hour
            var statsRequest = new Core.DTOs.Requests.LogStatisticsRequest
            {
                StartDate = lastHour,
                EndDate = now
            };
            var overallStats = await logService.GetStatisticsAsync(statsRequest, cancellationToken);

            var errorCount = overallStats.Summary.ErrorCount + overallStats.Summary.CriticalCount;
            var errorRate = overallStats.Summary.TotalLogs > 0
                ? Math.Round((double)errorCount / overallStats.Summary.TotalLogs * 100, 2)
                : 0;

            // Broadcast overall stats as system notification
            await realTimeService.BroadcastSystemNotificationAsync(
                "log_stats",
                "Log Statistics Update",
                $"Logs: {overallStats.Summary.TotalLogs}, Errors: {errorCount}, Error Rate: {errorRate}%",
                errorRate > 10 ? "warning" : "info",
                new Dictionary<string, object>
                {
                    ["totalLogs"] = overallStats.Summary.TotalLogs,
                    ["errorCount"] = errorCount,
                    ["warningCount"] = overallStats.Summary.WarningCount,
                    ["errorRate"] = errorRate,
                    ["timestamp"] = now
                },
                cancellationToken);

            // Get per-server statistics
            await BroadcastServerLogStatsAsync(
                logService, serverService, realTimeService,
                lastHour, now, cancellationToken);

            // Get per-job statistics for active jobs
            await BroadcastJobLogStatsAsync(
                logService, jobService, realTimeService,
                lastHour, now, cancellationToken);

            _logger.LogDebug(
                "Log statistics broadcasted: Total={Total}, Errors={Errors}, Warnings={Warnings}",
                overallStats.Summary.TotalLogs,
                overallStats.Summary.ErrorCount + overallStats.Summary.CriticalCount,
                overallStats.Summary.WarningCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating log statistics");
            throw;
        }
    }


    private async Task BroadcastServerLogStatsAsync(
        ILogService logService,
        IServerService serverService,
        IRealTimeService realTimeService,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get all servers
            var servers = await serverService.GetAllServersAsync(cancellationToken);
            // Filter for online/active servers
            var activeServers = servers.Where(s =>
                s.Status == Core.Enums.ServerStatus.Online ||
                s.Status == Core.Enums.ServerStatus.Degraded).ToList();

            foreach (var server in activeServers.Take(50)) // Limit to prevent overload
            {
                try
                {
                    // Get logs for this server
                    var searchResult = await logService.SearchAsync(
                        new Core.DTOs.Requests.LogSearchRequest
                        {
                            ServerName = server.ServerName,
                            StartDate = startTime,
                            EndDate = endTime,
                            PageSize = 1 // Just need counts
                        },
                        cancellationToken);

                    // Get error count for server
                    var errorResult = await logService.SearchAsync(
                        new Core.DTOs.Requests.LogSearchRequest
                        {
                            ServerName = server.ServerName,
                            StartDate = startTime,
                            EndDate = endTime,
                            MinLevel = Core.Enums.LogLevel.Error,
                            PageSize = 1
                        },
                        cancellationToken);

                    var errorRate = searchResult.TotalCount > 0
                        ? Math.Round((double)errorResult.TotalCount / searchResult.TotalCount * 100, 2)
                        : 0;

                    // Broadcast server-specific stats
                    if (searchResult.TotalCount > 0)
                    {
                        await realTimeService.BroadcastSystemNotificationAsync(
                            "server_log_stats",
                            $"Server {server.ServerName} Log Stats",
                            $"Logs: {searchResult.TotalCount}, Errors: {errorResult.TotalCount}",
                            errorRate > 10 ? "warning" : "info",
                            new Dictionary<string, object>
                            {
                                ["serverName"] = server.ServerName,
                                ["totalLogs"] = searchResult.TotalCount,
                                ["errorCount"] = errorResult.TotalCount,
                                ["errorRate"] = errorRate
                            },
                            cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting log stats for server {Server}", server.ServerName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting server log statistics");
        }
    }

    private async Task BroadcastJobLogStatsAsync(
        ILogService logService,
        IJobService jobService,
        IRealTimeService realTimeService,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken)
    {
        try
        {
            // TODO: GetCriticalJobsAsync doesn't exist on IJobService
            // Instead, get unhealthy jobs which are likely to be critical
            var criticalJobs = await jobService.GetUnhealthyJobsAsync(top: 20, cancellationToken);

            foreach (var job in criticalJobs.Take(20)) // Limit to critical jobs
            {
                try
                {
                    // Get error count for this job
                    var errorResult = await logService.SearchAsync(
                        new Core.DTOs.Requests.LogSearchRequest
                        {
                            JobId = job.JobId,
                            StartDate = startTime,
                            EndDate = endTime,
                            MinLevel = Core.Enums.LogLevel.Error,
                            PageSize = 1
                        },
                        cancellationToken);

                    if (errorResult.TotalCount > 0)
                    {
                        // Broadcast job-specific error notification
                        await realTimeService.BroadcastSystemNotificationAsync(
                            "job_log_stats",
                            $"Job {job.DisplayName} Log Stats",
                            $"Critical job has {errorResult.TotalCount} errors in the last hour",
                            "warning",
                            new Dictionary<string, object>
                            {
                                ["jobId"] = job.JobId,
                                ["jobName"] = job.DisplayName,
                                ["errorCount"] = errorResult.TotalCount
                            },
                            cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting log stats for job {JobId}", job.JobId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting job log statistics");
        }
    }
}
