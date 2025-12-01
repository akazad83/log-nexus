using Microsoft.Extensions.Logging;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Repositories;
using FMSLogNexus.Core.Interfaces.Services;
using System.Text.Json;
using FmsLogLevel = FMSLogNexus.Core.Enums.LogLevel;
using FmsLogTrendPoint = FMSLogNexus.Core.DTOs.Responses.LogTrendPoint;

namespace FMSLogNexus.Infrastructure.Services;

/// <summary>
/// Service implementation for log operations.
/// </summary>
public class LogService : ServiceBase, ILogService
{
    private readonly IRealTimeService? _realTimeService;

    public LogService(
        IUnitOfWork unitOfWork,
        ILogger<LogService> logger,
        IRealTimeService? realTimeService = null)
        : base(unitOfWork, logger)
    {
        _realTimeService = realTimeService;
    }

    #region Ingestion

    /// <inheritdoc />
    public async Task<LogIngestionResult> CreateLogAsync(
        CreateLogRequest request,
        string? clientIp = null,
        CancellationToken cancellationToken = default)
    {
        var logEntry = MapToEntity(request, clientIp);

        await UnitOfWork.Logs.AddAsync(logEntry, cancellationToken);

        // Update job execution log counts if applicable
        if (logEntry.JobExecutionId.HasValue)
        {
            await UnitOfWork.JobExecutions.IncrementLogCountAsync(
                logEntry.JobExecutionId.Value,
                logEntry.Level,
                cancellationToken);
        }

        await SaveChangesAsync(cancellationToken);

        // Notify real-time clients
        if (_realTimeService != null)
        {
            await _realTimeService.BroadcastNewLogAsync(MapToResponse(logEntry), cancellationToken);
        }

        Logger.LogDebug("Created log entry {LogId} for {ServerName}", logEntry.Id, logEntry.ServerName);

        return new LogIngestionResult
        {
            Id = logEntry.Id,
            ReceivedAt = logEntry.ReceivedAt
        };
    }

    /// <inheritdoc />
    public async Task<BatchLogResult> CreateBatchAsync(
        BatchLogRequest request,
        string? clientIp = null,
        CancellationToken cancellationToken = default)
    {
        var result = new BatchLogResult
        {
            Accepted = 0,
            Rejected = 0,
            Errors = new Dictionary<int, string>()
        };

        var logEntries = new List<LogEntry>();
        var executionLogCounts = new Dictionary<Guid, Dictionary<FmsLogLevel, int>>();

        for (int i = 0; i < request.Logs.Count; i++)
        {
            var logRequest = request.Logs[i];
            try
            {
                var logEntry = MapToEntity(logRequest, clientIp);
                logEntries.Add(logEntry);

                // Track execution log counts
                if (logEntry.JobExecutionId.HasValue)
                {
                    var execId = logEntry.JobExecutionId.Value;
                    if (!executionLogCounts.ContainsKey(execId))
                        executionLogCounts[execId] = new Dictionary<FmsLogLevel, int>();

                    if (!executionLogCounts[execId].ContainsKey(logEntry.Level))
                        executionLogCounts[execId][logEntry.Level] = 0;

                    executionLogCounts[execId][logEntry.Level]++;
                }

                result.Accepted++;
            }
            catch (Exception ex)
            {
                result.Rejected++;
                result.Errors[i] = ex.Message;
            }
        }

        if (logEntries.Count > 0)
        {
            // Bulk insert logs
            await UnitOfWork.Logs.BulkInsertAsync(logEntries, cancellationToken);

            // Update execution log counts
            foreach (var (execId, counts) in executionLogCounts)
            {
                await UnitOfWork.JobExecutions.IncrementLogCountsAsync(execId, counts, cancellationToken);
            }

            await SaveChangesAsync(cancellationToken);
        }

        Logger.LogInformation(
            "Batch log ingestion: {Accepted} accepted, {Rejected} rejected out of {Total}",
            result.Accepted, result.Rejected, request.Logs.Count);

        return result;
    }

    /// <inheritdoc />
    public async Task<int> IngestStreamAsync(
        IAsyncEnumerable<CreateLogRequest> logs,
        string? clientIp = null,
        CancellationToken cancellationToken = default)
    {
        async IAsyncEnumerable<LogEntry> MapLogs()
        {
            await foreach (var request in logs.WithCancellation(cancellationToken))
            {
                yield return MapToEntity(request, clientIp);
            }
        }

        var count = await UnitOfWork.Logs.StreamInsertAsync(MapLogs(), 1000, cancellationToken);

        Logger.LogInformation("Stream log ingestion: {Count} logs ingested", count);
        return count;
    }

