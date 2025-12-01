using Microsoft.Extensions.Options;
using FMSLogNexus.Api.Hubs;
using FMSLogNexus.Core.Interfaces.Services;

using IRealTimeService = FMSLogNexus.Api.Hubs.IRealTimeService;

namespace FMSLogNexus.Api.BackgroundServices;

/// <summary>
/// Background service that monitors job health scores
/// and broadcasts updates when health changes significantly.
/// </summary>
public class JobHealthMonitorService : BaseBackgroundService
{
    private readonly BackgroundServiceOptions _options;
    private readonly Dictionary<string, int> _lastHealthScores = new();
    private const int HealthChangeThreshold = 10; // Notify on 10+ point change

    public JobHealthMonitorService(
        IServiceProvider serviceProvider,
        IOptions<BackgroundServiceOptions> options,
        ILogger<JobHealthMonitorService> logger)
        : base(serviceProvider, logger)
    {
        _options = options.Value;
    }

    protected override TimeSpan Interval => TimeSpan.FromSeconds(45);

    protected override TimeSpan InitialDelay => TimeSpan.FromSeconds(35);

    protected override async Task ExecuteTaskAsync(CancellationToken cancellationToken)
    {
        using var scope = CreateScope();

        var jobService = GetService<IJobService>(scope);
        var realTimeService = GetService<IRealTimeService>(scope);

        try
        {
            // Get all active jobs
            var searchResult = await jobService.SearchJobsAsync(
                new Core.DTOs.Requests.JobSearchRequest { IsActive = true, PageSize = 1000 },
                cancellationToken);
            var jobList = searchResult.Items.ToList();

            _logger.LogDebug("Monitoring health for {Count} active jobs", jobList.Count);

            var healthUpdates = new List<JobHealthNotification>();

            foreach (var job in jobList)
            {
                try
                {
                    var currentScore = await jobService.CalculateHealthScoreAsync(job.JobId, cancellationToken);

                    // Use health score from job if available, otherwise use calculated
                    if (job.HealthScore.HasValue)
                    {
                        currentScore = job.HealthScore.Value;
                    }
                    var previousScore = _lastHealthScores.GetValueOrDefault(job.JobId, -1);

                    // Check if health changed significantly
                    var shouldNotify = previousScore == -1 || // First check
                        Math.Abs(currentScore - previousScore) >= HealthChangeThreshold ||
                        (previousScore >= 50 && currentScore < 50) || // Crossed warning threshold
                        (previousScore >= 80 && currentScore < 80) || // Crossed healthy threshold
                        (previousScore < 50 && currentScore >= 50) || // Recovered from critical
                        (previousScore < 80 && currentScore >= 80);   // Recovered to healthy

                    if (shouldNotify)
                    {
                        // Get statistics to get consecutive failures and success rate
                        var stats = await jobService.GetJobStatisticsAsync(
                            job.JobId,
                            DateTime.UtcNow.AddDays(-30),
                            DateTime.UtcNow,
                            cancellationToken);

                        var notification = new JobHealthNotification
                        {
                            JobId = job.JobId,
                            DisplayName = job.DisplayName,
                            HealthScore = currentScore,
                            HealthStatus = GetHealthStatus(currentScore),
                            ConsecutiveFailures = 0, // Not directly available, would need separate query
                            SuccessRate = (double?)stats.SuccessRate
                        };

                        healthUpdates.Add(notification);

                        // Broadcast individual health update
                        await realTimeService.BroadcastJobHealthUpdatedAsync(
                            job.JobId,
                            job.DisplayName,
                            currentScore,
                            0, // ConsecutiveFailures - not directly available
                            (double?)stats.SuccessRate,
                            cancellationToken);

                        // Log significant changes
                        if (previousScore != -1)
                        {
                            var change = currentScore - previousScore;
                            var direction = change > 0 ? "improved" : "degraded";

                            _logger.LogInformation(
                                "Job {JobId} health {Direction}: {PreviousScore} -> {CurrentScore} ({Change:+0;-0})",
                                job.JobId,
                                direction,
                                previousScore,
                                currentScore,
                                change);

                            // Send system notification for critical changes
                            if (currentScore < 50 && previousScore >= 50)
                            {
                                await realTimeService.BroadcastSystemNotificationAsync(
                                    "job_health_critical",
                                    "Job Health Critical",
                                    $"Job '{job.DisplayName}' health dropped to critical: {currentScore}%",
                                    "error",
                                    new Dictionary<string, object>
                                    {
                                        ["jobId"] = job.JobId,
                                        ["healthScore"] = currentScore,
                                        ["previousScore"] = previousScore
                                    },
                                    cancellationToken);
                            }
                            else if (currentScore >= 80 && previousScore < 80)
                            {
                                await realTimeService.BroadcastSystemNotificationAsync(
                                    "job_health_recovered",
                                    "Job Health Recovered",
                                    $"Job '{job.DisplayName}' health recovered to healthy: {currentScore}%",
                                    "success",
                                    new Dictionary<string, object>
                                    {
                                        ["jobId"] = job.JobId,
                                        ["healthScore"] = currentScore,
                                        ["previousScore"] = previousScore
                                    },
                                    cancellationToken);
                            }
                        }
                    }

                    _lastHealthScores[job.JobId] = currentScore;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error monitoring health for job {JobId}", job.JobId);
                }
            }

            // Clean up old entries for inactive jobs
            var activeJobIds = jobList.Select(j => j.JobId).ToHashSet();
            var keysToRemove = _lastHealthScores.Keys.Where(k => !activeJobIds.Contains(k)).ToList();
            foreach (var key in keysToRemove)
            {
                _lastHealthScores.Remove(key);
            }

            if (healthUpdates.Count > 0)
            {
                _logger.LogDebug("Broadcasted {Count} job health updates", healthUpdates.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in job health monitoring");
            throw;
        }
    }

    private static string GetHealthStatus(int score)
    {
        return score switch
        {
            >= 80 => "Healthy",
            >= 50 => "Warning",
            _ => "Critical"
        };
    }
}
