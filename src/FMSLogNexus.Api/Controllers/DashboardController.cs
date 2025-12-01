using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Services;
using FMSLogNexus.Infrastructure.Services;
using LogTrendResponse = System.Collections.Generic.List<FMSLogNexus.Core.DTOs.Responses.LogTrendPoint>;
using ExecutionTrendResponse = System.Collections.Generic.List<FMSLogNexus.Core.DTOs.Responses.ExecutionTrendPoint>;
using RecentFailureResponse = FMSLogNexus.Core.DTOs.Responses.RecentFailure;
using ActiveAlertResponse = FMSLogNexus.Core.DTOs.Responses.AlertInstanceListItem;
using RunningJobResponse = FMSLogNexus.Core.DTOs.Responses.RunningJobInfo;

namespace FMSLogNexus.Api.Controllers;

/// <summary>
/// Controller for dashboard operations.
/// </summary>
[Route("api/v1/dashboard")]
[ApiExplorerSettings(GroupName = "Dashboard")]
[Authorize]
public class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IJobService _jobService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, IJobService jobService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _jobService = jobService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the dashboard summary with aggregated statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard summary.</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(CancellationToken cancellationToken)
    {
        var request = new FMSLogNexus.Core.DTOs.Requests.DashboardRequest();
        var summary = await _dashboardService.GetSummaryAsync(request, cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Gets log volume trend data.
    /// </summary>
    /// <param name="hours">Number of hours to look back.</param>
    /// <param name="interval">Aggregation interval.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Log trend data.</returns>
    [HttpGet("log-trend")]
    [ProducesResponseType(typeof(LogTrendResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LogTrendResponse>> GetLogTrend(
        [FromQuery] int hours = 24,
        [FromQuery] TrendInterval interval = TrendInterval.Hour,
        CancellationToken cancellationToken = default)
    {
        hours = Math.Clamp(hours, 1, 168); // Max 7 days
        var period = hours switch
        {
            <= 1 => TimePeriod.Last1Hour,
            <= 24 => TimePeriod.Last24Hours,
            <= 168 => TimePeriod.Last7Days,
            _ => TimePeriod.Last24Hours
        };
        var trendsResponse = await _dashboardService.GetLogTrendsAsync(period, interval, null, cancellationToken);
        return Ok(trendsResponse.Data);
    }

    /// <summary>
    /// Gets recent error logs.
    /// </summary>
    /// <param name="count">Number of errors to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent errors.</returns>
    [HttpGet("recent-errors")]
    [ProducesResponseType(typeof(IReadOnlyList<RecentErrorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecentErrorResponse>>> GetRecentErrors(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 50);
        var errors = await _dashboardService.GetRecentErrorsAsync(count, cancellationToken);
        return Ok(errors);
    }

    /// <summary>
    /// Gets recent job failures.
    /// </summary>
    /// <param name="count">Number of failures to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent failures.</returns>
    [HttpGet("recent-failures")]
    [ProducesResponseType(typeof(IReadOnlyList<RecentFailureResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecentFailureResponse>>> GetRecentFailures(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 50);
        var failures = await _dashboardService.GetRecentFailuresAsync(count, cancellationToken);
        return Ok(failures);
    }

    /// <summary>
    /// Gets job health overview.
    /// </summary>
    /// <param name="count">Number of jobs to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job health data.</returns>
    [HttpGet("job-health")]
    [ProducesResponseType(typeof(JobHealthOverviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobHealthOverviewResponse>> GetJobHealthOverview(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 50);
        var health = await _jobService.GetHealthOverviewAsync(cancellationToken);
        return Ok(health);
    }

    /// <summary>
    /// Gets active alerts.
    /// </summary>
    /// <param name="count">Number of alerts to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active alerts.</returns>
    [HttpGet("active-alerts")]
    [ProducesResponseType(typeof(IReadOnlyList<ActiveAlertResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ActiveAlertResponse>>> GetActiveAlerts(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 50);
        var alerts = await _dashboardService.GetActiveAlertsAsync(count, cancellationToken);
        return Ok(alerts);
    }

    /// <summary>
    /// Gets currently running jobs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Running jobs.</returns>
    [HttpGet("running-jobs")]
    [ProducesResponseType(typeof(IReadOnlyList<RunningJobResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RunningJobResponse>>> GetRunningJobs(CancellationToken cancellationToken)
    {
        var jobs = await _dashboardService.GetRunningJobsAsync(cancellationToken);
        return Ok(jobs);
    }

    /// <summary>
    /// Gets server statuses.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server statuses.</returns>
    [HttpGet("server-statuses")]
    [ProducesResponseType(typeof(IReadOnlyList<ServerStatusResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServerStatusResponse>>> GetServerStatuses(CancellationToken cancellationToken)
    {
        var statuses = await _dashboardService.GetServerStatusesAsync(cancellationToken);
        return Ok(statuses);
    }
}