    #endregion

    #region Query

    /// <inheritdoc />
    public async Task<LogEntryResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var log = await UnitOfWork.Logs.GetByIdAsync(id, cancellationToken);
        return log == null ? null : MapToResponse(log);
    }

    /// <inheritdoc />
    public async Task<LogEntryDetailResponse?> GetDetailAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var log = await UnitOfWork.Logs.GetByIdAsync(id, cancellationToken);
        if (log == null)
            return null;

        var baseResponse = MapToResponse(log);
        var response = new LogEntryDetailResponse
        {
            Id = baseResponse.Id,
            Timestamp = baseResponse.Timestamp,
            Level = baseResponse.Level,
            Message = baseResponse.Message,
            MessageTemplate = baseResponse.MessageTemplate,
            JobId = baseResponse.JobId,
            JobDisplayName = baseResponse.JobDisplayName,
            JobExecutionId = baseResponse.JobExecutionId,
            ServerName = baseResponse.ServerName,
            Category = baseResponse.Category,
            SourceContext = baseResponse.SourceContext,
            CorrelationId = baseResponse.CorrelationId,
            TraceId = baseResponse.TraceId,
            SpanId = baseResponse.SpanId,
            HasException = baseResponse.HasException,
            Exception = baseResponse.Exception,
            Properties = baseResponse.Properties,
            Tags = baseResponse.Tags,
            Environment = baseResponse.Environment,
            ApplicationVersion = baseResponse.ApplicationVersion,
            ReceivedAt = log.ReceivedAt,
            ClientIp = log.ClientIp,
            RelatedLogs = new List<LogEntryResponse>()
        };

        // Get related logs by correlation ID
        if (!string.IsNullOrEmpty(log.CorrelationId))
        {
            var relatedLogs = await UnitOfWork.Logs.GetByCorrelationIdAsync(log.CorrelationId, cancellationToken);
            response.RelatedLogs = relatedLogs
                .Where(l => l.Id != id)
                .Take(50)
                .Select(MapToResponse)
                .ToList();
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<LogEntryResponse>> SearchAsync(
        LogSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await UnitOfWork.Logs.SearchAsync(request, cancellationToken);

        return PaginatedResult<LogEntryResponse>.Create(
            result.Items.Select(MapToResponse),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LogEntryResponse>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var logs = await UnitOfWork.Logs.GetByCorrelationIdAsync(correlationId, cancellationToken);
        return logs.Select(MapToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LogEntryResponse>> GetByTraceIdAsync(
        string traceId,
        CancellationToken cancellationToken = default)
    {
        var logs = await UnitOfWork.Logs.GetByTraceIdAsync(traceId, cancellationToken);
        return logs.Select(MapToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LogEntryResponse>> GetByJobExecutionAsync(
        Guid jobExecutionId,
        FmsLogLevel? minLevel = null,
        CancellationToken cancellationToken = default)
    {
        var logs = await UnitOfWork.Logs.GetByJobExecutionAsync(jobExecutionId, minLevel, cancellationToken);
        return logs.Select(MapToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecentErrorResponse>> GetRecentErrorsAsync(
        int count = 50,
        string? serverName = null,
        string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        var logs = await UnitOfWork.Logs.GetRecentErrorsAsync(count, serverName, jobId, cancellationToken);

        return logs.Select(log => new RecentErrorResponse
        {
            Id = log.Id,
            Timestamp = log.Timestamp,
            Level = log.Level,
            Message = log.Message,
            ServerName = log.ServerName,
            JobId = log.JobId,
            JobDisplayName = log.Job?.DisplayName,
            ExceptionType = log.ExceptionType
        }).ToList();
    }

    #endregion

    #region Statistics

    /// <inheritdoc />
    public async Task<LogStatisticsResponse> GetStatisticsAsync(
        LogStatisticsRequest request,
        CancellationToken cancellationToken = default)
    {
        var summary = await UnitOfWork.Logs.GetSummaryAsync(
            request.StartDate ?? DateTime.UtcNow.AddDays(-7),
            request.EndDate ?? DateTime.UtcNow,
            request.ServerName,
            request.JobId,
            cancellationToken);

        var topExceptions = await UnitOfWork.Logs.GetTopExceptionsAsync(
            request.StartDate ?? DateTime.UtcNow.AddDays(-7),
            request.EndDate ?? DateTime.UtcNow,
            10,
            cancellationToken);

        return new LogStatisticsResponse
        {
            Summary = summary,
            TopExceptions = topExceptions.ToList()
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FmsLogTrendPoint>> GetTrendAsync(
        DateTime startDate,
        DateTime endDate,
        TrendInterval interval,
        string? serverName = null,
        string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.Logs.GetTrendDataAsync(
            startDate, endDate, interval, serverName, jobId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Dictionary<FmsLogLevel, long>> GetCountsByLevelAsync(
        DateTime startDate,
        DateTime endDate,
        string? serverName = null,
        string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.Logs.GetCountsByLevelAsync(
            startDate, endDate, serverName, jobId, cancellationToken);
    }

    #endregion

    #region Lookups

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDistinctServersAsync(CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.Logs.GetDistinctServersAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDistinctJobsAsync(CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.Logs.GetDistinctJobsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDistinctCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.Logs.GetDistinctCategoriesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDistinctTagsAsync(CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.Logs.GetDistinctTagsAsync(cancellationToken);
    }

    #endregion

    #region Export

    /// <inheritdoc />
    public async IAsyncEnumerable<LogEntryResponse> ExportAsync(
        LogExportRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var log in UnitOfWork.Logs.ExportAsync(request, cancellationToken))
        {
            yield return MapToResponse(log);
        }
    }

    /// <inheritdoc />
    public async Task<int> ExportToStreamAsync(
        LogExportRequest request,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        var count = 0;
        await using var writer = new StreamWriter(outputStream);

        // Write CSV header
        await writer.WriteLineAsync("Id,Timestamp,Level,Message,ServerName,JobId,Category,CorrelationId,ExceptionType,ExceptionMessage");

        await foreach (var log in UnitOfWork.Logs.ExportAsync(request, cancellationToken))
        {
            var line = $"\"{log.Id}\",\"{log.Timestamp:O}\",\"{log.Level}\",\"{EscapeCsv(log.Message)}\",\"{log.ServerName}\",\"{log.JobId}\",\"{log.Category}\",\"{log.CorrelationId}\",\"{log.ExceptionType}\",\"{EscapeCsv(log.ExceptionMessage)}\"";
            await writer.WriteLineAsync(line);
            count++;
        }

        await writer.FlushAsync(cancellationToken);
        return count;
    }

    #endregion

    #region Private Methods

    private static LogEntry MapToEntity(CreateLogRequest request, string? clientIp)
    {
        return new LogEntry
        {
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            Level = request.Level,
            Message = request.Message,
            MessageTemplate = request.MessageTemplate,
            ServerName = request.ServerName,
            JobId = request.JobId,
            JobExecutionId = request.JobExecutionId,
            Category = request.Category,
            SourceContext = request.SourceContext,
            CorrelationId = request.CorrelationId,
            TraceId = request.TraceId,
            SpanId = request.SpanId,
            ParentSpanId = request.ParentSpanId,
            ExceptionType = request.Exception?.Type,
            ExceptionMessage = request.Exception?.Message,
            ExceptionStackTrace = request.Exception?.StackTrace,
            ExceptionSource = request.Exception?.Source,
            Properties = request.Properties != null ? JsonSerializer.Serialize(request.Properties) : null,
            Tags = request.Tags != null ? string.Join(",", request.Tags) : null,
            Environment = request.Environment,
            ApplicationVersion = request.ApplicationVersion,
            ClientIp = clientIp,
            ReceivedAt = DateTime.UtcNow
        };
    }

    private static LogEntryResponse MapToResponse(LogEntry log)
    {
        return new LogEntryResponse
        {
            Id = log.Id,
            Timestamp = log.Timestamp,
            Level = log.Level,
            Message = log.Message,
            MessageTemplate = log.MessageTemplate,
            ServerName = log.ServerName,
            JobId = log.JobId,
            JobDisplayName = log.Job?.DisplayName,
            JobExecutionId = log.JobExecutionId,
            Category = log.Category,
            SourceContext = log.SourceContext,
            CorrelationId = log.CorrelationId,
            TraceId = log.TraceId,
            SpanId = log.SpanId,
            HasException = log.HasException,
            Exception = log.HasException ? new ExceptionResponse
            {
                Type = log.ExceptionType,
                Message = log.ExceptionMessage,
                StackTrace = log.ExceptionStackTrace,
                Source = log.ExceptionSource
            } : null,
            Properties = !string.IsNullOrEmpty(log.Properties)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(log.Properties)
                : null,
            Tags = !string.IsNullOrEmpty(log.Tags)
                ? log.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                : null,
            Environment = log.Environment,
            ApplicationVersion = log.ApplicationVersion
        };
    }

    private static string? EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value.Replace("\"", "\"\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    #endregion
}
