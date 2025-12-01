using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Interfaces.Services;
using FMSLogNexus.Infrastructure.Services;

namespace FMSLogNexus.Api.Controllers;

/// <summary>
/// Controller for authentication operations.
/// </summary>
[Route("api/v1/auth")]
[ApiExplorerSettings(GroupName = "Authentication")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns JWT tokens.
    /// </summary>
    /// <param name="request">Login request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login response with tokens.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(request, ClientIpAddress, ClientUserAgent, cancellationToken);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                ErrorCodes.AccountLocked => StatusCode(423, new ProblemDetails
                {
                    Status = 423,
                    Title = "Account Locked",
                    Detail = result.ErrorMessage
                }),
                ErrorCodes.AccountDisabled => StatusCode(403, new ProblemDetails
                {
                    Status = 403,
                    Title = "Account Disabled",
                    Detail = result.ErrorMessage
                }),
                _ => Unauthorized(new ProblemDetails
                {
                    Status = 401,
                    Title = "Unauthorized",
                    Detail = result.ErrorMessage
                })
            };
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    /// <param name="request">Refresh token request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New tokens.</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.RefreshTokenAsync(request, ClientIpAddress, ClientUserAgent, cancellationToken);

        if (!result.Success)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = 401,
                Title = "Unauthorized",
                Detail = result.ErrorMessage
            });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Logs out by revoking the refresh token.
    /// </summary>
    /// <param name="refreshToken">Refresh token to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        [FromBody] string refreshToken,
        CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(refreshToken, ClientIpAddress, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Logs out from all sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAllSessions(CancellationToken cancellationToken)
    {
        if (!CurrentUserId.HasValue)
            return UnauthorizedResponse();

        await _authService.LogoutAllSessionsAsync(CurrentUserId.Value, ClientIpAddress, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Changes the current user's password.
    /// </summary>
    /// <param name="request">Change password request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!CurrentUserId.HasValue)
            return UnauthorizedResponse();

        var result = await _authService.ChangePasswordAsync(CurrentUserId.Value, request, cancellationToken);
        if (!result)
        {
            return BadRequestResponse("Current password is incorrect.");
        }

        return NoContent();
    }

    /// <summary>
    /// Gets the current authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current user information.</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        if (!CurrentUserId.HasValue)
            return UnauthorizedResponse();

        var user = await _authService.GetCurrentUserAsync(CurrentUserId.Value, cancellationToken);
        if (user == null)
            return NotFoundResponse("User", CurrentUserId.Value);

        return Ok(user);
    }

    /// <summary>
    /// Validates a JWT access token.
    /// </summary>
    /// <param name="token">Token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token validation result.</returns>
    [HttpPost("validate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenValidationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenValidationResult>> ValidateToken(
        [FromBody] string token,
        CancellationToken cancellationToken)
    {
        var result = await _authService.ValidateTokenAsync(token, cancellationToken);
        return Ok(result);
    }
}
