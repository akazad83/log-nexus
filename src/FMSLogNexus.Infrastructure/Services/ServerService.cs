using Microsoft.Extensions.Logging;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Repositories;
using FMSLogNexus.Core.Interfaces.Services;
using System.Text.Json;

namespace FMSLogNexus.Infrastructure.Services;

/// <summary>
/// Service implementation for server operations.
/// </summary>
public class ServerService : ServiceBase, IServerService
{
    private readonly IRealTimeService? _realTimeService;

    public ServerService(
        IUnitOfWork unitOfWork,
        ILogger<ServerService> logger,
        IRealTimeService? realTimeService = null)
        : base(unitOfWork, logger)
    {
        _realTimeService = realTimeService;
    }

    #region Server Management

    /// <inheritdoc />
    public async Task<ServerStatusResponse> CreateServerAsync(
        CreateServerRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await UnitOfWork.Servers.GetByIdAsync(request.ServerName, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"Server '{request.ServerName}' already exists.");
        }

        var server = new Server
        {
            Id = request.ServerName,
            DisplayName = request.DisplayName ?? request.ServerName,
            Description = request.Description,
            IpAddress = request.IpAddress,
            Status = ServerStatus.Unknown,
            IsActive = true,
            HeartbeatIntervalSeconds = request.HeartbeatIntervalSeconds ?? 60,
            Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null
        };

        await UnitOfWork.Servers.AddAsync(server, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Created server {ServerName}", server.Id);

        return MapToStatusResponse(server);
    }

    /// <inheritdoc />
    public async Task<ServerStatusResponse?> GetServerAsync(
        string serverName,
        CancellationToken cancellationToken = default)
    {
        var server = await UnitOfWork.Servers.GetByIdAsync(serverName, cancellationToken);
        return server == null ? null : MapToStatusResponse(server);
    }

