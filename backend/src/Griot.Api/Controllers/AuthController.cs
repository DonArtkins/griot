using Griot.Application.DTOs.Auth;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;
using Griot.Infrastructure.Redis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

/// <summary>
/// Auth lifecycle: register / login / refresh / logout.
/// All business logic is delegated to <see cref="IAuthService"/>.
/// Rate limiting is enforced via Redis sliding window on login.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // Login rate limit: 10 attempts per 15-minute window per IP.
    private const int LoginRateLimit = 10;
    private const int LoginWindowSeconds = 900; // 15 minutes

    private readonly IAuthService _authService;
    private readonly IRedisRateLimiter _rateLimiter;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IRedisRateLimiter rateLimiter,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    /// <summary>Register a new account.</summary>
    /// <remarks>Returns 409 if the email is already in use.</remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var response = await _authService.RegisterAsync(request).ConfigureAwait(false);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (DuplicateEmailException)
        {
            return Conflict(new { message = "Email is already registered." });
        }
    }

    /// <summary>Login with email + password. Returns an access + refresh token pair.</summary>
    /// <remarks>Rate-limited: max 10 attempts per 15-minute window per IP. Returns 429 when exceeded.</remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Redis sliding-window rate limit keyed by IP.
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var rlKey = $"ratelimit:login:{ip}";

        var rlResult = await _rateLimiter.TryAcquireAsync(rlKey, LoginRateLimit, LoginWindowSeconds)
            .ConfigureAwait(false);

        if (!rlResult.Allowed)
        {
            Response.Headers["Retry-After"] = ((int)rlResult.RetryAfter.TotalSeconds).ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                message = "Too many login attempts. Please try again later.",
                retryAfterSeconds = (int)rlResult.RetryAfter.TotalSeconds
            });
        }

        var response = await _authService.LoginAsync(request).ConfigureAwait(false);
        if (response is null)
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(response);
    }

    /// <summary>Rotate a refresh token. Returns a new access + refresh token pair.</summary>
    /// <remarks>
    /// If the presented token has already been used (replay attack), the entire token family is
    /// revoked and 401 is returned.
    /// </remarks>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _authService.RefreshAsync(request.RefreshToken).ConfigureAwait(false);
        if (response is null)
            return Unauthorized(new { message = "Invalid, expired, or already-used refresh token." });

        return Ok(response);
    }

    /// <summary>Revoke the presented refresh token (logout). Idempotent.</summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _authService.LogoutAsync(request.RefreshToken).ConfigureAwait(false);
        return NoContent();
    }
}
