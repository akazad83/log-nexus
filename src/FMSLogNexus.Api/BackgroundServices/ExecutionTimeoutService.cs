using Microsoft.Extensions.Options;
using FMSLogNexus.Api.Hubs;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Services;
using FMSLogNexus.Core.DTOs.Responses;

using IRealTimeService = FMSLogNexus.Api.Hubs.IRealTimeService;

namespace FMSLogNexus.Api.BackgroundServices;

/// <summary>
/// Background service that monitors running executions and marks them as timed out
/// when they exceed their job's timeout threshold.
/// </summary>
public class ExecutionTimeoutService : BaseBackgroundService
{
    private readonly BackgroundServiceOptions _options;

    public ExecutionTimeoutService(
        IServiceProvider serviceProvider,
        IOptions<BackgroundServiceOptions> options,
        ILogger<ExecutionTimeoutService> logger)
        : base(serviceProvider, logger)
    {
        _options = options.Value;
    }

    protected override TimeSpan Interval => TimeSpan.FromSeconds(_options.ExecutionTimeoutCheckIntervalSeconds);

    protected override TimeSpan InitialDelay => TimeSpan.FromSeconds(15);

    protected override async Task ExecuteTaskAsync(CancellationToken cancellationToken)
    {
        using var scope = CreateScope();

        var executionService = GetService<IJobExecutionService>(scope);
        var jobService = GetService<IJobService>(scope);
        var alertService = GetService<IAlertService>(scope);
        var realTimeService = GetService<IRealTimeService>(scope);

        try
        {
            // Get overdue executions
            var overdueExecutions = await executionService.GetOverdueAsync(cancellationToken);
            var overdueList = overdueExecutions.ToList();

            if (overdueList.Count == 0)
            {
                _logger.LogDebug("No overdue executions detected");
                return;
            }

            _logger.LogWarning(
                "Detected {Count} overdue executions",
                overdueList.Count);

            foreach (var execution in overdueList)
            {
                // Skip if not running (might have been completed since query)
                if (execution.Status != JobStatus.Running)
                {
                    continue;
                }

                var job = await jobService.GetJobAsync(execution.JobId, cancellationToken);
                if (job == null)
                {
                    _logger.LogWarning(
                        "Job not found for execution {ExecutionId}",
                        execution.Id);
                    continue;
                }

                var runningTime = DateTime.UtcNow - execution.StartedAt;
                var timeoutMinutes = job.MaxDurationMs.HasValue
                    ? (int)(job.MaxDurationMs.Value / 60000)
                    : 60; // Default 60 minutes

                // Mark execution as timed out
                var completeRequest = new Core.DTOs.Requests.CompleteExecutionRequest
                {
                    Status = JobStatus.Timeout,
                    ErrorMessage = $"Execution timed out after {runningTime.TotalMinutes:F1} minutes (timeout: {timeoutMinutes} minutes)"
                };
                await executionService.CompleteExecutionAsync(
                    execution.Id,
                    completeRequest,
                    cancellationToken);

                _logger.LogWarning(
                    "Execution {ExecutionId} for job {JobId} timed out after {Duration} minutes",
                    execution.Id,
                    execution.JobId,
                    runningTime.TotalMinutes);

                // Broadcast completion
                var completedAt = DateTime.UtcNow;
                await realTimeService.BroadcastExecutionCompletedAsync(
                    execution.Id,
                    execution.JobId,
                    job.DisplayName,
                    execution.ServerName,
                    JobStatus.Timeout,
                    execution.StartedAt,
                    completedAt,
                    (long)runningTime.TotalMilliseconds,
                    $"Execution timed out after {runningTime.TotalMinutes:F1} minutes",
                    cancellationToken);

                // Trigger timeout alert
                await TriggerTimeoutAlertAsync(
                    alertService,
                    realTimeService,
                    job.JobId,
                    job.DisplayName,
                    execution.Id,
                    execution.ServerName,
                    runningTime,
                    timeoutMinutes,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking execution timeouts");
            throw;
        }
    }

    private async Task TriggerTimeoutAlertAsync(
        IAlertService alertService,
        IRealTimeService realTimeService,
        string jobId,
        string jobDisplayName,
        Guid executionId,
        string serverName,
        TimeSpan runningTime,
        int timeoutMinutes,
        CancellationToken cancellationToken)
    {
        try
        {
            // TODO: GetByTypeAsync and GetByJobAsync methods don't exist on IAlertService
            // TODO: AlertType.JobTimeout doesn't exist (use DurationExceeded instead)
            // For now, get all active alerts and filter for the ones that might apply
            var allAlerts = await alertService.GetAlertsAsync(activeOnly: true, cancellationToken);
            var activeAlerts = allAlerts.Where(a =>
                a.AlertType == AlertType.DurationExceeded &&
                (string.IsNullOrEmpty(a.JobId) || a.JobId.Equals(jobId, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            if (activeAlerts.Count == 0)
            {
                _logger.LogDebug("No active JobTimeout alerts configured");
                return;
            }

            foreach (var alert in activeAlerts)
            {
                // Check if alert applies to this job (if job filter is set)
                if (!string.IsNullOrEmpty(alert.JobId) &&
                    !alert.JobId.Equals(jobId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var message = $"Job '{jobDisplayName}' ({jobId}) execution timed out on server '{serverName}'. " +
                    $"Running time: {runningTime.TotalMinutes:F1} minutes, Timeout: {timeoutMinutes} minutes. " +
                    $"ExecutionId: {executionId}";

                var context = new Dictionary<string, object>
                {
                    ["executionId"] = executionId,
                    ["runningTimeMinutes"] = runningTime.TotalMinutes,
                    ["timeoutMinutes"] = timeoutMinutes
                };

                var instance = await alertService.TriggerAlertAsync(
                    alert.Id,
                    message,
                    context: context,
                    jobId: jobId,
                    serverName: serverName,
                    cancellationToken: cancellationToken);

                if (instance != null)
                {
                    _logger.LogWarning(
                        "Triggered timeout alert {AlertName} for job {JobId}",
                        alert.Name,
                        jobId);

                    await realTimeService.BroadcastAlertTriggeredAsync(
                        instance.Id,
                        alert.Id,
                        alert.Name,
                        AlertType.DurationExceeded,
                        alert.Severity,
                        message,
                        jobId,
                        serverName,
                        cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering timeout alert for job {JobId}", jobId);
        }
    }
}
