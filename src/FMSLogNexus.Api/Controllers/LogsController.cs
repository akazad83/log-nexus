using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Services;
using FmsLogLevel = FMSLogNexus.Core.Enums.LogLevel;

namespace FMSLogNexus.Api.Controllers;

/// <summary>
/// Controller for log operations.
/// </summary>
[Route("api/v1/logs")]
[ApiExplorerSettings(GroupName = "Logs")]
public class LogsController : ApiControllerBase
{
    private readonly ILogService _logService;
    private readonly ILogger<LogsController> _logger;

    public LogsController(ILogService logService, ILogger<LogsController> logger)
    {
        _logService = logService;
        _logger = logger;
    }

    #region Ingestion

    /// <summary>
    /// Creates a single log entry.
    /// </summary>
    /// <param name="request">Log creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Log ingestion result.</returns>
    [HttpPost]
    [Authorize(Policy = "LogIngestion")]
    [ProducesResponseType(typeof(LogIngestionResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LogIngestionResult>> CreateLog(
        [FromBody] CreateLogRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _logService.CreateLogAsync(request, ClientIpAddress, cancellationToken);
        return CreatedAtAction(nameof(GetLog), new { id = result.Id }, result);
    }

    /// <summary>
    /// Creates multiple log entries in a batch.
    /// </summary>
    /// <param name="request">Batch log request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Batch result.</returns>
    [HttpPost("batch")]
    [Authorize(Policy = "LogIngestion")]
    [ProducesResponseType(typeof(BatchLogResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BatchLogResult>> CreateBatch(
        [FromBody] BatchLogRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (request.Logs == null || request.Logs.Count == 0)
            return BadRequestResponse("At least one log entry is required.");

        if (request.Logs.Count > 10000)
            return BadRequestResponse("Maximum batch size is 10,000 logs.");

        var result = await _logService.CreateBatchAsync(request, ClientIpAddress, cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Query

    /// <summary>
    /// Gets a log entry by ID.
    /// </summary>
    /// <param name="id">Log entry ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Log entry.</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "LogRead")]
    [ProducesResponseType(typeof(LogEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LogEntryResponse>> GetLog(
        Guid id,
        CancellationToken cancellationToken)
    {
        var log = await _logService.GetByIdAsync(id, cancellationToken);
        if (log == null)
            return NotFoundResponse("Log entry", id);

        return Ok(log);
    }

    /// <summary>
    /// Gets detailed log entry with related logs.
    /// </summary>
    /// <param name="id">Log entry ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed log entry.</returns>
    [HttpGet("{id:guid}/detail")]
    [Authorize(Policy = "LogRead")]
    [ProducesResponseType(typeof(LogEntryDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LogEntryDetailResponse>> GetLogDetail(
        Guid id,
        CancellationToken cancellationToken)
    {
        var log = await _logService.GetDetailAsync(id, cancellationToken);
        if (log == null)
            return NotFoundResponse("Log entry", id);

        return Ok(log);
    }

    /// <summary>
    /// Searches logs with filtering and pagination.
    /// </summary>
    /// <param name="request">Search request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated log entries.</returns>
    [HttpGet]
    [Authorize(Policy = "LogRead")]
    [ProducesResponseType(typeof(PaginatedResult<LogEntryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<LogEntryResponse>>> SearchLogs(
        [FromQuery] LogSearchRequest request,
        CancellationToken cancellationToken)
    {
        // Set defaults
        request.StartDate ??= DateTime.UtcNow.AddDays(-1);
        request.EndDate ??= DateTime.UtcNow;

        var result = await _logService.SearchAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets logs by correlation ID.
    /// </summary>
    /// <param name="correlationId">Correlation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Related log entries.</returns>
    [HttpGet("correlation/{correlationId}")]
    [Authorize(Policy = "LogRead")]
    [ProducesResponseType(typeof(IReadOnlyList<LogEntryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LogEntryResponse>>> GetByCorrelationId(
        string correlationId,
        CancellationToken cancellationToken)
    {
        var logs = await _logService.GetByCorrelationIdAsync(correlationId, cancellationToken);
        return Ok(logs);
    }

    /// <summary>
    /// Gets logs by trace ID.
    /// </summary>
    /// <param name="traceId">Trace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Related log entries.</returns>
    [HttpGet("trace/{traceId}")]
    [Authorize(Policy = "LogRead")]
    [ProducesResponseType(typeof(IReadOnlyList<LogEntryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LogEntryResponse>>> GetByTraceId(
        string traceId,
        CancellationToken cancellationToken)
    {
        var logs = await _logService.GetByTraceIdAsync(traceId, cancellationToken);
        return Ok(logs);
    }

    /// <summary>
    /// Gets logs for a job execution.
    /// </summary>
    /// <param name="executionId">Job execution ID.</param>
    /// <param name="minLevel">Minimum log level.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Log entries for the execution.</returns>
    [HttpGet("execution/{executionId:guid}")]
    [Authorize(Policy = "LogRead")]
    [ProducesResponseType(typeof(IReadOnlyList<LogEntryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LogEntryResponse>>> GetByJobExecution(
        Guid executionId,
        [FromQuery] FmsLogLevel? minLevel,
        CancellationToken cancellationToken)
    {
        var logs = await _logService.GetByJobExecutionAsync(executionId, minLevel, cancellationToken);
        return Ok(logs);
    }

    /// <summary>
    /// Gets recent error logs.
    /// </summary>
    /// <param name="count">Number of errors to return.</param>
    /// <param name="serverName">Optional server filter.</param>
    /// <param name="jobId">Optional job filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent error logs.</returns>
    [HttpGet("errors/recent")]
    [Authorize(Policy = "LogRead")]
    [ProducesResponseType(typeof(IReadOnlyList<RecentErrorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecentErrorResponse>>> GetRecentErrors(
        [FromQuery] int count = 50,
        [FromQuery] string? serverName = null,
        [FromQuery] string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 500);
        var logs = await _logService.GetRecentErrorsAsync(count, serverName, jobId, cancellationToken);
        return Ok(logs);
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Gets log statistics for a time range.
    /// </summary>
    /// <param name="request">Statistics request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Log statistics.</returns>
    [HttpGet("statistics")]
    [Authorize(Policy = "LogRead")]
    [ProducesResponseType(typeof(LogStatisticsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LogStatisticsResponse>> GetStatistics(
        [FromQuery] LogStatisticsRequest request,
        CancellationToken cancellationToken)
    {
        request.StartDate ??= DateTime.UtcNow.AddDays(-1);
        request.EndDate ??= DateTime.UtcNow;

        var stats = await _logService.GetStatisticsAsync(request, cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// Gets log volume trend data.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="interval">Aggregation interval.</param>
    /// <param name="serverName">Optional server filter.</param>
    /// <param name="jobId">Optional job filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Trend data points.</returns>
    [HttpGet("trend")]
    [Authorize(Policy = "LogRead")]
    [ProducesResponseType(typeof(IReadOnlyList<LogTrendPoint>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LogTrendPoint>>> GetTrend(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] TrendInterval interval = TrendInterval.Hour,
        [FromQuery] string? serverName = null,
        [FromQuery] string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        startDate ??= DateTime.UtcNow.AddDays(-1);
        endDate ??= DateTime.UtcNow;

        var trend = await _logService.GetTrendAsync(startDate.Value, endDate.Value, interval, serverName, jobId, cancellationToken);
        return Ok(trend);
    }

    /// <summary>
    /// Gets log counts by level.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="serverName">Optional server filter.</param>
    /// <param name="jobId">Optional job filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Counts by level.</returns>
    [HttpGet("counts-by-level")]
    [Authorize(Policy = "LogRead")]
    [ProducesResponseType(typeof(Dictionary<FmsLogLevel, long>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<FmsLogLevel, long>>> GetCountsByLevel(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? serverName = null,
        [FromQuery] string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        startDate ??= DateTime.UtcNow.AddDays(-1);
        endDate ??= DateTime.UtcNow;

        var counts = await _logService.GetCountsByLevelAsync(startDate.Value, endDate.Value, serverName, jobId, cancellationToken);
        return Ok(counts);
    }

    #endregion

    #region Lookups

    /// <summary>
    /// Gets distinct server names from logs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server names.</returns>
    [HttpGet("lookup/servers")]
    [Authorize(Policy = "LogRead")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetDistinctServers(CancellationToken cancellationToken)
    {
        var servers = await _logService.GetDistinctServersAsync(cancellationToken);
        return Ok(servers);
    }

    /// <summary>
    /// Gets distinct job IDs from logs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job IDs.</returns>
    [HttpGet("lookup/jobs")]
    [Authorize(Policy = "LogRead")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetDistinctJobs(CancellationToken cancellationToken)
    {
        var jobs = await _logService.GetDistinctJobsAsync(cancellationToken);
        return Ok(jobs);
    }

    /// <summary>
    /// Gets distinct categories from logs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Categories.</returns>
    [HttpGet("lookup/categories")]
    [Authorize(Policy = "LogRead")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetDistinctCategories(CancellationToken cancellationToken)
    {
        var categories = await _logService.GetDistinctCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    /// <summary>
    /// Gets distinct tags from logs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tags.</returns>
    [HttpGet("lookup/tags")]
    [Authorize(Policy = "LogRead")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetDistinctTags(CancellationToken cancellationToken)
    {
        var tags = await _logService.GetDistinctTagsAsync(cancellationToken);
        return Ok(tags);
    }

    #endregion

    #region Export

    /// <summary>
    /// Exports logs matching the filter as CSV.
    /// </summary>
    /// <param name="request">Export request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CSV file.</returns>
    [HttpGet("export")]
    [Authorize(Policy = "LogExport")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportLogs(
        [FromQuery] LogExportRequest request,
        CancellationToken cancellationToken)
    {
        request.StartDate ??= DateTime.UtcNow.AddDays(-1);
        request.EndDate ??= DateTime.UtcNow;

        var stream = new MemoryStream();
        var count = await _logService.ExportToStreamAsync(request, stream, cancellationToken);
        stream.Position = 0;

        var fileName = $"logs_{request.StartDate:yyyyMMdd}_{request.EndDate:yyyyMMdd}.csv";
        return File(stream, "text/csv", fileName);
    }

    #endregion
}
