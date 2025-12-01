using Microsoft.Extensions.Options;
using FMSLogNexus.Api.Hubs;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Services;
using FMSLogNexus.Core.DTOs.Responses;

using IRealTimeService = FMSLogNexus.Api.Hubs.IRealTimeService;

namespace FMSLogNexus.Api.BackgroundServices;

/// <summary>
/// Background service that periodically evaluates alert rules
/// and triggers alerts based on configured conditions.
/// </summary>
public class AlertEvaluationService : BaseBackgroundService
{
    private readonly BackgroundServiceOptions _options;

    public AlertEvaluationService(
        IServiceProvider serviceProvider,
        IOptions<BackgroundServiceOptions> options,
        ILogger<AlertEvaluationService> logger)
        : base(serviceProvider, logger)
    {
        _options = options.Value;
    }

    protected override TimeSpan Interval => TimeSpan.FromSeconds(_options.AlertEvaluationIntervalSeconds);

    protected override TimeSpan InitialDelay => TimeSpan.FromSeconds(20);

    protected override async Task ExecuteTaskAsync(CancellationToken cancellationToken)
    {
        using var scope = CreateScope();

        var alertService = GetService<IAlertService>(scope);
        var logService = GetService<ILogService>(scope);
        var jobService = GetService<IJobService>(scope);
        var executionService = GetService<IJobExecutionService>(scope);
        var realTimeService = GetService<IRealTimeService>(scope);

        try
        {
            // Get all active alerts
            var alerts = await alertService.GetAlertsAsync(activeOnly: true, cancellationToken);
            var alertList = alerts.ToList();

            if (alertList.Count == 0)
            {
                _logger.LogDebug("No active alerts to evaluate");
                return;
            }

            _logger.LogDebug("Evaluating {Count} active alerts", alertList.Count);

            foreach (var alert in alertList)
            {
                try
                {
                    // Get alert detail to access configuration
                    var alertDetail = await alertService.GetAlertAsync(alert.Id, cancellationToken);
                    var config = alertDetail?.Condition as Dictionary<string, object>;

                    await EvaluateAlertAsync(
                        alert.Id,
                        alert.AlertType,
                        alert.Name,
                        alert.Severity,
                        alert.JobId,
                        config,
                        alertService,
                        logService,
                        jobService,
                        executionService,
                        realTimeService,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating alert {AlertId}: {AlertName}", alert.Id, alert.Name);
                }
            }

            // Broadcast alert summary update
            await BroadcastAlertSummaryAsync(alertService, realTimeService, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in alert evaluation");
            throw;
        }
    }

    private async Task EvaluateAlertAsync(
        Guid alertId,
        AlertType alertType,
        string alertName,
        AlertSeverity severity,
        string? jobId,
        Dictionary<string, object>? config,
        IAlertService alertService,
        ILogService logService,
        IJobService jobService,
        IJobExecutionService executionService,
        IRealTimeService realTimeService,
        CancellationToken cancellationToken)
    {
        switch (alertType)
        {
            case AlertType.ErrorThreshold:
                await EvaluateErrorThresholdAsync(
                    alertId, alertName, severity, jobId, config,
                    alertService, logService, realTimeService, cancellationToken);
                break;

            case AlertType.JobFailure:
                // Job failure alerts are triggered immediately on execution completion
                // This evaluation is for catching any missed failures
                await EvaluateJobFailureAsync(
                    alertId, alertName, severity, jobId, config,
                    alertService, executionService, jobService, realTimeService, cancellationToken);
                break;

            case AlertType.ServerOffline:
                // Handled by ServerHealthCheckService
                break;

            case AlertType.DurationExceeded:
                // Handled by ExecutionTimeoutService
                break;

            default:
                _logger.LogDebug("Alert type {AlertType} - no automatic evaluation", alertType);
                break;
        }
    }

    private async Task EvaluateErrorThresholdAsync(
        Guid alertId,
        string alertName,
        AlertSeverity severity,
        string? jobId,
        Dictionary<string, object>? config,
        IAlertService alertService,
        ILogService logService,
        IRealTimeService realTimeService,
        CancellationToken cancellationToken)
    {
        // Get configuration values
        var threshold = GetConfigValue<int>(config, "threshold", 100);
        var windowMinutes = GetConfigValue<int>(config, "windowMinutes", 60);
        var serverName = GetConfigValue<string>(config, "serverName", null);

        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddMinutes(-windowMinutes);

        // Get error count
        var statsRequest = new Core.DTOs.Requests.LogStatisticsRequest
        {
            StartDate = startTime,
            EndDate = endTime,
            ServerName = serverName,
            JobId = jobId
        };
        var stats = await logService.GetStatisticsAsync(statsRequest, cancellationToken);
        var errorCount = stats.Summary.ErrorCount + stats.Summary.CriticalCount;

        if (errorCount >= threshold)
        {
            var message = $"Error threshold exceeded: {errorCount} errors in the last {windowMinutes} minutes (threshold: {threshold})";

            if (!string.IsNullOrEmpty(jobId))
            {
                message += $" for job '{jobId}'";
            }
            if (!string.IsNullOrEmpty(serverName))
            {
                message += $" on server '{serverName}'";
            }

            var instance = await alertService.TriggerAlertAsync(
                alertId,
                message,
                jobId: jobId,
                serverName: serverName,
                cancellationToken: cancellationToken);

            if (instance != null)
            {
                _logger.LogWarning("Triggered error threshold alert: {AlertName}", alertName);
                await realTimeService.BroadcastAlertTriggeredAsync(
                    instance.Id,
                    alertId,
                    alertName,
                    AlertType.ErrorThreshold,
                    severity,
                    message,
                    jobId,
                    serverName,
                    cancellationToken);
            }
        }
    }

    private async Task EvaluateJobFailureAsync(
        Guid alertId,
        string alertName,
        AlertSeverity severity,
        string? jobId,
        Dictionary<string, object>? config,
        IAlertService alertService,
        IJobExecutionService executionService,
        IJobService jobService,
        IRealTimeService realTimeService,
        CancellationToken cancellationToken)
    {
        // This evaluates for recently failed jobs that might not have triggered an alert
        var lookbackMinutes = GetConfigValue<int>(config, "lookbackMinutes", 5);
        var startTime = DateTime.UtcNow.AddMinutes(-lookbackMinutes);

        var recentFailures = await executionService.GetRecentFailuresAsync(20, null, cancellationToken);

        foreach (var failure in recentFailures)
        {
            // Skip if older than lookback
            if (failure.CompletedAt < startTime)
                continue;

            // Skip if job filter doesn't match
            if (!string.IsNullOrEmpty(jobId) &&
                !failure.JobId.Equals(jobId, StringComparison.OrdinalIgnoreCase))
                continue;

            var job = await jobService.GetJobAsync(failure.JobId, cancellationToken);
            var jobName = job?.DisplayName ?? failure.JobId;

            var message = $"Job '{jobName}' ({failure.JobId}) failed on server '{failure.ServerName}'. " +
                $"Status: {failure.Status}. ExecutionId: {failure.Id}";

            if (!string.IsNullOrEmpty(failure.ErrorMessage))
            {
                message += $" Error: {failure.ErrorMessage.Substring(0, Math.Min(200, failure.ErrorMessage.Length))}";
            }

            var context = new Dictionary<string, object>
            {
                ["executionId"] = failure.Id,
                ["status"] = failure.Status.ToString()
            };

            var instance = await alertService.TriggerAlertAsync(
                alertId,
                message,
                context: context,
                jobId: failure.JobId,
                serverName: failure.ServerName,
                cancellationToken: cancellationToken);

            if (instance != null)
            {
                _logger.LogWarning("Triggered job failure alert: {AlertName} for {JobId}", alertName, failure.JobId);
                await realTimeService.BroadcastAlertTriggeredAsync(
                    instance.Id,
                    alertId,
                    alertName,
                    AlertType.JobFailure,
                    severity,
                    message,
                    failure.JobId,
                    failure.ServerName,
                    cancellationToken);
            }
        }
    }

    private async Task BroadcastAlertSummaryAsync(
        IAlertService alertService,
        IRealTimeService realTimeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = await alertService.GetSummaryAsync(cancellationToken);

            await realTimeService.BroadcastSystemNotificationAsync(
                "alert_summary",
                "Alert Summary Update",
                $"Active: {summary.Total}, Critical: {summary.Critical}, New: {summary.New}",
                summary.Critical > 0 ? "error" : summary.High > 0 ? "warning" : "info",
                new Dictionary<string, object>
                {
                    ["activeCount"] = summary.Total,
                    ["criticalCount"] = summary.Critical,
                    ["highCount"] = summary.High,
                    ["newCount"] = summary.New
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting alert summary");
        }
    }

    private T? GetConfigValue<T>(Dictionary<string, object>? config, string key, T? defaultValue = default)
    {
        if (config == null || !config.TryGetValue(key, out var value))
            return defaultValue;

        try
        {
            if (value is T typedValue)
                return typedValue;

            if (typeof(T) == typeof(int) && value is long longValue)
                return (T)(object)(int)longValue;

            if (typeof(T) == typeof(int) && value is double doubleValue)
                return (T)(object)(int)doubleValue;

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }
}
