using Microsoft.EntityFrameworkCore;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Repositories;
using System.Text.Json;
using RepoServerStatusSummary = FMSLogNexus.Core.Interfaces.Repositories.ServerStatusSummary;

namespace FMSLogNexus.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for server operations.
/// </summary>
public class ServerRepository : RepositoryBase<Server, string>, IServerRepository
{
    public ServerRepository(FMSLogNexusDbContext context) : base(context)
    {
    }

    #region Query Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<Server>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Server>> GetByStatusAsync(
        ServerStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(s => s.Status == status && s.IsActive)
            .OrderBy(s => s.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Server?> GetWithJobsAsync(
        string serverName,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(s => s.Jobs.Where(j => j.IsActive))
            .FirstOrDefaultAsync(s => s.Id == serverName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Server?> GetWithRunningExecutionsAsync(
        string serverName,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(s => s.Executions.Where(e => e.Status == JobStatus.Running || e.Status == JobStatus.Pending))
            .FirstOrDefaultAsync(s => s.Id == serverName, cancellationToken);
    }

    #endregion

    #region Heartbeat Operations

    /// <inheritdoc />
    public async Task<Server> UpdateHeartbeatAsync(
        string serverName,
        string? ipAddress,
        string? agentVersion,
        string? agentType,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var server = await DbSet.FirstOrDefaultAsync(s => s.Id == serverName, cancellationToken);

        if (server == null)
        {
            // Create new server
            server = new Server
            {
                Id = serverName,
                DisplayName = serverName,
                Status = ServerStatus.Online,
                IsActive = true,
                LastHeartbeat = DateTime.UtcNow,
                IpAddress = ipAddress,
                AgentVersion = agentVersion,
                AgentType = agentType,
                Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
                HeartbeatIntervalSeconds = 60
            };
            await DbSet.AddAsync(server, cancellationToken);
        }
        else
        {
            // Update existing server
            server.Status = ServerStatus.Online;
            server.LastHeartbeat = DateTime.UtcNow;
            server.IpAddress = ipAddress ?? server.IpAddress;
            server.AgentVersion = agentVersion ?? server.AgentVersion;
            server.AgentType = agentType ?? server.AgentType;
            if (metadata != null)
            {
                server.Metadata = JsonSerializer.Serialize(metadata);
            }
            server.UpdatedAt = DateTime.UtcNow;
        }

        return server;
    }

    /// <inheritdoc />
    public async Task<int> MarkOfflineAsync(
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        var threshold = DateTime.UtcNow.AddSeconds(-timeoutSeconds);

        var offlineServers = await DbSet
            .Where(s => s.IsActive)
            .Where(s => s.Status == ServerStatus.Online)
            .Where(s => s.LastHeartbeat.HasValue && s.LastHeartbeat < threshold)
            .ToListAsync(cancellationToken);

        foreach (var server in offlineServers)
        {
            server.Status = ServerStatus.Offline;
            server.UpdatedAt = DateTime.UtcNow;
        }

        return offlineServers.Count;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Server>> GetUnresponsiveAsync(
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        var threshold = DateTime.UtcNow.AddSeconds(-timeoutSeconds);

        return await DbSet.AsNoTracking()
            .Where(s => s.IsActive)
            .Where(s => !s.LastHeartbeat.HasValue || s.LastHeartbeat < threshold)
            .OrderBy(s => s.LastHeartbeat)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Server>> GetRecentlyActiveAsync(
        int withinSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        var threshold = DateTime.UtcNow.AddSeconds(-withinSeconds);

        return await DbSet.AsNoTracking()
            .Where(s => s.IsActive)
            .Where(s => s.LastHeartbeat.HasValue && s.LastHeartbeat >= threshold)
            .OrderByDescending(s => s.LastHeartbeat)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Status Operations

    /// <inheritdoc />
    public async Task<RepoServerStatusSummary> GetStatusSummaryAsync(CancellationToken cancellationToken = default)
    {
        var servers = await DbSet.AsNoTracking()
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);

        return new RepoServerStatusSummary
        {
            Total = servers.Count,
            Online = servers.Count(s => s.Status == ServerStatus.Online),
            Offline = servers.Count(s => s.Status == ServerStatus.Offline),
            Degraded = servers.Count(s => s.Status == ServerStatus.Degraded),
            Maintenance = servers.Count(s => s.Status == ServerStatus.Maintenance),
            Unknown = servers.Count(s => s.Status == ServerStatus.Unknown)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServerStatusInfo>> GetAllStatusesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await DbSet.AsNoTracking()
            .Where(s => s.IsActive)
            .Include(s => s.Jobs.Where(j => j.IsActive))
            .Include(s => s.Executions.Where(e => e.Status == JobStatus.Running))
            .Select(s => new ServerStatusInfo
            {
                ServerName = s.Id,
                DisplayName = s.DisplayName,
                Status = s.Status,
                LastHeartbeat = s.LastHeartbeat,
                SecondsSinceHeartbeat = s.LastHeartbeat.HasValue
                    ? (int)(now - s.LastHeartbeat.Value).TotalSeconds
                    : null,
                AgentVersion = s.AgentVersion,
                ActiveJobCount = s.Jobs.Count(j => j.IsActive),
                RunningJobCount = s.Executions.Count(e => e.Status == JobStatus.Running)
            })
            .OrderBy(s => s.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ServerStatus> CalculateStatusAsync(
        string serverName,
        CancellationToken cancellationToken = default)
    {
        var server = await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serverName, cancellationToken);

        if (server == null)
            return ServerStatus.Unknown;

        if (!server.IsActive)
            return ServerStatus.Offline;

        if (server.Status == ServerStatus.Maintenance)
            return ServerStatus.Maintenance;

        if (!server.LastHeartbeat.HasValue)
            return ServerStatus.Unknown;

        var secondsSinceHeartbeat = (DateTime.UtcNow - server.LastHeartbeat.Value).TotalSeconds;
        var timeoutThreshold = server.HeartbeatIntervalSeconds * 2;

        if (secondsSinceHeartbeat > timeoutThreshold)
            return ServerStatus.Offline;

        if (secondsSinceHeartbeat > server.HeartbeatIntervalSeconds * 1.5)
            return ServerStatus.Degraded;

        return ServerStatus.Online;
    }

    #endregion

    #region Lookup Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<LookupItem>> GetLookupItemsAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        if (activeOnly)
            query = query.Where(s => s.IsActive);

        return await query
            .OrderBy(s => s.DisplayName)
            .Select(s => new LookupItem
            {
                Value = s.Id,
                Text = s.DisplayName
            })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string serverName, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(s => s.Id == serverName, cancellationToken);
    }

    #endregion

    #region Registration

    /// <inheritdoc />
    public async Task<bool> ActivateAsync(string serverName, CancellationToken cancellationToken = default)
    {
        var server = await DbSet.FirstOrDefaultAsync(s => s.Id == serverName, cancellationToken);
        if (server == null)
            return false;

        server.IsActive = true;
        server.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateAsync(string serverName, CancellationToken cancellationToken = default)
    {
        var server = await DbSet.FirstOrDefaultAsync(s => s.Id == serverName, cancellationToken);
        if (server == null)
            return false;

        server.IsActive = false;
        server.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> SetMaintenanceModeAsync(string serverName, CancellationToken cancellationToken = default)
    {
        var server = await DbSet.FirstOrDefaultAsync(s => s.Id == serverName, cancellationToken);
        if (server == null)
            return false;

        server.Status = ServerStatus.Maintenance;
        server.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RegisterOrUpdateAsync(
        Server server,
        CancellationToken cancellationToken = default)
    {
        var existing = await DbSet.FirstOrDefaultAsync(s => s.Id == server.Id, cancellationToken);

        if (existing == null)
        {
            await DbSet.AddAsync(server, cancellationToken);
            return true;
        }

        existing.DisplayName = server.DisplayName;
        existing.Description = server.Description;
        existing.IpAddress = server.IpAddress;
        existing.AgentVersion = server.AgentVersion;
        existing.AgentType = server.AgentType;
        existing.HeartbeatIntervalSeconds = server.HeartbeatIntervalSeconds;
        existing.Metadata = server.Metadata;
        existing.UpdatedAt = DateTime.UtcNow;

        return false;
    }

    #endregion
}
