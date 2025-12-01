using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FMSLogNexus.Core.DTOs;
using FMSLogNexus.Core.DTOs.Requests;
using FMSLogNexus.Core.DTOs.Responses;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Services;

namespace FMSLogNexus.Api.Controllers;

/// <summary>
/// Controller for user management operations.
/// </summary>
[Route("api/v1/users")]
[ApiExplorerSettings(GroupName = "Users")]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly IApiKeyService _apiKeyService;
    private readonly IAuthService _authService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        IApiKeyService apiKeyService,
        IAuthService authService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _apiKeyService = apiKeyService;
        _authService = authService;
        _logger = logger;
    }

    #region User Management

    /// <summary>
    /// Creates a new user (admin only).
    /// </summary>
    /// <param name="request">User creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created user.</returns>
    [HttpPost]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var user = await _userService.CreateUserAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetUser), new { userId = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return ConflictResponse(ex.Message);
        }
    }

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User.</returns>
    [HttpGet("{userId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetUser(
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Non-admins can only view their own profile
        if (!IsAdmin && CurrentUserId != userId)
            return ForbiddenResponse();

        var user = await _userService.GetUserAsync(userId, cancellationToken);
        if (user == null)
            return NotFoundResponse("User", userId);

        return Ok(user);
    }

    /// <summary>
    /// Updates a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated user.</returns>
    [HttpPut("{userId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> UpdateUser(
        Guid userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Non-admins can only update their own profile (excluding role)
        if (!IsAdmin)
        {
            if (CurrentUserId != userId)
                return ForbiddenResponse();
            request.Role = null; // Prevent non-admins from changing their role
        }

        try
        {
            var user = await _userService.UpdateUserAsync(userId, request, cancellationToken);
            if (user == null)
                return NotFoundResponse("User", userId);

            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return ConflictResponse(ex.Message);
        }
    }

    /// <summary>
    /// Deletes (deactivates) a user (admin only).
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{userId:guid}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Prevent deleting yourself
        if (CurrentUserId == userId)
            return BadRequestResponse("Cannot delete your own account.");

        var result = await _userService.DeleteUserAsync(userId, cancellationToken);
        if (!result)
            return NotFoundResponse("User", userId);

        return NoContent();
    }

    /// <summary>
    /// Searches users (admin only).
    /// </summary>
    /// <param name="search">Search text.</param>
    /// <param name="role">Role filter.</param>
    /// <param name="isActive">Active filter.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated users.</returns>
    [HttpGet]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(PaginatedResult<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<UserResponse>>> SearchUsers(
        [FromQuery] string? search = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.SearchUsersAsync(search, role, isActive, page, pageSize, cancellationToken);
        return Ok(result);
    }

    #endregion

    #region User Actions

    /// <summary>
    /// Activates a user (admin only).
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("{userId:guid}/activate")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateUser(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _userService.ActivateUserAsync(userId, cancellationToken);
        if (!result)
            return NotFoundResponse("User", userId);

        return NoContent();
    }

    /// <summary>
    /// Deactivates a user (admin only).
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("{userId:guid}/deactivate")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Prevent deactivating yourself
        if (CurrentUserId == userId)
            return BadRequestResponse("Cannot deactivate your own account.");

        var result = await _userService.DeactivateUserAsync(userId, cancellationToken);
        if (!result)
            return NotFoundResponse("User", userId);

        return NoContent();
    }

    /// <summary>
    /// Unlocks a user account (admin only).
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("{userId:guid}/unlock")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlockUser(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _userService.UnlockUserAsync(userId, cancellationToken);
        if (!result)
            return NotFoundResponse("User", userId);

        return NoContent();
    }

    /// <summary>
    /// Resets a user's password (admin only).
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Reset password request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("{userId:guid}/reset-password")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        Guid userId,
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.ResetPasswordAsync(userId, request.NewPassword, cancellationToken);
        if (!result)
            return NotFoundResponse("User", userId);

        return NoContent();
    }

    #endregion

    #region User Preferences

    /// <summary>
    /// Gets user preferences.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User preferences.</returns>
    [HttpGet("{userId:guid}/preferences")]
    [Authorize]
    [ProducesResponseType(typeof(UserPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserPreferencesResponse>> GetPreferences(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!IsAdmin && CurrentUserId != userId)
            return ForbiddenResponse();

        var prefs = await _userService.GetPreferencesAsync(userId, cancellationToken);
        if (prefs == null)
            return NotFoundResponse("User", userId);

        return Ok(prefs);
    }

    /// <summary>
    /// Updates user preferences.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Update preferences request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated preferences.</returns>
    [HttpPut("{userId:guid}/preferences")]
    [Authorize]
    [ProducesResponseType(typeof(UserPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserPreferencesResponse>> UpdatePreferences(
        Guid userId,
        [FromBody] UpdatePreferencesRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsAdmin && CurrentUserId != userId)
            return ForbiddenResponse();

        var prefs = await _userService.UpdatePreferencesAsync(userId, request, cancellationToken);
        if (prefs == null)
            return NotFoundResponse("User", userId);

        return Ok(prefs);
    }

    #endregion

    #region API Keys

    /// <summary>
    /// Creates an API key.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">API key creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created API key (with full key shown once).</returns>
    [HttpPost("{userId:guid}/api-keys")]
    [Authorize]
    [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiKeyResponse>> CreateApiKey(
        Guid userId,
        [FromBody] CreateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!IsAdmin && CurrentUserId != userId)
            return ForbiddenResponse();

        var key = await _apiKeyService.CreateApiKeyAsync(userId, request, cancellationToken);
        return CreatedAtAction(nameof(GetApiKey), new { userId, keyId = key.Id }, key);
    }

    /// <summary>
    /// Gets an API key by ID.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="keyId">API key ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>API key (without full key).</returns>
    [HttpGet("{userId:guid}/api-keys/{keyId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiKeyListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiKeyListItem>> GetApiKey(
        Guid userId,
        Guid keyId,
        CancellationToken cancellationToken)
    {
        if (!IsAdmin && CurrentUserId != userId)
            return ForbiddenResponse();

        var key = await _apiKeyService.GetApiKeyAsync(keyId, cancellationToken);
        if (key == null)
            return NotFoundResponse("API key", keyId);

        return Ok(key);
    }

    /// <summary>
    /// Gets all API keys for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="includeRevoked">Include revoked keys.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>API keys.</returns>
    [HttpGet("{userId:guid}/api-keys")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<ApiKeyListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ApiKeyListItem>>> GetUserApiKeys(
        Guid userId,
        [FromQuery] bool includeRevoked = false,
        CancellationToken cancellationToken = default)
    {
        if (!IsAdmin && CurrentUserId != userId)
            return ForbiddenResponse();

        var keys = await _apiKeyService.GetUserApiKeysAsync(userId, includeRevoked, cancellationToken);
        return Ok(keys);
    }

    /// <summary>
    /// Revokes an API key.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="keyId">API key ID.</param>
    /// <param name="reason">Revocation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{userId:guid}/api-keys/{keyId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeApiKey(
        Guid userId,
        Guid keyId,
        [FromQuery] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsAdmin && CurrentUserId != userId)
            return ForbiddenResponse();

        var result = await _apiKeyService.RevokeApiKeyAsync(keyId, userId, reason, cancellationToken);
        if (!result)
            return NotFoundResponse("API key", keyId);

        return NoContent();
    }

    /// <summary>
    /// Revokes all API keys for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="reason">Revocation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of keys revoked.</returns>
    [HttpDelete("{userId:guid}/api-keys")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> RevokeAllApiKeys(
        Guid userId,
        [FromQuery] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsAdmin && CurrentUserId != userId)
            return ForbiddenResponse();

        var count = await _apiKeyService.RevokeAllUserApiKeysAsync(userId, reason, cancellationToken);
        return Ok(new { RevokedCount = count });
    }

    #endregion

    #region Validation

    /// <summary>
    /// Checks if a username is available.
    /// </summary>
    /// <param name="username">Username to check.</param>
    /// <param name="excludeUserId">User ID to exclude.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Availability result.</returns>
    [HttpGet("check-username")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> CheckUsernameAvailable(
        [FromQuery] string username,
        [FromQuery] Guid? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var available = await _userService.IsUsernameAvailableAsync(username, excludeUserId, cancellationToken);
        return Ok(new { Username = username, IsAvailable = available });
    }

    /// <summary>
    /// Checks if an email is available.
    /// </summary>
    /// <param name="email">Email to check.</param>
    /// <param name="excludeUserId">User ID to exclude.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Availability result.</returns>
    [HttpGet("check-email")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> CheckEmailAvailable(
        [FromQuery] string email,
        [FromQuery] Guid? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var available = await _userService.IsEmailAvailableAsync(email, excludeUserId, cancellationToken);
        return Ok(new { Email = email, IsAvailable = available });
    }

    #endregion
}

/// <summary>
/// Reset password request.
/// </summary>
public class ResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
