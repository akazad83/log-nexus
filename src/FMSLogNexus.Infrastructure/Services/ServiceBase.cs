using Microsoft.Extensions.Logging;
using FMSLogNexus.Core.Interfaces.Repositories;

namespace FMSLogNexus.Infrastructure.Services;

/// <summary>
/// Base class for all services providing common functionality.
/// </summary>
public abstract class ServiceBase
{
    protected readonly IUnitOfWork UnitOfWork;
    protected readonly ILogger Logger;

    protected ServiceBase(IUnitOfWork unitOfWork, ILogger logger)
    {
        UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes an operation with standard error handling and logging.
    /// </summary>
    protected async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Starting operation: {OperationName}", operationName);
            var result = await operation();
            Logger.LogDebug("Completed operation: {OperationName}", operationName);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in operation: {OperationName}", operationName);
            throw;
        }
    }

    /// <summary>
    /// Executes an operation within a transaction.
    /// </summary>
    protected async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Starting transactional operation: {OperationName}", operationName);
            var result = await UnitOfWork.ExecuteInTransactionAsync(operation, cancellationToken);
            Logger.LogDebug("Completed transactional operation: {OperationName}", operationName);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in transactional operation: {OperationName}", operationName);
            throw;
        }
    }

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    protected async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Saves changes with audit tracking.
    /// </summary>
    protected async Task<int> SaveChangesWithAuditAsync(
        Guid? userId,
        string? username,
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWork.SaveChangesWithAuditAsync(userId, username, cancellationToken);
    }
}

/// <summary>
/// Standard service result for operations that may fail.
/// </summary>
/// <typeparam name="T">Result data type.</typeparam>
public class ServiceResult<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public IDictionary<string, string[]>? ValidationErrors { get; private set; }

    private ServiceResult() { }

    public static ServiceResult<T> Ok(T data) => new()
    {
        Success = true,
        Data = data
    };

    public static ServiceResult<T> Fail(string errorCode, string errorMessage) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };

    public static ServiceResult<T> ValidationFail(IDictionary<string, string[]> errors) => new()
    {
        Success = false,
        ErrorCode = "VALIDATION_ERROR",
        ErrorMessage = "One or more validation errors occurred.",
        ValidationErrors = errors
    };

    public static ServiceResult<T> NotFound(string entityName, object id) => new()
    {
        Success = false,
        ErrorCode = "NOT_FOUND",
        ErrorMessage = $"{entityName} with ID '{id}' was not found."
    };
}

/// <summary>
/// Service result for operations without return data.
/// </summary>
public class ServiceResult
{
    public bool Success { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public IDictionary<string, string[]>? ValidationErrors { get; private set; }

    private ServiceResult() { }

    public static ServiceResult Ok() => new() { Success = true };

    public static ServiceResult Fail(string errorCode, string errorMessage) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };

    public static ServiceResult ValidationFail(IDictionary<string, string[]> errors) => new()
    {
        Success = false,
        ErrorCode = "VALIDATION_ERROR",
        ErrorMessage = "One or more validation errors occurred.",
        ValidationErrors = errors
    };
}

/// <summary>
/// Common error codes used across services.
/// </summary>
public static class ErrorCodes
{
    // Authentication
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string AccountDisabled = "ACCOUNT_DISABLED";
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string TokenInvalid = "TOKEN_INVALID";
    public const string ApiKeyInvalid = "API_KEY_INVALID";
    public const string ApiKeyExpired = "API_KEY_EXPIRED";
    public const string ApiKeyRevoked = "API_KEY_REVOKED";

    // Authorization
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";

    // Validation
    public const string ValidationError = "VALIDATION_ERROR";
    public const string InvalidInput = "INVALID_INPUT";
    public const string DuplicateEntry = "DUPLICATE_ENTRY";

    // Resources
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";

    // Operations
    public const string OperationFailed = "OPERATION_FAILED";
    public const string ConcurrencyError = "CONCURRENCY_ERROR";

    // Server
    public const string InternalError = "INTERNAL_ERROR";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
}
