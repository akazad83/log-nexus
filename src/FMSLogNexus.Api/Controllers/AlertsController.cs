using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Services;
using FMSLogNexus.Infrastructure.Data.Repositories;
using AlertInstanceSummary = FMSLogNexus.Core.DTOs.Responses.AlertSummary;

namespace FMSLogNexus.Api.Controllers;

/// <summary>
/// Controller for alert operations.
/// </summary>
[Route("api/v1/alerts")]
[ApiExplorerSettings(GroupName = "Alerts")]
public class AlertsController : ApiControllerBase
{
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(IAlertService alertService, ILogger<AlertsController> logger)
    {
        _alertService = alertService;
        _logger = logger;
    }

    #region Alert Rule Management

    /// <summary>
    /// Creates a new alert rule.
    /// </summary>
    /// <param name="request">Alert creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created alert.</returns>
    [HttpPost]
    [Authorize(Policy = "AlertWrite")]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AlertResponse>> CreateAlert(
        [FromBody] CreateAlertRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var alert = await _alertService.CreateAlertAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAlert), new { alertId = alert.Id }, alert);
    }

    /// <summary>
    /// Gets an alert by ID.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Alert.</returns>
    [HttpGet("{alertId:guid}")]
    [Authorize(Policy = "AlertRead")]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertResponse>> GetAlert(
        Guid alertId,
        CancellationToken cancellationToken)
    {
        var alert = await _alertService.GetAlertAsync(alertId, cancellationToken);
        if (alert == null)
            return NotFoundResponse("Alert", alertId);

        return Ok(alert);
    }

    /// <summary>
    /// Gets detailed alert information with recent instances.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="recentInstanceCount">Number of recent instances to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed alert information.</returns>
    [HttpGet("{alertId:guid}/detail")]
    [Authorize(Policy = "AlertRead")]
    [ProducesResponseType(typeof(AlertDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertDetailResponse>> GetAlertDetail(
        Guid alertId,
        [FromQuery] int recentInstanceCount = 10,
        CancellationToken cancellationToken = default)
    {
        recentInstanceCount = Math.Clamp(recentInstanceCount, 1, 50);
        var alert = await _alertService.GetAlertAsync(alertId, cancellationToken);
        if (alert == null)
            return NotFoundResponse("Alert", alertId);

        return Ok(alert);
    }

    /// <summary>
    /// Updates an alert.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="request">Update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated alert.</returns>
    [HttpPut("{alertId:guid}")]
    [Authorize(Policy = "AlertWrite")]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertResponse>> UpdateAlert(
        Guid alertId,
        [FromBody] UpdateAlertRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var alert = await _alertService.UpdateAlertAsync(alertId, request, cancellationToken);
        if (alert == null)
            return NotFoundResponse("Alert", alertId);

        return Ok(alert);
    }

    /// <summary>
    /// Deletes (deactivates) an alert.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{alertId:guid}")]
    [Authorize(Policy = "AlertWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAlert(
        Guid alertId,
        CancellationToken cancellationToken)
    {
        var result = await _alertService.DeleteAlertAsync(alertId, cancellationToken);
        if (!result)
            return NotFoundResponse("Alert", alertId);

        return NoContent();
    }

    /// <summary>
    /// Gets all alerts.
    /// </summary>
    /// <param name="activeOnly">Include only active alerts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Alerts.</returns>
    [HttpGet]
    [Authorize(Policy = "AlertRead")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertResponse>>> GetAllAlerts(
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var alerts = await _alertService.GetAlertsAsync(activeOnly, cancellationToken);
        return Ok(alerts);
    }

    /// <summary>
    /// Gets alerts by type.
    /// </summary>
    /// <param name="alertType">Alert type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Alerts of the specified type.</returns>
    [HttpGet("by-type/{alertType}")]
    [Authorize(Policy = "AlertRead")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertResponse>>> GetAlertsByType(
        AlertType alertType,
        CancellationToken cancellationToken)
    {
        var allAlerts = await _alertService.GetAlertsAsync(false, cancellationToken);
        var filteredAlerts = allAlerts.Where(a => a.AlertType == alertType).ToList();
        return Ok(filteredAlerts);
    }

    /// <summary>
    /// Gets alerts for a job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Alerts for the job.</returns>
    [HttpGet("by-job/{jobId}")]
    [Authorize(Policy = "AlertRead")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertResponse>>> GetAlertsByJob(
        string jobId,
        CancellationToken cancellationToken)
    {
        var allAlerts = await _alertService.GetAlertsAsync(false, cancellationToken);
        var filteredAlerts = allAlerts.Where(a => a.JobId == jobId).ToList();
        return Ok(filteredAlerts);
    }

    /// <summary>
    /// Activates an alert.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("{alertId:guid}/activate")]
    [Authorize(Policy = "AlertWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateAlert(
        Guid alertId,
        CancellationToken cancellationToken)
    {
        var result = await _alertService.ActivateAlertAsync(alertId, cancellationToken);
        if (!result)
            return NotFoundResponse("Alert", alertId);

        return NoContent();
    }

    /// <summary>
    /// Deactivates an alert.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("{alertId:guid}/deactivate")]
    [Authorize(Policy = "AlertWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateAlert(
        Guid alertId,
        CancellationToken cancellationToken)
    {
        var result = await _alertService.DeactivateAlertAsync(alertId, cancellationToken);
        if (!result)
            return NotFoundResponse("Alert", alertId);

        return NoContent();
    }

    #endregion

    #region Alert Instances

    /// <summary>
    /// Searches alert instances.
    /// </summary>
    /// <param name="request">Search request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated alert instances.</returns>
    [HttpGet("instances")]
    [Authorize(Policy = "AlertRead")]
    [ProducesResponseType(typeof(PaginatedResult<AlertInstanceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<AlertInstanceResponse>>> SearchInstances(
        [FromQuery] AlertInstanceSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _alertService.SearchInstancesAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets active (unresolved) alert instances.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active alert instances.</returns>
    [HttpGet("instances/active")]
    [Authorize(Policy = "AlertRead")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertInstanceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertInstanceResponse>>> GetActiveInstances(CancellationToken cancellationToken)
    {
        var instances = await _alertService.GetActiveInstancesAsync(cancellationToken);
        return Ok(instances);
    }

    /// <summary>
    /// Gets unacknowledged alert instances.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unacknowledged alert instances.</returns>
    [HttpGet("instances/unacknowledged")]
    [Authorize(Policy = "AlertRead")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertInstanceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertInstanceResponse>>> GetUnacknowledged(CancellationToken cancellationToken)
    {
        var instances = await _alertService.GetUnacknowledgedInstancesAsync(cancellationToken);
        return Ok(instances);
    }

    /// <summary>
    /// Gets an alert instance by ID.
    /// </summary>
    /// <param name="instanceId">Instance ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Alert instance.</returns>
    [HttpGet("instances/{instanceId:guid}")]
    [Authorize(Policy = "AlertRead")]
    [ProducesResponseType(typeof(AlertInstanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertInstanceResponse>> GetInstance(
        Guid instanceId,
        CancellationToken cancellationToken)
    {
        var instance = await _alertService.GetInstanceAsync(instanceId, cancellationToken);
        if (instance == null)
            return NotFoundResponse("Alert instance", instanceId);

        return Ok(instance);
    }

    /// <summary>
    /// Acknowledges an alert instance.
    /// </summary>
    /// <param name="instanceId">Instance ID.</param>
    /// <param name="request">Acknowledgement request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("instances/{instanceId:guid}/acknowledge")]
    [Authorize(Policy = "AlertWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcknowledgeInstance(
        Guid instanceId,
        [FromBody] AcknowledgeAlertRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _alertService.AcknowledgeInstanceAsync(
            instanceId,
            request ?? new AcknowledgeAlertRequest(),
            CurrentUsername ?? "Unknown",
            cancellationToken);

        if (!result)
            return NotFoundResponse("Alert instance", instanceId);

        return NoContent();
    }

    /// <summary>
    /// Resolves an alert instance.
    /// </summary>
    /// <param name="instanceId">Instance ID.</param>
    /// <param name="request">Resolution request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("instances/{instanceId:guid}/resolve")]
    [Authorize(Policy = "AlertWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveInstance(
        Guid instanceId,
        [FromBody] ResolveAlertRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _alertService.ResolveInstanceAsync(
            instanceId,
            request ?? new ResolveAlertRequest(),
            CurrentUsername ?? "Unknown",
            cancellationToken);

        if (!result)
            return NotFoundResponse("Alert instance", instanceId);

        return NoContent();
    }

    /// <summary>
    /// Bulk acknowledges alert instances.
    /// </summary>
    /// <param name="request">Bulk acknowledge request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of instances acknowledged.</returns>
    [HttpPost("instances/bulk-acknowledge")]
    [Authorize(Policy = "AlertWrite")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> BulkAcknowledge(
        [FromBody] BulkAlertActionRequest request,
        CancellationToken cancellationToken)
    {
        var count = await _alertService.BulkAcknowledgeAsync(
            request.InstanceIds,
            request.Note,
            CurrentUsername ?? "Unknown",
            cancellationToken);

        return Ok(new { AcknowledgedCount = count });
    }

    /// <summary>
    /// Bulk resolves alert instances.
    /// </summary>
    /// <param name="request">Bulk resolve request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of instances resolved.</returns>
    [HttpPost("instances/bulk-resolve")]
    [Authorize(Policy = "AlertWrite")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> BulkResolve(
        [FromBody] BulkAlertActionRequest request,
        CancellationToken cancellationToken)
    {
        var count = await _alertService.BulkResolveAsync(
            request.InstanceIds,
            request.Note,
            CurrentUsername ?? "Unknown",
            cancellationToken);

        return Ok(new { ResolvedCount = count });
    }

    /// <summary>
    /// Gets alert instance summary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance summary.</returns>
    [HttpGet("instances/summary")]
    [Authorize(Policy = "AlertRead")]
    [ProducesResponseType(typeof(AlertInstanceSummary), StatusCodes.Status200OK)]
    public async Task<ActionResult<AlertInstanceSummary>> GetInstanceSummary(CancellationToken cancellationToken)
    {
        var summary = await _alertService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    #endregion

    #region Manual Triggering

    /// <summary>
    /// Manually triggers an alert (for testing).
    /// </summary>
    /// <param name="request">Trigger request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created instance or null if throttled.</returns>
    [HttpPost("trigger")]
    [Authorize(Policy = "AlertWrite")]
    [ProducesResponseType(typeof(AlertInstanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<AlertInstanceResponse?>> TriggerAlert(
        [FromBody] TriggerAlertRequest request,
        CancellationToken cancellationToken)
    {
        var instance = await _alertService.TriggerAlertAsync(
            request.AlertId,
            request.Message,
            request.Context,
            request.JobId,
            request.ServerName,
            cancellationToken);

        if (instance == null)
            return NoContent(); // Throttled

        return Ok(instance);
    }

    #endregion
}

/// <summary>
/// Bulk alert action request.
/// </summary>
public class BulkAlertActionRequest
{
    public List<Guid> InstanceIds { get; set; } = new();
    public string? Note { get; set; }
}

/// <summary>
/// Manual trigger alert request.
/// </summary>
public class TriggerAlertRequest
{
    public Guid AlertId { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Context { get; set; }
    public string? JobId { get; set; }
    public string? ServerName { get; set; }
}
