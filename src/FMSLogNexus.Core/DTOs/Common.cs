using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Core.DTOs;

/// <summary>
/// Base class for paginated requests.
/// </summary>
public class PaginationRequest
{
    private int _page = 1;
    private int _pageSize = 50;

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 1 : (value > 1000 ? 1000 : value);
    }

    /// <summary>
    /// Field to sort by.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort order (asc/desc).
    /// </summary>
    public SortOrder SortOrder { get; set; } = SortOrder.Descending;

    /// <summary>
    /// Calculated skip count for database queries.
    /// </summary>
    public int Skip => (Page - 1) * PageSize;
}

/// <summary>
/// Wrapper for paginated response data.
/// </summary>
/// <typeparam name="T">Type of items in the result.</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// Items for the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Creates an empty result.
    /// </summary>
    public static PaginatedResult<T> Empty(int page = 1, int pageSize = 50) => new()
    {
        Items = Array.Empty<T>(),
        Page = page,
        PageSize = pageSize,
        TotalCount = 0
    };

    /// <summary>
    /// Creates a result from items and total count.
    /// </summary>
    public static PaginatedResult<T> Create(IEnumerable<T> items, int totalCount, int page, int pageSize) => new()
    {
        Items = items.ToList(),
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount
    };
}

/// <summary>
/// Base class for date range filters.
/// </summary>
public class DateRangeFilter
{
    /// <summary>
    /// Start date (inclusive).
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date (inclusive).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Predefined time period.
    /// </summary>
    public TimePeriod? Period { get; set; }

    /// <summary>
    /// Gets the effective start date based on period or explicit date.
    /// </summary>
    public DateTime GetEffectiveStartDate()
    {
        if (StartDate.HasValue)
            return StartDate.Value;

        return Period switch
        {
            TimePeriod.LastHour => DateTime.UtcNow.AddHours(-1),
            TimePeriod.Last6Hours => DateTime.UtcNow.AddHours(-6),
            TimePeriod.Last24Hours => DateTime.UtcNow.AddHours(-24),
            TimePeriod.Last7Days => DateTime.UtcNow.AddDays(-7),
            TimePeriod.Last30Days => DateTime.UtcNow.AddDays(-30),
            _ => DateTime.UtcNow.AddHours(-24)
        };
    }

    /// <summary>
    /// Gets the effective end date.
    /// </summary>
    public DateTime GetEffectiveEndDate()
    {
        return EndDate ?? DateTime.UtcNow;
    }
}

/// <summary>
/// Generic API response wrapper.
/// </summary>
/// <typeparam name="T">Type of the data payload.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Whether the request was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response data (null on error).
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Error information (null on success).
    /// </summary>
    public ApiError? Error { get; set; }

    /// <summary>
    /// Creates a successful response.
    /// </summary>
    public static ApiResponse<T> Ok(T data) => new()
    {
        Success = true,
        Data = data
    };

    /// <summary>
    /// Creates an error response.
    /// </summary>
    public static ApiResponse<T> Fail(string code, string message, IEnumerable<FieldError>? details = null) => new()
    {
        Success = false,
        Error = new ApiError
        {
            Code = code,
            Message = message,
            Details = details?.ToList()
        }
    };
}

/// <summary>
/// API error information.
/// </summary>
public class ApiError
{
    /// <summary>
    /// Error code for programmatic handling.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Field-level validation errors.
    /// </summary>
    public List<FieldError>? Details { get; set; }

    /// <summary>
    /// Trace ID for debugging.
    /// </summary>
    public string? TraceId { get; set; }
}

/// <summary>
/// Field-level validation error.
/// </summary>
public class FieldError
{
    /// <summary>
    /// Field name that has the error.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Error message for the field.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Result of a batch operation.
/// </summary>
public class BatchResult
{
    /// <summary>
    /// Total items in the batch.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Number of successfully processed items.
    /// </summary>
    public int Accepted { get; set; }

    /// <summary>
    /// Number of rejected items.
    /// </summary>
    public int Rejected { get; set; }

    /// <summary>
    /// Errors for rejected items (index -> error message).
    /// </summary>
    public Dictionary<int, string>? Errors { get; set; }
}

/// <summary>
/// Key-value pair for lookups and dropdowns.
/// </summary>
public class LookupItem
{
    /// <summary>
    /// Value/key.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Display text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Optional group for grouped dropdowns.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Whether this item is disabled.
    /// </summary>
    public bool Disabled { get; set; }
}

/// <summary>
/// Summary counts for dashboard widgets.
/// </summary>
public class CountSummary
{
    /// <summary>
    /// Total count.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Count of active/healthy items.
    /// </summary>
    public int Active { get; set; }

    /// <summary>
    /// Count of items with warnings.
    /// </summary>
    public int Warning { get; set; }

    /// <summary>
    /// Count of critical/failed items.
    /// </summary>
    public int Critical { get; set; }
}

/// <summary>
/// Time series data point for charts.
/// </summary>
public class TimeSeriesPoint
{
    /// <summary>
    /// Timestamp for this data point.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Value at this timestamp.
    /// </summary>
    public long Value { get; set; }

    /// <summary>
    /// Optional label for the data point.
    /// </summary>
    public string? Label { get; set; }
}

/// <summary>
/// Named value for pie charts and breakdowns.
/// </summary>
public class NamedValue
{
    /// <summary>
    /// Name/label.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Value.
    /// </summary>
    public long Value { get; set; }

    /// <summary>
    /// Optional percentage.
    /// </summary>
    public decimal? Percentage { get; set; }

    /// <summary>
    /// Optional color for display.
    /// </summary>
    public string? Color { get; set; }
}
