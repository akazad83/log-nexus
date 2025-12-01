using Microsoft.Extensions.Logging;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Repositories;
using FMSLogNexus.Core.Interfaces.Services;
using RepoServerStatusSummary = FMSLogNexus.Core.Interfaces.Repositories.ServerStatusSummary;

namespace FMSLogNexus.Infrastructure.Services;

/// <summary>
/// Service implementation for dashboard operations.
/// </summary>
public class DashboardService : ServiceBase, IDashboardService
{
    private readonly ICacheService? _cacheService;

    public DashboardService(
        IUnitOfWork unitOfWork,
        ILogger<DashboardService> logger,
        ICacheService? cacheService = null)
        : base(unitOfWork, logger)
    {
        _cacheService = cacheService;
    }

    #region Summary Data

    /// <inheritdoc />
    public async Task<DashboardSummaryResponse> GetSummaryAsync(
        DashboardRequest request,
        CancellationToken cancellationToken = default)
    {
        const string cacheKey = "dashboard:summary";

        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<DashboardSummaryResponse>(cacheKey, cancellationToken);
            if (cached != null)
                return cached;
        }

        var summary = new DashboardSummaryResponse
        {
            LastUpdated = DateTime.UtcNow
        };

        // Get stats based on request period
        var (startDate, endDate) = GetDateRange(request.Period);

        // Log stats
        summary.Logs = await GetLogStatsAsync(request.Period, request.ServerName, cancellationToken);

        // Job stats
        summary.Jobs = await GetJobStatsAsync(cancellationToken);

        // Server stats
        summary.Servers = await GetServerStatsAsync(cancellationToken);

        // Alert stats
        summary.Alerts = await GetAlertStatsAsync(cancellationToken);

        if (_cacheService != null)
        {
            await _cacheService.SetAsync(cacheKey, summary, TimeSpan.FromMinutes(1), cancellationToken);
        }

