using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Services;
using FMSLogNexus.Infrastructure.Data.Repositories;
using FMSLogNexus.Infrastructure.Services;

namespace FMSLogNexus.Api.Controllers;

/// <summary>
/// Controller for server operations.
/// </summary>
[Route("api/v1/servers")]
[ApiExplorerSettings(GroupName = "Servers")]
public class ServersController : ApiControllerBase
{
    private readonly IServerService _serverService;
    private readonly ILogger<ServersController> _logger;

    public ServersController(IServerService serverService, ILogger<ServersController> logger)
    {
        _serverService = serverService;
        _logger = logger;
    }

    #region Server Management

    /// <summary>
    /// Creates a new server.
    /// </summary>
    /// <param name="request">Server creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created server.</returns>
    [HttpPost]
    [Authorize(Policy = "ServerWrite")]
    [ProducesResponseType(typeof(ServerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServerResponse>> CreateServer(
        [FromBody] CreateServerRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var server = await _serverService.CreateServerAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetServer), new { serverName = server.ServerName }, server);
        }
        catch (InvalidOperationException ex)
        {
            return ConflictResponse(ex.Message);
        }
    }

    /// <summary>
    /// Gets a server by name.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server.</returns>
    [HttpGet("{serverName}")]
    [Authorize(Policy = "ServerRead")]
    [ProducesResponseType(typeof(ServerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServerResponse>> GetServer(
        string serverName,
        CancellationToken cancellationToken)
    {
        var server = await _serverService.GetServerAsync(serverName, cancellationToken);
        if (server == null)
            return NotFoundResponse("Server", serverName);

        return Ok(server);
    }

    /// <summary>
    /// Gets detailed server information.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed server information.</returns>
    [HttpGet("{serverName}/detail")]
    [Authorize(Policy = "ServerRead")]
    [ProducesResponseType(typeof(ServerDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServerDetailResponse>> GetServerDetail(
        string serverName,
        CancellationToken cancellationToken)
    {
        var server = await _serverService.GetServerAsync(serverName, cancellationToken);
        if (server == null)
            return NotFoundResponse("Server", serverName);

        return Ok(server);
    }

    /// <summary>
    /// Updates a server.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="request">Update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated server.</returns>
    [HttpPut("{serverName}")]
    [Authorize(Policy = "ServerWrite")]
    [ProducesResponseType(typeof(ServerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServerResponse>> UpdateServer(
        string serverName,
        [FromBody] UpdateServerRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var server = await _serverService.UpdateServerAsync(serverName, request, cancellationToken);
        if (server == null)
            return NotFoundResponse("Server", serverName);

        return Ok(server);
    }

    /// <summary>
    /// Deletes (deactivates) a server.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{serverName}")]
    [Authorize(Policy = "ServerWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteServer(
        string serverName,
        CancellationToken cancellationToken)
    {
        var result = await _serverService.DeleteServerAsync(serverName, cancellationToken);
        if (!result)
            return NotFoundResponse("Server", serverName);

        return NoContent();
    }

    /// <summary>
    /// Gets all servers.
    /// </summary>
    /// <param name="activeOnly">Include only active servers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Servers.</returns>
    [HttpGet]
    [Authorize(Policy = "ServerRead")]
    [ProducesResponseType(typeof(IReadOnlyList<ServerResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServerResponse>>> GetAllServers(
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var servers = await _serverService.GetAllServersAsync(cancellationToken);
        return Ok(servers);
    }

    /// <summary>
    /// Gets servers by status.
    /// </summary>
    /// <param name="status">Server status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Servers with the status.</returns>
    [HttpGet("by-status/{status}")]
    [Authorize(Policy = "ServerRead")]
    [ProducesResponseType(typeof(IReadOnlyList<ServerResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServerResponse>>> GetByStatus(
        ServerStatus status,
        CancellationToken cancellationToken)
    {
        var servers = await _serverService.GetServersByStatusAsync(status, cancellationToken);
        return Ok(servers);
    }

    #endregion

    #region Heartbeat

    /// <summary>
    /// Processes a heartbeat from a server agent.
    /// </summary>
    /// <param name="request">Heartbeat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server state.</returns>
    [HttpPost("heartbeat")]
    [Authorize(Policy = "Heartbeat")]
    [ProducesResponseType(typeof(ServerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServerResponse>> ProcessHeartbeat(
        [FromBody] ServerHeartbeatRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Use actual client IP
        var clientIp = ClientIpAddress;

        var server = await _serverService.ProcessHeartbeatAsync(request, clientIp, cancellationToken);
        return Ok(server);
    }

    /// <summary>
    /// Gets unresponsive servers.
    /// </summary>
    /// <param name="timeoutSeconds">Timeout in seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unresponsive servers.</returns>
    [HttpGet("unresponsive")]
    [Authorize(Policy = "ServerRead")]
    [ProducesResponseType(typeof(IReadOnlyList<ServerResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServerResponse>>> GetUnresponsive(
        [FromQuery] int timeoutSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        var servers = await _serverService.GetUnresponsiveServersAsync(timeoutSeconds, cancellationToken);
        return Ok(servers);
    }

    #endregion

    #region Server Actions

    /// <summary>
    /// Activates a server.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("{serverName}/activate")]
    [Authorize(Policy = "ServerWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateServer(
        string serverName,
        CancellationToken cancellationToken)
    {
        var result = await _serverService.ActivateServerAsync(serverName, cancellationToken);
        if (!result)
            return NotFoundResponse("Server", serverName);

        return NoContent();
    }

    /// <summary>
    /// Deactivates a server.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("{serverName}/deactivate")]
    [Authorize(Policy = "ServerWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateServer(
        string serverName,
        CancellationToken cancellationToken)
    {
        var result = await _serverService.DeactivateServerAsync(serverName, cancellationToken);
        if (!result)
            return NotFoundResponse("Server", serverName);

        return NoContent();
    }

    /// <summary>
    /// Sets server maintenance mode.
    /// </summary>
    /// <param name="serverName">Server name.</param>
    /// <param name="inMaintenance">Maintenance mode flag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("{serverName}/maintenance")]
    [Authorize(Policy = "ServerWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetMaintenanceMode(
        string serverName,
        [FromQuery] bool inMaintenance = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _serverService.SetMaintenanceModeAsync(serverName, cancellationToken);
        if (!result)
            return NotFoundResponse("Server", serverName);

        return NoContent();
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Gets server status summary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Status summary.</returns>
    [HttpGet("statistics/summary")]
    [Authorize(Policy = "ServerRead")]
    [ProducesResponseType(typeof(ServerStatusSummary), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServerStatusSummary>> GetStatusSummary(CancellationToken cancellationToken)
    {
        var summary = await _serverService.GetStatusSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Gets server lookup items for dropdowns.
    /// </summary>
    /// <param name="activeOnly">Include only active servers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Lookup items.</returns>
    [HttpGet("lookup/items")]
    [Authorize(Policy = "ServerRead")]
    [ProducesResponseType(typeof(IReadOnlyList<LookupItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LookupItem>>> GetLookupItems(
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var items = await _serverService.GetServerLookupAsync(activeOnly, cancellationToken);
        return Ok(items);
    }

    #endregion
}