    /// <inheritdoc />
    public async Task<ServerStatusResponse?> UpdateServerAsync(
        string serverName,
        UpdateServerRequest request,
        CancellationToken cancellationToken = default)
    {
        var server = await UnitOfWork.Servers.GetByIdAsync(serverName, cancellationToken);
        if (server == null)
            return null;

        if (request.DisplayName != null) server.DisplayName = request.DisplayName;
        if (request.Description != null) server.Description = request.Description;
        if (request.IpAddress != null) server.IpAddress = request.IpAddress;
        if (request.HeartbeatIntervalSeconds.HasValue) server.HeartbeatIntervalSeconds = request.HeartbeatIntervalSeconds.Value;
        if (request.IsActive.HasValue) server.IsActive = request.IsActive.Value;
        if (request.Metadata != null) server.Metadata = JsonSerializer.Serialize(request.Metadata);

        server.UpdatedAt = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Updated server {ServerName}", serverName);

        return MapToStatusResponse(server);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteServerAsync(
        string serverName,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.Servers.DeactivateAsync(serverName, cancellationToken);
        if (result)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Deactivated server {ServerName}", serverName);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServerStatusResponse>> GetAllServersAsync(
        CancellationToken cancellationToken = default)
    {
        var servers = await UnitOfWork.Servers.GetAllAsync(cancellationToken);
        return servers.Select(MapToStatusResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServerStatusResponse>> GetServersByStatusAsync(
        ServerStatus status,
        CancellationToken cancellationToken = default)
    {
        var servers = await UnitOfWork.Servers.GetByStatusAsync(status, cancellationToken);
        return servers.Select(MapToStatusResponse).ToList();
    }

    #endregion

    #region Heartbeat

    /// <inheritdoc />
    public async Task<ServerStatusResponse> ProcessHeartbeatAsync(
        ServerHeartbeatRequest request,
        string? clientIp,
        CancellationToken cancellationToken = default)
    {
        var server = await UnitOfWork.Servers.UpdateHeartbeatAsync(
            request.ServerName,
            clientIp,
            request.AgentVersion,
            request.AgentType,
            request.Metadata != null ? new Dictionary<string, object>
            {
                ["OsVersion"] = request.Metadata.OsVersion ?? string.Empty,
                ["CpuUsage"] = request.Metadata.CpuUsage ?? 0,
                ["MemoryUsage"] = request.Metadata.MemoryUsage ?? 0,
                ["DiskUsage"] = request.Metadata.DiskUsage ?? 0
            } : null,
            cancellationToken);

        await SaveChangesAsync(cancellationToken);

        Logger.LogDebug("Processed heartbeat from {ServerName}", request.ServerName);

        if (_realTimeService != null)
        {
            await _realTimeService.BroadcastServerHeartbeatAsync(
                request.ServerName, cancellationToken);
        }

        return MapToStatusResponse(server);
    }

    /// <inheritdoc />
    public async Task<int> CheckAndMarkOfflineServersAsync(
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        var count = await UnitOfWork.Servers.MarkOfflineAsync(timeoutSeconds, cancellationToken);
        if (count > 0)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogWarning("Marked {Count} servers as offline", count);

            if (_realTimeService != null)
            {
                var offlineServers = await UnitOfWork.Servers.GetByStatusAsync(ServerStatus.Offline, cancellationToken);
                foreach (var server in offlineServers)
                {
                    await _realTimeService.BroadcastServerStatusChangeAsync(
                        server.Id, ServerStatus.Online, ServerStatus.Offline, cancellationToken);
                }
            }
        }
        return count;
    }

    #endregion

    #region Status and Health

    /// <inheritdoc />
    public async Task<DashboardServerStats> GetStatusSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var summary = await UnitOfWork.Servers.GetStatusSummaryAsync(cancellationToken);
        return new DashboardServerStats
        {
            Total = summary.Total,
            Online = summary.Online,
            Offline = summary.Offline,
            Degraded = summary.Degraded,
            Unknown = summary.Unknown
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServerStatusResponse>> GetUnresponsiveServersAsync(
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        var servers = await UnitOfWork.Servers.GetUnresponsiveAsync(timeoutSeconds, cancellationToken);
        return servers.Select(MapToStatusResponse).ToList();
    }

    #endregion

    #region Server Actions

    /// <inheritdoc />
    public async Task<bool> ActivateServerAsync(
        string serverName,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.Servers.ActivateAsync(serverName, cancellationToken);
        if (result)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Activated server {ServerName}", serverName);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateServerAsync(
        string serverName,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.Servers.DeactivateAsync(serverName, cancellationToken);
        if (result)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Deactivated server {ServerName}", serverName);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> SetMaintenanceModeAsync(
        string serverName,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.Servers.SetMaintenanceModeAsync(serverName, cancellationToken);
        if (result)
        {
            await SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Set maintenance mode ON for server {ServerName}", serverName);

            if (_realTimeService != null)
            {
                // TODO: Need to track old status before setting maintenance mode
                // For now, comment out - requires refactoring to get old status first
                // await _realTimeService.BroadcastServerStatusChangeAsync(serverName, oldStatus, ServerStatus.Maintenance, cancellationToken);
            }
        }
        return result;
    }

    #endregion

    #region Lookups

    /// <inheritdoc />
    public async Task<IReadOnlyList<LookupItem>> GetServerLookupAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.Servers.GetLookupItemsAsync(activeOnly, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ServerExistsAsync(
        string serverName,
        CancellationToken cancellationToken = default)
    {
        var server = await UnitOfWork.Servers.GetByIdAsync(serverName, cancellationToken);
        return server != null;
    }

    #endregion

    #region Private Methods

    private static ServerStatusResponse MapToStatusResponse(Server server)
    {
        return new ServerStatusResponse
        {
            ServerName = server.Id,
            DisplayName = server.DisplayName,
            IpAddress = server.IpAddress,
            Status = server.Status,
            LastHeartbeat = server.LastHeartbeat,
            SecondsSinceHeartbeat = server.LastHeartbeat.HasValue
                ? (int?)(DateTime.UtcNow - server.LastHeartbeat.Value).TotalSeconds
                : null,
            AgentVersion = server.AgentVersion,
            ActiveJobCount = 0, // Would need to query jobs
            RunningJobCount = 0, // Would need to query executions
            Metrics = !string.IsNullOrEmpty(server.Metadata)
                ? ParseMetrics(server.Metadata)
                : null
        };
    }

    private static ServerMetrics? ParseMetrics(string metadata)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(metadata);
            if (dict == null) return null;

            return new ServerMetrics
            {
                CpuUsage = dict.TryGetValue("CpuUsage", out var cpu) && cpu is JsonElement cpuEl
                    ? cpuEl.GetDecimal()
                    : null,
                MemoryUsage = dict.TryGetValue("MemoryUsage", out var mem) && mem is JsonElement memEl
                    ? memEl.GetDecimal()
                    : null,
                DiskUsage = dict.TryGetValue("DiskUsage", out var disk) && disk is JsonElement diskEl
                    ? diskEl.GetDecimal()
                    : null
            };
        }
        catch
        {
            return null;
        }
    }

    #endregion
}