        return summary;
    }

    /// <inheritdoc />
    public async Task<DashboardLogStats> GetLogStatsAsync(
        TimePeriod period,
        string? serverName = null,
        CancellationToken cancellationToken = default)
    {
        var (startDate, endDate) = GetDateRange(period);
        var logSummary = await UnitOfWork.Logs.GetSummaryAsync(startDate, endDate, serverName, null, cancellationToken);

        return new DashboardLogStats
        {
            Total = logSummary.TotalLogs,
            Errors = logSummary.ErrorCount,
            Warnings = logSummary.WarningCount,
            Critical = logSummary.CriticalCount,
            LogsPerMinute = (decimal)logSummary.TotalLogs / (decimal)(endDate - startDate).TotalMinutes
        };
    }

    /// <inheritdoc />
    public async Task<DashboardJobStats> GetJobStatsAsync(CancellationToken cancellationToken = default)
    {
        var jobStats = await UnitOfWork.Jobs.GetStatisticsSummaryAsync(cancellationToken);

        return new DashboardJobStats
        {
            Total = jobStats.TotalJobs,
            Active = jobStats.ActiveJobs,
            Running = jobStats.RunningNow,
            Failed = jobStats.FailedLastRun,
            SuccessRate = jobStats.TotalJobs > 0
                ? (decimal)jobStats.HealthyJobs / jobStats.TotalJobs * 100
                : null
        };
    }

    /// <inheritdoc />
    public async Task<DashboardServerStats> GetServerStatsAsync(CancellationToken cancellationToken = default)
    {
        var serverStats = await UnitOfWork.Servers.GetStatusSummaryAsync(cancellationToken);

        return new DashboardServerStats
        {
            Total = serverStats.Total,
            Online = serverStats.Online,
            Offline = serverStats.Offline,
            Degraded = serverStats.Degraded,
            Unknown = serverStats.Unknown
        };
    }

    /// <inheritdoc />
    public async Task<DashboardAlertStats> GetAlertStatsAsync(CancellationToken cancellationToken = default)
    {
        var alertStats = await UnitOfWork.AlertInstances.GetSummaryAsync(cancellationToken);

        return new DashboardAlertStats
        {
            Total = alertStats.Total,
            New = alertStats.New,
            Critical = alertStats.Critical,
            High = alertStats.High
        };
    }

    #endregion

    #region Trend Data

    /// <inheritdoc />
    public async Task<DashboardTrendsResponse> GetLogTrendsAsync(
        TimePeriod period,
        TrendInterval interval,
        string? serverName = null,
        CancellationToken cancellationToken = default)
    {
        var (startDate, endDate) = GetDateRange(period);

        var trendData = await UnitOfWork.Logs.GetTrendDataAsync(
            startDate, endDate, interval, serverName, null, cancellationToken);

        return new DashboardTrendsResponse
        {
            Period = period,
            Interval = interval,
            Data = trendData.Select(p => new LogTrendPoint
            {
                Timestamp = p.Timestamp,
                TotalCount = (int)p.TotalCount,
                ErrorCount = (int)p.ErrorCount,
                WarningCount = (int)p.WarningCount
            }).ToList()
        };
    }

    #endregion

    #region List Methods

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecentErrorResponse>> GetRecentErrorsAsync(
        int count = 20,
        CancellationToken cancellationToken = default)
    {
        var logs = await UnitOfWork.Logs.GetRecentErrorsAsync(count, null, null, cancellationToken);

        return logs.Select(log => new RecentErrorResponse
        {
            Id = log.Id,
            Timestamp = log.Timestamp,
            Level = log.Level,
            Message = log.Message,
            ServerName = log.ServerName,
            JobId = log.JobId,
            ExceptionType = log.ExceptionType
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RunningJobInfo>> GetRunningJobsAsync(
        CancellationToken cancellationToken = default)
    {
        var executions = await UnitOfWork.JobExecutions.GetRunningAsync(cancellationToken);

        return executions.Select(e => new RunningJobInfo
        {
            ExecutionId = e.Id,
            JobId = e.JobId,
            JobDisplayName = e.Job?.DisplayName ?? e.JobId,
            ServerName = e.ServerName,
            StartedAt = e.StartedAt,
            RunningDurationMs = (long)(DateTime.UtcNow - e.StartedAt).TotalMilliseconds,
            IsOverdue = e.Job?.MaxDurationMs.HasValue == true &&
                       (DateTime.UtcNow - e.StartedAt).TotalMilliseconds > e.Job.MaxDurationMs.Value
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecentFailure>> GetRecentFailuresAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var failures = await UnitOfWork.JobExecutions.GetRecentFailuresAsync(count, null, cancellationToken);

        return failures.Select(e => new RecentFailure
        {
            ExecutionId = e.Id,
            JobId = e.JobId,
            JobDisplayName = e.Job?.DisplayName ?? e.JobId,
            FailedAt = e.CompletedAt ?? e.StartedAt,
            ErrorMessage = e.ErrorMessage,
            ErrorCategory = e.ErrorCategory
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertInstanceListItem>> GetActiveAlertsAsync(
        int count = 20,
        CancellationToken cancellationToken = default)
    {
        var instances = await UnitOfWork.AlertInstances.GetActiveAsync(cancellationToken);

        return instances.Take(count).Select(i => new AlertInstanceListItem
        {
            Id = i.Id,
            AlertId = i.AlertId,
            AlertName = i.Alert?.Name ?? string.Empty,
            Severity = i.Severity,
            Message = i.Message,
            Status = i.Status,
            TriggeredAt = i.TriggeredAt,
            AcknowledgedAt = i.AcknowledgedAt,
            ResolvedAt = i.ResolvedAt
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServerStatusResponse>> GetServerStatusesAsync(
        CancellationToken cancellationToken = default)
    {
        var servers = await UnitOfWork.Servers.GetActiveAsync(cancellationToken);

        return servers.Select(s => new ServerStatusResponse
        {
            ServerName = s.Id,
            DisplayName = s.DisplayName,
            Status = s.Status,
            LastHeartbeat = s.LastHeartbeat,
            SecondsSinceHeartbeat = s.LastHeartbeat.HasValue
                ? (int?)(DateTime.UtcNow - s.LastHeartbeat.Value).TotalSeconds
                : null,
            IpAddress = s.IpAddress,
            AgentVersion = s.AgentVersion,
            ActiveJobCount = 0,
            RunningJobCount = 0
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobListItem>> GetUnhealthyJobsAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var jobs = await UnitOfWork.Jobs.GetUnhealthyJobsAsync(count, cancellationToken);

        return jobs.Select(j => new JobListItem
        {
            JobId = j.Id,
            DisplayName = j.DisplayName,
            Category = j.Category,
            JobType = j.JobType,
            LastStatus = j.LastStatus,
            LastExecutionAt = j.LastExecutionAt,
            HealthScore = j.SuccessRate.HasValue ? (int?)Math.Round(j.SuccessRate.Value) : null,
            IsActive = j.IsActive,
            IsCritical = j.IsCritical
        }).ToList();
    }

    #endregion

    #region Health

    /// <inheritdoc />
    public async Task<SystemHealthResponse> GetSystemHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var serverStats = await UnitOfWork.Servers.GetStatusSummaryAsync(cancellationToken);
        var jobStats = await UnitOfWork.Jobs.GetStatisticsSummaryAsync(cancellationToken);
        var alertStats = await UnitOfWork.AlertInstances.GetSummaryAsync(cancellationToken);

        var overallHealth = CalculateOverallHealth(serverStats, jobStats, alertStats);
        var healthStatus = overallHealth >= 80 ? HealthStatus.Healthy :
                          overallHealth >= 50 ? HealthStatus.Warning : HealthStatus.Critical;

        return new SystemHealthResponse
        {
            Status = healthStatus,
            Components = new List<HealthCheckResult>
            {
                new HealthCheckResult
                {
                    Name = "Servers",
                    Status = serverStats.Online == serverStats.Total ? HealthStatus.Healthy :
                            serverStats.Online >= serverStats.Total / 2 ? HealthStatus.Warning : HealthStatus.Critical,
                    Description = $"{serverStats.Online}/{serverStats.Total} servers online",
                    Data = new Dictionary<string, object>
                    {
                        ["Total"] = serverStats.Total,
                        ["Online"] = serverStats.Online,
                        ["Offline"] = serverStats.Offline,
                        ["Degraded"] = serverStats.Degraded
                    }
                },
                new HealthCheckResult
                {
                    Name = "Jobs",
                    Status = jobStats.HealthyJobs == jobStats.TotalJobs ? HealthStatus.Healthy :
                            jobStats.HealthyJobs >= jobStats.TotalJobs / 2 ? HealthStatus.Warning : HealthStatus.Critical,
                    Description = $"{jobStats.HealthyJobs}/{jobStats.TotalJobs} jobs healthy",
                    Data = new Dictionary<string, object>
                    {
                        ["Total"] = jobStats.TotalJobs,
                        ["Healthy"] = jobStats.HealthyJobs,
                        ["Active"] = jobStats.ActiveJobs,
                        ["Running"] = jobStats.RunningNow
                    }
                },
                new HealthCheckResult
                {
                    Name = "Alerts",
                    Status = alertStats.Critical == 0 && alertStats.High == 0 ? HealthStatus.Healthy :
                            alertStats.Critical == 0 ? HealthStatus.Warning : HealthStatus.Critical,
                    Description = $"{alertStats.Total} active alerts ({alertStats.Critical} critical)",
                    Data = new Dictionary<string, object>
                    {
                        ["Total"] = alertStats.Total,
                        ["Critical"] = alertStats.Critical,
                        ["High"] = alertStats.High,
                        ["New"] = alertStats.New
                    }
                }
            },
            CheckedAt = DateTime.UtcNow,
            DurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
        };
    }

    #endregion

    #region Real-time Updates

    /// <inheritdoc />
    public async Task<DashboardUpdateResponse> GetUpdatesAsync(
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        var recentErrors = await UnitOfWork.Logs.GetRecentErrorsAsync(10, null, null, cancellationToken);
        var runningJobs = await UnitOfWork.JobExecutions.GetRunningAsync(cancellationToken);
        var activeAlerts = await UnitOfWork.AlertInstances.GetActiveAsync(cancellationToken);

        return new DashboardUpdateResponse
        {
            Timestamp = DateTime.UtcNow,
            NewErrors = recentErrors.Where(e => e.Timestamp > since)
                .Select(log => new RecentErrorResponse
                {
                    Id = log.Id,
                    Timestamp = log.Timestamp,
                    Level = log.Level,
                    Message = log.Message,
                    ServerName = log.ServerName,
                    JobId = log.JobId
                }).ToList(),
            NewAlerts = activeAlerts.Where(a => a.TriggeredAt > since)
                .Select(i => new AlertInstanceListItem
                {
                    Id = i.Id,
                    AlertId = i.AlertId,
                    AlertName = i.Alert?.Name ?? string.Empty,
                    Severity = i.Severity,
                    Message = i.Message,
                    Status = i.Status,
                    TriggeredAt = i.TriggeredAt
                }).ToList()
        };
    }

    #endregion

    #region Private Methods

    private static (DateTime startDate, DateTime endDate) GetDateRange(TimePeriod period)
    {
        var endDate = DateTime.UtcNow;
        var startDate = period switch
        {
            TimePeriod.Last1Hour => endDate.AddHours(-1),
            TimePeriod.Last6Hours => endDate.AddHours(-6),
            TimePeriod.Last24Hours => endDate.AddHours(-24),
            TimePeriod.Last7Days => endDate.AddDays(-7),
            TimePeriod.Last30Days => endDate.AddDays(-30),
            _ => endDate.AddHours(-24)
        };
        return (startDate, endDate);
    }

    private static int CalculateOverallHealth(
        RepoServerStatusSummary serverStats,
        dynamic jobStats,
        dynamic alertStats)
    {
        var score = 100;

        // Server health impact
        if (serverStats.Total > 0)
        {
            var serverHealthPct = (decimal)serverStats.Online / serverStats.Total * 100;
            if (serverHealthPct < 100) score -= (int)((100 - serverHealthPct) * 0.3m);
        }

        // Job health impact
        if (jobStats.TotalJobs > 0)
        {
            var jobHealthPct = (decimal)jobStats.HealthyJobs / jobStats.TotalJobs * 100;
            if (jobHealthPct < 100) score -= (int)((100 - jobHealthPct) * 0.4m);
        }

        // Alert impact
        if (alertStats.Critical > 0) score -= alertStats.Critical * 10;
        if (alertStats.High > 0) score -= alertStats.High * 5;

        return Math.Max(0, Math.Min(100, score));
    }

    #endregion
}
