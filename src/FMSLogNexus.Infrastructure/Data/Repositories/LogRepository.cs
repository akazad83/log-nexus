using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Repositories;
using CoreLogTrendPoint = FMSLogNexus.Core.DTOs.Responses.LogTrendPoint;
using CoreExceptionSummary = FMSLogNexus.Core.DTOs.Responses.ExceptionSummary;
using CoreLogSummary = FMSLogNexus.Core.DTOs.Responses.LogSummary;
using CoreJobLogSummary = FMSLogNexus.Core.DTOs.Responses.JobLogSummary;

namespace FMSLogNexus.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for log entry operations.
/// Optimized for high-volume log ingestion and querying.
/// </summary>
public class LogRepository : RepositoryBase<LogEntry>, ILogRepository
{
    public LogRepository(FMSLogNexusDbContext context) : base(context)
    {
    }

    #region High-Performance Ingestion

    /// <inheritdoc />
    public async Task<int> BulkInsertAsync(IEnumerable<LogEntry> logs, CancellationToken cancellationToken = default)
    {
        var logList = logs.ToList();
        if (logList.Count == 0)
            return 0;

        // For smaller batches, use standard EF Core
        if (logList.Count <= 100)
        {
            await DbSet.AddRangeAsync(logList, cancellationToken);
            return logList.Count;
        }

        // For larger batches, use bulk operations
        // Note: In production, use a library like EFCore.BulkExtensions
        const int batchSize = 1000;
        var totalInserted = 0;

        foreach (var batch in logList.Chunk(batchSize))
        {
            await DbSet.AddRangeAsync(batch, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);
            totalInserted += batch.Length;
            Context.ChangeTracker.Clear();
        }

        return totalInserted;
    }

    /// <inheritdoc />
    public async Task<int> StreamInsertAsync(
        IAsyncEnumerable<LogEntry> logs,
        int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        var batch = new List<LogEntry>(batchSize);
        var totalInserted = 0;

        await foreach (var log in logs.WithCancellation(cancellationToken))
        {
            batch.Add(log);

            if (batch.Count >= batchSize)
            {
                await DbSet.AddRangeAsync(batch, cancellationToken);
                await Context.SaveChangesAsync(cancellationToken);
                totalInserted += batch.Count;
                batch.Clear();
                Context.ChangeTracker.Clear();
            }
        }

        // Insert remaining
        if (batch.Count > 0)
        {
            await DbSet.AddRangeAsync(batch, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);
            totalInserted += batch.Count;
        }

        return totalInserted;
    }

    #endregion

    #region Search Operations

