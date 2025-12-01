using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Services;
using FMSLogNexus.Infrastructure.Data.Repositories;

namespace FMSLogNexus.Api.Controllers;

/// <summary>
/// Controller for job execution operations.
/// </summary>
[Route("api/v1/executions")]
[ApiExplorerSettings(GroupName = "Executions")]
public class ExecutionsController : ApiControllerBase
{
    private readonly IJobExecutionService _executionService;
    private readonly ILogger<ExecutionsController> _logger;

    public ExecutionsController(IJobExecutionService executionService, ILogger<ExecutionsController> logger)
    {
        _executionService = executionService;
        _logger = logger;
    }

    #region Lifecycle

    /// <summary>
    /// Starts a new job execution.
    /// </summary>
    /// <param name="request">Start execution request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Started execution.</returns>
    [HttpPost]
    [Authorize(Policy = "ExecutionWrite")]
    [ProducesResponseType(typeof(JobExecutionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobExecutionResponse>> StartExecution(
        [FromBody] StartExecutionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var execution = await _executionService.StartExecutionAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetExecution), new { executionId = execution.Id }, execution);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Not Found",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return ConflictResponse(ex.Message);
        }
    }

    /// <summary>
    /// Completes an execution.
    /// </summary>
    /// <param name="executionId">Execution ID.</param>
    /// <param name="request">Completion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed execution.</returns>
    [HttpPost("{executionId:guid}/complete")]
    [Authorize(Policy = "ExecutionWrite")]
    [ProducesResponseType(typeof(JobExecutionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobExecutionResponse>> CompleteExecution(
        Guid executionId,
        [FromBody] CompleteExecutionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var execution = await _executionService.CompleteExecutionAsync(executionId, request, cancellationToken);
        if (execution == null)
            return NotFoundResponse("Execution", executionId);

        return Ok(execution);
    }

    /// <summary>
    /// Cancels an execution.
    /// </summary>
    /// <param name="executionId">Execution ID.</param>
    /// <param name="reason">Cancellation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cancelled execution.</returns>
    [HttpPost("{executionId:guid}/cancel")]
    [Authorize(Policy = "ExecutionWrite")]
    [ProducesResponseType(typeof(JobExecutionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobExecutionResponse>> CancelExecution(
        Guid executionId,
        [FromQuery] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var execution = await _executionService.CancelExecutionAsync(executionId, reason, cancellationToken);
        if (execution == null)
            return NotFoundResponse("Execution", executionId);

        return Ok(execution);
    }

    #endregion

    #region Query

    /// <summary>
    /// Gets an execution by ID.
    /// </summary>
    /// <param name="executionId">Execution ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution.</returns>
    [HttpGet("{executionId:guid}")]
    [Authorize(Policy = "ExecutionRead")]
    [ProducesResponseType(typeof(JobExecutionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobExecutionResponse>> GetExecution(
        Guid executionId,
        CancellationToken cancellationToken)
    {
        var execution = await _executionService.GetExecutionAsync(executionId, cancellationToken);
        if (execution == null)
            return NotFoundResponse("Execution", executionId);

        return Ok(execution);
    }

    /// <summary>
    /// Gets detailed execution information.
    /// </summary>
    /// <param name="executionId">Execution ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed execution.</returns>
    [HttpGet("{executionId:guid}/detail")]
    [Authorize(Policy = "ExecutionRead")]
    [ProducesResponseType(typeof(JobExecutionDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobExecutionDetailResponse>> GetExecutionDetail(
        Guid executionId,
        CancellationToken cancellationToken)
    {
        var execution = await _executionService.GetExecutionDetailAsync(executionId, cancellationToken);
        if (execution == null)
            return NotFoundResponse("Execution", executionId);

        return Ok(execution);
    }

    /// <summary>
    /// Searches executions with filtering and pagination.
    /// </summary>
    /// <param name="request">Search request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated executions.</returns>
    [HttpGet]
    [Authorize(Policy = "ExecutionRead")]
    [ProducesResponseType(typeof(PaginatedResult<JobExecutionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<JobExecutionResponse>>> SearchExecutions(
        [FromQuery] ExecutionSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _executionService.SearchAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets executions for a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="startDate">Start date filter.</param>
    /// <param name="endDate">End date filter.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated executions.</returns>
    [HttpGet("by-job/{jobId}")]
    [Authorize(Policy = "ExecutionRead")]
    [ProducesResponseType(typeof(PaginatedResult<JobExecutionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<JobExecutionResponse>>> GetByJob(
        string jobId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _executionService.GetByJobAsync(jobId, startDate, endDate, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets recent executions for a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="count">Number of executions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent executions.</returns>
    [HttpGet("by-job/{jobId}/recent")]
    [Authorize(Policy = "ExecutionRead")]
    [ProducesResponseType(typeof(IReadOnlyList<JobExecutionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobExecutionResponse>>> GetRecentByJob(
        string jobId,
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 100);
        var executions = await _executionService.GetRecentByJobAsync(jobId, count, cancellationToken);
        return Ok(executions);
    }

    /// <summary>
    /// Gets the last execution for a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Last execution or not found.</returns>
    [HttpGet("by-job/{jobId}/last")]
    [Authorize(Policy = "ExecutionRead")]
    [ProducesResponseType(typeof(JobExecutionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobExecutionResponse>> GetLastByJob(
        string jobId,
        CancellationToken cancellationToken)
    {
        var execution = await _executionService.GetLastByJobAsync(jobId, cancellationToken);
        if (execution == null)
            return NotFoundResponse("Execution for job", jobId);

        return Ok(execution);
    }

    #endregion

    #region Running Executions

    /// <summary>
    /// Gets all running executions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Running executions.</returns>
    [HttpGet("running")]
    [Authorize(Policy = "ExecutionRead")]
    [ProducesResponseType(typeof(IReadOnlyList<JobExecutionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobExecutionResponse>>> GetRunning(CancellationToken cancellationToken)
    {
        var executions = await _executionService.GetRunningAsync(cancellationToken);
        return Ok(executions);
    }

    /// <summary>
    /// Gets running executions for a server.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Running executions on the server.</returns>
    [HttpGet("running/by-server/{serverName}")]
    [Authorize(Policy = "ExecutionRead")]
    [ProducesResponseType(typeof(IReadOnlyList<JobExecutionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobExecutionResponse>>> GetRunningByServer(
        string serverName,
        CancellationToken cancellationToken)
    {
        var executions = await _executionService.GetRunningByServerAsync(serverName, cancellationToken);
        return Ok(executions);
    }

    /// <summary>
    /// Checks if a job is running.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if running.</returns>
    [HttpGet("is-running/{jobId}")]
    [Authorize(Policy = "ExecutionRead")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> IsJobRunning(
        string jobId,
        CancellationToken cancellationToken)
    {
        var isRunning = await _executionService.IsJobRunningAsync(jobId, cancellationToken);
        return Ok(new { JobId = jobId, IsRunning = isRunning });
    }

    /// <summary>
    /// Gets overdue executions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Overdue executions.</returns>
    [HttpGet("overdue")]
    [Authorize(Policy = "ExecutionRead")]
    [ProducesResponseType(typeof(IReadOnlyList<JobExecutionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobExecutionResponse>>> GetOverdue(CancellationToken cancellationToken)
    {
        var executions = await _executionService.GetOverdueAsync(cancellationToken);
        return Ok(executions);
    }

    #endregion

    #region Failures

    /// <summary>
    /// Gets recent failed executions.
    /// </summary>
    /// <param name="count">Number of failures.</param>
    /// <param name="serverName">Optional server filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent failures.</returns>
    [HttpGet("failures/recent")]
    [Authorize(Policy = "ExecutionRead")]
    [ProducesResponseType(typeof(IReadOnlyList<JobExecutionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobExecutionResponse>>> GetRecentFailures(
        [FromQuery] int count = 20,
        [FromQuery] string? serverName = null,
        CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 100);
        var executions = await _executionService.GetRecentFailuresAsync(count, serverName, cancellationToken);
        return Ok(executions);
    }

    /// <summary>
    /// Gets consecutive failure count for a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Consecutive failure count.</returns>
    [HttpGet("failures/consecutive/{jobId}")]
    [Authorize(Policy = "ExecutionRead")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetConsecutiveFailureCount(
        string jobId,
        CancellationToken cancellationToken)
    {
        var count = await _executionService.GetConsecutiveFailureCountAsync(jobId, cancellationToken);
        return Ok(new { JobId = jobId, ConsecutiveFailures = count });
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Gets execution statistics for a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution statistics.</returns>
    [HttpGet("statistics/{jobId}")]
    [Authorize(Policy = "ExecutionRead")]
    [ProducesResponseType(typeof(ExecutionStatistics), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExecutionStatistics>> GetStatistics(
        string jobId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;

        var stats = await _executionService.GetStatisticsAsync(jobId, startDate.Value, endDate.Value, cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// Gets execution trend data.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="interval">Aggregation interval.</param>
    /// <param name="jobId">Optional job filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Trend data.</returns>
    [HttpGet("trend")]
    [Authorize(Policy = "ExecutionRead")]
    [ProducesResponseType(typeof(IReadOnlyList<ExecutionTrendPoint>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ExecutionTrendPoint>>> GetTrend(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] TrendInterval interval = TrendInterval.Hour,
        [FromQuery] string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        startDate ??= DateTime.UtcNow.AddDays(-1);
        endDate ??= DateTime.UtcNow;

        var trend = await _executionService.GetTrendAsync(startDate.Value, endDate.Value, interval, jobId, cancellationToken);
        return Ok(trend);
    }

    /// <summary>
    /// Gets execution counts by status.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="jobId">Optional job filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Counts by status.</returns>
    [HttpGet("counts-by-status")]
    [Authorize(Policy = "ExecutionRead")]
    [ProducesResponseType(typeof(Dictionary<JobStatus, int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<JobStatus, int>>> GetCountsByStatus(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        startDate ??= DateTime.UtcNow.AddDays(-1);
        endDate ??= DateTime.UtcNow;

        var counts = await _executionService.GetCountsByStatusAsync(startDate.Value, endDate.Value, jobId, cancellationToken);
        return Ok(counts);
    }

    #endregion
}
