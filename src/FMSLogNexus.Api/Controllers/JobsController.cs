using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Services;
using JobStatisticsSummary = FMSLogNexus.Core.DTOs.Responses.JobStatistics;

namespace FMSLogNexus.Api.Controllers;

/// <summary>
/// Controller for job operations.
/// </summary>
[Route("api/v1/jobs")]
[ApiExplorerSettings(GroupName = "Jobs")]
public class JobsController : ApiControllerBase
{
    private readonly IJobService _jobService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IJobService jobService, ILogger<JobsController> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    #region Job Management

    /// <summary>
    /// Creates a new job.
    /// </summary>
    /// <param name="request">Job creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created job.</returns>
    [HttpPost]
    [Authorize(Policy = "JobWrite")]
    [ProducesResponseType(typeof(JobDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobDetailResponse>> CreateJob(
        [FromBody] CreateJobRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var job = await _jobService.CreateJobAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetJob), new { jobId = job.JobId }, job);
        }
        catch (InvalidOperationException ex)
        {
            return ConflictResponse(ex.Message);
        }
    }

    /// <summary>
    /// Gets a job by ID.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job.</returns>
    [HttpGet("{jobId}")]
    [Authorize(Policy = "JobRead")]
    [ProducesResponseType(typeof(JobDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobDetailResponse>> GetJob(
        string jobId,
        CancellationToken cancellationToken)
    {
        var job = await _jobService.GetJobAsync(jobId, cancellationToken);
        if (job == null)
            return NotFoundResponse("Job", jobId);

        return Ok(job);
    }

    /// <summary>
    /// Gets detailed job information with recent executions.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="recentExecutionCount">Number of recent executions to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed job information.</returns>
    [HttpGet("{jobId}/detail")]
    [Authorize(Policy = "JobRead")]
    [ProducesResponseType(typeof(JobDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobDetailResponse>> GetJobDetail(
        string jobId,
        [FromQuery] int recentExecutionCount = 10,
        CancellationToken cancellationToken = default)
    {
        recentExecutionCount = Math.Clamp(recentExecutionCount, 1, 50);
        var job = await _jobService.GetJobAsync(jobId, cancellationToken);
        if (job == null)
            return NotFoundResponse("Job", jobId);

        return Ok(job);
    }

    /// <summary>
    /// Updates a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="request">Update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated job.</returns>
    [HttpPut("{jobId}")]
    [Authorize(Policy = "JobWrite")]
    [ProducesResponseType(typeof(JobDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobDetailResponse>> UpdateJob(
        string jobId,
        [FromBody] UpdateJobRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var job = await _jobService.UpdateJobAsync(jobId, request, cancellationToken);
        if (job == null)
            return NotFoundResponse("Job", jobId);

        return Ok(job);
    }

    /// <summary>
    /// Deletes (deactivates) a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{jobId}")]
    [Authorize(Policy = "JobWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteJob(
        string jobId,
        CancellationToken cancellationToken)
    {
        var result = await _jobService.DeleteJobAsync(jobId, cancellationToken);
        if (!result)
            return NotFoundResponse("Job", jobId);

        return NoContent();
    }

    /// <summary>
    /// Searches jobs with filtering and pagination.
    /// </summary>
    /// <param name="request">Search request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated jobs.</returns>
    [HttpGet]
    [Authorize(Policy = "JobRead")]
    [ProducesResponseType(typeof(PaginatedResult<JobListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<JobListItem>>> SearchJobs(
        [FromQuery] JobSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _jobService.SearchJobsAsync(request, cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Job Registration

    /// <summary>
    /// Registers or updates a job (used by agents).
    /// </summary>
    /// <param name="request">Registration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registered job.</returns>
    [HttpPost("register")]
    [Authorize(Policy = "JobWrite")]
    [ProducesResponseType(typeof(JobDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobDetailResponse>> RegisterJob(
        [FromBody] RegisterJobRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var job = await _jobService.CreateJobAsync(request, cancellationToken);
        return Ok(job);
    }

    #endregion

    #region Job Status

    /// <summary>
    /// Activates a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("{jobId}/activate")]
    [Authorize(Policy = "JobWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateJob(
        string jobId,
        CancellationToken cancellationToken)
    {
        var result = await _jobService.ActivateJobAsync(jobId, cancellationToken);
        if (!result)
            return NotFoundResponse("Job", jobId);

        return NoContent();
    }

    /// <summary>
    /// Deactivates a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("{jobId}/deactivate")]
    [Authorize(Policy = "JobWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateJob(
        string jobId,
        CancellationToken cancellationToken)
    {
        var result = await _jobService.DeactivateJobAsync(jobId, cancellationToken);
        if (!result)
            return NotFoundResponse("Job", jobId);

        return NoContent();
    }

    /// <summary>
    /// Gets jobs by server.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="activeOnly">Include only active jobs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Jobs on the server.</returns>
    [HttpGet("by-server/{serverName}")]
    [Authorize(Policy = "JobRead")]
    [ProducesResponseType(typeof(IReadOnlyList<JobListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobListItem>>> GetByServer(
        string serverName,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var request = new JobSearchRequest
        {
            ServerName = serverName,
            IsActive = activeOnly ? true : null
        };
        var result = await _jobService.SearchJobsAsync(request, cancellationToken);
        return Ok(result.Items);
    }

    /// <summary>
    /// Gets jobs by category.
    /// </summary>
    /// <param name="category">Category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Jobs in the category.</returns>
    [HttpGet("by-category/{category}")]
    [Authorize(Policy = "JobRead")]
    [ProducesResponseType(typeof(IReadOnlyList<JobListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobListItem>>> GetByCategory(
        string category,
        CancellationToken cancellationToken)
    {
        var request = new JobSearchRequest
        {
            Category = category
        };
        var result = await _jobService.SearchJobsAsync(request, cancellationToken);
        return Ok(result.Items);
    }

    /// <summary>
    /// Gets critical jobs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Critical jobs.</returns>
    [HttpGet("critical")]
    [Authorize(Policy = "JobRead")]
    [ProducesResponseType(typeof(IReadOnlyList<JobListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobListItem>>> GetCriticalJobs(CancellationToken cancellationToken)
    {
        var request = new JobSearchRequest
        {
            IsCritical = true
        };
        var result = await _jobService.SearchJobsAsync(request, cancellationToken);
        return Ok(result.Items);
    }

    /// <summary>
    /// Gets failed jobs (last run failed).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Failed jobs.</returns>
    [HttpGet("failed")]
    [Authorize(Policy = "JobRead")]
    [ProducesResponseType(typeof(IReadOnlyList<JobListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobListItem>>> GetFailedJobs(CancellationToken cancellationToken)
    {
        var request = new JobSearchRequest
        {
            LastStatus = JobStatus.Failed
        };
        var result = await _jobService.SearchJobsAsync(request, cancellationToken);
        return Ok(result.Items);
    }

    #endregion

    #region Health and Statistics

    /// <summary>
    /// Gets health information for a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job health information.</returns>
    [HttpGet("{jobId}/health")]
    [Authorize(Policy = "JobRead")]
    [ProducesResponseType(typeof(JobHealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobHealthResponse>> GetJobHealth(
        string jobId,
        CancellationToken cancellationToken)
    {
        var job = await _jobService.GetJobAsync(jobId, cancellationToken);
        if (job == null)
            return NotFoundResponse("Job", jobId);

        var healthScore = await _jobService.CalculateHealthScoreAsync(jobId, cancellationToken);

        var health = new JobHealthResponse
        {
            JobId = job.JobId,
            DisplayName = job.DisplayName,
            HealthScore = healthScore,
            HealthStatus = healthScore switch
            {
                >= 80 => HealthStatus.Healthy,
                >= 50 => HealthStatus.Warning,
                _ => HealthStatus.Critical
            },
            LastStatus = job.Statistics.LastStatus,
            LastExecutionAt = job.Statistics.LastExecutionAt,
            SuccessRate = job.Statistics.SuccessRate,
            AvgDurationMs = job.Statistics.AvgDurationMs,
            TotalExecutions = job.Statistics.TotalExecutions
        };

        return Ok(health);
    }

    /// <summary>
    /// Gets health overview for all jobs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All job health information.</returns>
    [HttpGet("health")]
    [Authorize(Policy = "JobRead")]
    [ProducesResponseType(typeof(JobHealthOverviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobHealthOverviewResponse>> GetAllJobHealth(CancellationToken cancellationToken)
    {
        var health = await _jobService.GetHealthOverviewAsync(cancellationToken);
        return Ok(health);
    }

    /// <summary>
    /// Gets job statistics summary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Statistics summary.</returns>
    [HttpGet("statistics")]
    [Authorize(Policy = "JobRead")]
    [ProducesResponseType(typeof(JobHealthSummary), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobHealthSummary>> GetStatisticsSummary(CancellationToken cancellationToken)
    {
        var healthOverview = await _jobService.GetHealthOverviewAsync(cancellationToken);
        return Ok(healthOverview.Summary);
    }

    #endregion

    #region Lookups

    /// <summary>
    /// Gets distinct job categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Categories.</returns>
    [HttpGet("lookup/categories")]
    [Authorize(Policy = "JobRead")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetDistinctCategories(CancellationToken cancellationToken)
    {
        var categories = await _jobService.GetCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    /// <summary>
    /// Gets distinct job tags.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tags.</returns>
    [HttpGet("lookup/tags")]
    [Authorize(Policy = "JobRead")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetDistinctTags(CancellationToken cancellationToken)
    {
        var tags = await _jobService.GetTagsAsync(cancellationToken);
        return Ok(tags);
    }

    /// <summary>
    /// Gets job lookup items for dropdowns.
    /// </summary>
    /// <param name="activeOnly">Include only active jobs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Lookup items.</returns>
    [HttpGet("lookup/items")]
    [Authorize(Policy = "JobRead")]
    [ProducesResponseType(typeof(IReadOnlyList<LookupItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LookupItem>>> GetLookupItems(
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var items = await _jobService.GetJobLookupAsync(activeOnly, cancellationToken);
        return Ok(items);
    }

    #endregion
}
