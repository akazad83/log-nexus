using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Api.Controllers;

/// <summary>
/// Base controller providing common functionality for all API controllers.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Gets the current user's ID from claims.
    /// </summary>
    protected Guid? CurrentUserId
    {
        get
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            return claim != null && Guid.TryParse(claim.Value, out var id) ? id : null;
        }
    }

    /// <summary>
    /// Gets the current user's username from claims.
    /// </summary>
    protected string? CurrentUsername => User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("unique_name")?.Value;

    /// <summary>
    /// Gets the current user's role from claims.
    /// </summary>
    protected UserRole? CurrentRole
    {
        get
        {
            var claim = User.FindFirst(ClaimTypes.Role);
            return claim != null && Enum.TryParse<UserRole>(claim.Value, out var role) ? role : null;
        }
    }

    /// <summary>
    /// Gets the client IP address.
    /// </summary>
    protected string? ClientIpAddress
    {
        get
        {
            // Check for forwarded IP (behind proxy/load balancer)
            var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',').First().Trim();
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }
    }

    /// <summary>
    /// Gets the client user agent.
    /// </summary>
    protected string? ClientUserAgent => HttpContext.Request.Headers.UserAgent.FirstOrDefault();

    /// <summary>
    /// Returns a standardized not found response.
    /// </summary>
    protected ActionResult NotFoundResponse(string entityName, object id)
    {
        return NotFound(new ProblemDetails
        {
            Status = 404,
            Title = "Not Found",
            Detail = $"{entityName} with ID '{id}' was not found."
        });
    }

    /// <summary>
    /// Returns a standardized bad request response.
    /// </summary>
    protected ActionResult BadRequestResponse(string message)
    {
        return BadRequest(new ProblemDetails
        {
            Status = 400,
            Title = "Bad Request",
            Detail = message
        });
    }

    /// <summary>
    /// Returns a standardized conflict response.
    /// </summary>
    protected ActionResult ConflictResponse(string message)
    {
        return Conflict(new ProblemDetails
        {
            Status = 409,
            Title = "Conflict",
            Detail = message
        });
    }

    /// <summary>
    /// Returns a standardized forbidden response.
    /// </summary>
    protected ActionResult ForbiddenResponse(string message = "You do not have permission to perform this action.")
    {
        return StatusCode(403, new ProblemDetails
        {
            Status = 403,
            Title = "Forbidden",
            Detail = message
        });
    }

    /// <summary>
    /// Returns a standardized unauthorized response.
    /// </summary>
    protected ActionResult UnauthorizedResponse(string message = "Authentication required.")
    {
        return Unauthorized(new ProblemDetails
        {
            Status = 401,
            Title = "Unauthorized",
            Detail = message
        });
    }

    /// <summary>
    /// Checks if the current user has the required role.
    /// </summary>
    protected bool HasRole(params UserRole[] allowedRoles)
    {
        return CurrentRole.HasValue && allowedRoles.Contains(CurrentRole.Value);
    }

    /// <summary>
    /// Checks if the current user is an admin.
    /// </summary>
    protected bool IsAdmin => HasRole(UserRole.Administrator);

    /// <summary>
    /// Checks if the current user can manage the specified user.
    /// </summary>
    protected bool CanManageUser(Guid targetUserId)
    {
        return IsAdmin || CurrentUserId == targetUserId;
    }
}

/// <summary>
/// Standard API response wrapper.
/// </summary>
/// <typeparam name="T">Data type.</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public static ApiResponse<T> Fail(string message, IDictionary<string, string[]>? errors = null) => new()
    {
        Success = false,
        Message = message,
        Errors = errors
    };
}

/// <summary>
/// Standard API response without data.
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public static ApiResponse Ok(string? message = null) => new() { Success = true, Message = message };
    public static ApiResponse Fail(string message) => new() { Success = false, Message = message };
}