    /// <inheritdoc />
    public async Task<PaginatedResult<LogEntry>> SearchAsync(
        LogSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        // Apply time range filter
        if (request.StartDate.HasValue)
            query = query.Where(l => l.Timestamp >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(l => l.Timestamp <= request.EndDate.Value);

        // Apply level filters
        if (request.MinLevel.HasValue)
            query = query.Where(l => l.Level >= request.MinLevel.Value);

        if (request.MaxLevel.HasValue)
            query = query.Where(l => l.Level <= request.MaxLevel.Value);

        // Apply entity filters
        if (!string.IsNullOrWhiteSpace(request.ServerName))
            query = query.Where(l => l.ServerName == request.ServerName);

        if (!string.IsNullOrWhiteSpace(request.JobId))
            query = query.Where(l => l.JobId == request.JobId);

        if (request.JobExecutionId.HasValue)
            query = query.Where(l => l.JobExecutionId == request.JobExecutionId.Value);

        // Apply correlation filter
        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
            query = query.Where(l => l.CorrelationId == request.CorrelationId);

        // Apply text search
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchPattern = $"%{request.Search}%";
            query = query.Where(l =>
                EF.Functions.Like(l.Message, searchPattern) ||
                (l.ExceptionMessage != null && EF.Functions.Like(l.ExceptionMessage, searchPattern)));
        }

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(l => l.Category == request.Category);

        // Apply exception filter
        if (request.HasException.HasValue)
        {
            if (request.HasException.Value)
                query = query.Where(l => l.ExceptionType != null);
            else
                query = query.Where(l => l.ExceptionType == null);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = request.SortOrder == FMSLogNexus.Core.Enums.SortOrder.Descending
            ? query.OrderByDescending(l => l.Timestamp)
            : query.OrderBy(l => l.Timestamp);

        // Apply pagination
        var page = request.Page;
        var pageSize = request.PageSize;
        
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PaginatedResult<LogEntry>.Create(items, totalCount, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<LogEntry>> FullTextSearchAsync(
        string searchText,
        DateTime? startDate,
        DateTime? endDate,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        if (startDate.HasValue)
            query = query.Where(l => l.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(l => l.Timestamp <= endDate.Value);

        // Use LIKE for full-text search (in production, use SQL Server Full-Text Search)
        var searchPattern = $"%{searchText}%";
        query = query.Where(l => 
            EF.Functions.Like(l.Message, searchPattern) ||
            (l.MessageTemplate != null && EF.Functions.Like(l.MessageTemplate, searchPattern)) ||
            (l.ExceptionMessage != null && EF.Functions.Like(l.ExceptionMessage, searchPattern)));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PaginatedResult<LogEntry>.Create(items, totalCount, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LogEntry>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(l => l.CorrelationId == correlationId)
            .OrderBy(l => l.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LogEntry>> GetByTraceIdAsync(
        string traceId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(l => l.TraceId == traceId)
            .OrderBy(l => l.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LogEntry>> GetByJobExecutionAsync(
        Guid jobExecutionId,
        LogLevel? minLevel = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Where(l => l.JobExecutionId == jobExecutionId);

        if (minLevel.HasValue)
            query = query.Where(l => l.Level >= minLevel.Value);

        return await query
            .OrderBy(l => l.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<LogEntry>> GetByJobAsync(
        string jobId,
        DateTime startDate,
        DateTime endDate,
        LogLevel? minLevel = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Where(l => l.JobId == jobId)
            .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate);

        if (minLevel.HasValue)
            query = query.Where(l => l.Level >= minLevel.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PaginatedResult<LogEntry>.Create(items, totalCount, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LogEntry>> GetRecentErrorsAsync(
        int count = 50,
        string? serverName = null,
        string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Where(l => l.Level >= LogLevel.Error);

        if (!string.IsNullOrWhiteSpace(serverName))
            query = query.Where(l => l.ServerName == serverName);

        if (!string.IsNullOrWhiteSpace(jobId))
            query = query.Where(l => l.JobId == jobId);

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Statistics and Aggregations

    /// <inheritdoc />
    public async Task<Dictionary<LogLevel, long>> GetCountsByLevelAsync(
        DateTime startDate,
        DateTime endDate,
        string? serverName = null,
        string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate);

        if (!string.IsNullOrWhiteSpace(serverName))
            query = query.Where(l => l.ServerName == serverName);

        if (!string.IsNullOrWhiteSpace(jobId))
            query = query.Where(l => l.JobId == jobId);

        var counts = await query
            .GroupBy(l => l.Level)
            .Select(g => new { Level = g.Key, Count = (long)g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.Level, x => x.Count);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CoreLogTrendPoint>> GetTrendDataAsync(
        DateTime startDate,
        DateTime endDate,
        TrendInterval interval,
        string? serverName = null,
        string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate);

        if (!string.IsNullOrWhiteSpace(serverName))
            query = query.Where(l => l.ServerName == serverName);

        if (!string.IsNullOrWhiteSpace(jobId))
            query = query.Where(l => l.JobId == jobId);

        // Group by time interval
        var groupedQuery = interval switch
        {
            TrendInterval.Minute => query.GroupBy(l => new { l.Timestamp.Year, l.Timestamp.Month, l.Timestamp.Day, l.Timestamp.Hour, l.Timestamp.Minute }),
            TrendInterval.Hour => query.GroupBy(l => new { l.Timestamp.Year, l.Timestamp.Month, l.Timestamp.Day, l.Timestamp.Hour, Minute = 0 }),
            TrendInterval.Day => query.GroupBy(l => new { l.Timestamp.Year, l.Timestamp.Month, l.Timestamp.Day, Hour = 0, Minute = 0 }),
            TrendInterval.Week => query.GroupBy(l => new { l.Timestamp.Year, l.Timestamp.Month, Day = l.Timestamp.Day / 7 * 7, Hour = 0, Minute = 0 }),
            TrendInterval.Month => query.GroupBy(l => new { l.Timestamp.Year, l.Timestamp.Month, Day = 1, Hour = 0, Minute = 0 }),
            _ => query.GroupBy(l => new { l.Timestamp.Year, l.Timestamp.Month, l.Timestamp.Day, l.Timestamp.Hour, Minute = 0 })
        };

        var results = await groupedQuery
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                g.Key.Hour,
                g.Key.Minute,
                Total = g.Count(),
                Errors = g.Count(l => l.Level >= LogLevel.Error),
                Warnings = g.Count(l => l.Level == LogLevel.Warning),
                Info = g.Count(l => l.Level == LogLevel.Information)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ThenBy(x => x.Day)
            .ThenBy(x => x.Hour)
            .ThenBy(x => x.Minute)
            .ToListAsync(cancellationToken);

        return results.Select(r => new CoreLogTrendPoint
        {
            Timestamp = new DateTime(r.Year, r.Month, r.Day, r.Hour, r.Minute, 0, DateTimeKind.Utc),
            TotalCount = r.Total,
            ErrorCount = r.Errors,
            WarningCount = r.Warnings,
            InfoCount = r.Info
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CoreExceptionSummary>> GetTopExceptionsAsync(
        DateTime startDate,
        DateTime endDate,
        int top = 10,
        CancellationToken cancellationToken = default)
    {
        var results = await DbSet.AsNoTracking()
            .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
            .Where(l => l.ExceptionType != null)
            .GroupBy(l => l.ExceptionType!)
            .Select(g => new
            {
                ExceptionType = g.Key,
                Count = g.Count(),
                LastOccurrence = g.Max(l => l.Timestamp),
                FirstOccurrence = g.Min(l => l.Timestamp),
                AffectedJobs = g.Select(l => l.JobId).Distinct().Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(top)
            .ToListAsync(cancellationToken);

        return results.Select(r => new CoreExceptionSummary
        {
            ExceptionType = r.ExceptionType,
            Count = r.Count,
            LastOccurrence = r.LastOccurrence,
            FirstOccurrence = r.FirstOccurrence,
            AffectedJobs = r.AffectedJobs
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<CoreLogSummary> GetSummaryAsync(
        DateTime startDate,
        DateTime endDate,
        string? serverName = null,
        string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate);

        if (!string.IsNullOrWhiteSpace(serverName))
            query = query.Where(l => l.ServerName == serverName);

        if (!string.IsNullOrWhiteSpace(jobId))
            query = query.Where(l => l.JobId == jobId);

        var summary = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalLogs = (long)g.Count(),
                TraceCount = (long)g.Count(l => l.Level == LogLevel.Trace),
                DebugCount = (long)g.Count(l => l.Level == LogLevel.Debug),
                InfoCount = (long)g.Count(l => l.Level == LogLevel.Information),
                WarningCount = (long)g.Count(l => l.Level == LogLevel.Warning),
                ErrorCount = (long)g.Count(l => l.Level == LogLevel.Error),
                CriticalCount = (long)g.Count(l => l.Level == LogLevel.Critical),
                ExceptionCount = (long)g.Count(l => l.ExceptionType != null),
                UniqueJobs = g.Select(l => l.JobId).Distinct().Count(),
                UniqueExecutions = g.Select(l => l.JobExecutionId).Distinct().Count(),
                FirstLog = g.Min(l => l.Timestamp),
                LastLog = g.Max(l => l.Timestamp)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (summary == null)
        {
            return new CoreLogSummary
            {
                FirstLog = null,
                LastLog = null
            };
        }

        return new CoreLogSummary
        {
            TotalLogs = summary.TotalLogs,
            TraceCount = summary.TraceCount,
            DebugCount = summary.DebugCount,
            InfoCount = summary.InfoCount,
            WarningCount = summary.WarningCount,
            ErrorCount = summary.ErrorCount,
            CriticalCount = summary.CriticalCount,
            ExceptionCount = summary.ExceptionCount,
            UniqueJobs = summary.UniqueJobs,
            UniqueExecutions = summary.UniqueExecutions,
            FirstLog = summary.FirstLog,
            LastLog = summary.LastLog
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CoreJobLogSummary>> GetLogsByJobAsync(
        DateTime startDate,
        DateTime endDate,
        int top = 20,
        CancellationToken cancellationToken = default)
    {
        var results = await DbSet.AsNoTracking()
            .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
            .Where(l => l.JobId != null)
            .GroupBy(l => l.JobId)
            .Select(g => new
            {
                JobId = g.Key!,
                TotalLogs = (long)g.Count(),
                ErrorCount = (long)g.Count(l => l.Level >= LogLevel.Error),
                WarningCount = (long)g.Count(l => l.Level == LogLevel.Warning)
            })
            .OrderByDescending(x => x.TotalLogs)
            .Take(top)
            .ToListAsync(cancellationToken);

        return results.Select(r => new CoreJobLogSummary
        {
            JobId = r.JobId,
            JobDisplayName = r.JobId, // Use JobId as display name since LogEntry doesn't have DisplayName
            TotalLogs = r.TotalLogs,
            ErrorCount = r.ErrorCount,
            WarningCount = r.WarningCount
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<int> CountErrorsInWindowAsync(
        int windowMinutes,
        LogLevel minLevel = LogLevel.Error,
        string? serverName = null,
        string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        var windowStart = DateTime.UtcNow.AddMinutes(-windowMinutes);

        var query = DbSet.AsNoTracking()
            .Where(l => l.Timestamp >= windowStart)
            .Where(l => l.Level >= minLevel);

        if (!string.IsNullOrWhiteSpace(serverName))
            query = query.Where(l => l.ServerName == serverName);

        if (!string.IsNullOrWhiteSpace(jobId))
            query = query.Where(l => l.JobId == jobId);

        return await query.CountAsync(cancellationToken);
    }

    #endregion

    #region Lookup Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDistinctServersAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Select(l => l.ServerName)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDistinctJobsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(l => l.JobId != null)
            .Select(l => l.JobId!)
            .Distinct()
            .OrderBy(j => j)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDistinctCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(l => l.Category != null)
            .Select(l => l.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDistinctTagsAsync(CancellationToken cancellationToken = default)
    {
        // Get all tags (comma-separated) and split them
        var allTags = await DbSet.AsNoTracking()
            .Where(l => l.Tags != null)
            .Select(l => l.Tags!)
            .ToListAsync(cancellationToken);

        return allTags
            .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct()
            .OrderBy(t => t)
            .ToList();
    }

    #endregion

    #region Maintenance Operations

    /// <inheritdoc />
    public async Task<long> DeleteOldLogsAsync(
        DateTime olderThan,
        int batchSize = 10000,
        CancellationToken cancellationToken = default)
    {
        long totalDeleted = 0;
        int deletedInBatch;

        do
        {
            // Delete in batches to avoid lock escalation
            var sql = @"
                DELETE TOP (@BatchSize) FROM [log].[LogEntries]
                WHERE [Timestamp] < @OlderThan";

            deletedInBatch = await Context.Database.ExecuteSqlRawAsync(
                sql,
                new SqlParameter("@BatchSize", batchSize),
                new SqlParameter("@OlderThan", olderThan));

            totalDeleted += deletedInBatch;

            // Small delay between batches to reduce lock contention
            if (deletedInBatch > 0 && deletedInBatch >= batchSize)
            {
                await Task.Delay(100, cancellationToken);
            }

        } while (deletedInBatch > 0 && deletedInBatch >= batchSize && !cancellationToken.IsCancellationRequested);

        return totalDeleted;
    }

    /// <inheritdoc />
    public async Task<long> ArchiveLogsAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        // Archive to a separate table (assumes [log].[LogEntriesArchive] exists with same schema)
        var sql = @"
            INSERT INTO [log].[LogEntriesArchive]
            SELECT * FROM [log].[LogEntries]
            WHERE [Timestamp] < @OlderThan;
            
            DELETE FROM [log].[LogEntries]
            WHERE [Timestamp] < @OlderThan;
            
            SELECT @@ROWCOUNT;";

        var result = await Context.Database.ExecuteSqlRawAsync(
            sql,
            new SqlParameter("@OlderThan", olderThan));

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PartitionInfo>> GetPartitionInfoAsync(CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT 
                p.partition_number AS PartitionNumber,
                MIN(prv.value) AS MinTimestamp,
                MAX(prv.value) AS MaxTimestamp,
                SUM(p.rows) AS RowCount,
                SUM(a.total_pages * 8 * 1024) AS SizeBytes,
                fg.name AS FileGroup
            FROM sys.partitions p
            INNER JOIN sys.tables t ON p.object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            LEFT JOIN sys.indexes i ON p.object_id = i.object_id AND p.index_id = i.index_id
            LEFT JOIN sys.partition_schemes ps ON i.data_space_id = ps.data_space_id
            LEFT JOIN sys.partition_functions pf ON ps.function_id = pf.function_id
            LEFT JOIN sys.partition_range_values prv ON pf.function_id = prv.function_id AND p.partition_number = prv.boundary_id + 1
            LEFT JOIN sys.allocation_units a ON p.partition_id = a.container_id
            LEFT JOIN sys.filegroups fg ON a.data_space_id = fg.data_space_id
            WHERE s.name = 'log' AND t.name = 'LogEntries'
            GROUP BY p.partition_number, fg.name
            ORDER BY p.partition_number";

        var results = new List<PartitionInfo>();

        try
        {
            await using var command = Context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            await Context.Database.OpenConnectionAsync(cancellationToken);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new PartitionInfo
                {
                    PartitionNumber = reader.GetInt32(0),
                    MinTimestamp = reader.IsDBNull(1) ? null : reader.GetDateTime(1),
                    MaxTimestamp = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                    RowCount = reader.GetInt64(3),
                    SizeBytes = reader.GetInt64(4),
                    FileGroup = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                });
            }
        }
        catch
        {
            // Return empty list if partitioning is not configured
        }

        return results;
    }

    #endregion

    #region Export

    /// <inheritdoc />
    public async IAsyncEnumerable<LogEntry> ExportAsync(
        LogExportRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Where(l => l.Timestamp >= request.StartDate && l.Timestamp <= request.EndDate);

        if (request.MinLevel.HasValue)
            query = query.Where(l => l.Level >= request.MinLevel.Value);

        if (!string.IsNullOrWhiteSpace(request.ServerName))
            query = query.Where(l => l.ServerName == request.ServerName);

        if (!string.IsNullOrWhiteSpace(request.JobId))
            query = query.Where(l => l.JobId == request.JobId);

        query = query.OrderBy(l => l.Timestamp);

        await foreach (var log in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return log;
        }
    }

    #endregion
}
