using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniNest.Application;

namespace UniNest.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    private const string RefreshTokenCookieName = "X-Refresh-Token";

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await authService.RegisterAsync(request, cancellationToken);
        if (!result.Success || result.Response == null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Registration failed", detail: result.ErrorMessage);
        }

        SetRefreshTokenCookie(result.RawRefreshToken);
        return Ok(result.Response);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status423Locked)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        // 400 = malformed/missing fields (caught by ModelState)
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await authService.LoginAsync(request, cancellationToken);

        // 423 = account locked due to too many failed attempts
        if (result.IsLockedOut)
            return Problem(statusCode: StatusCodes.Status423Locked, title: "Account Locked Out", detail: result.ErrorMessage);

        // 401 = valid request but wrong credentials / inactive account
        // (NOT 400 — that would be incorrect HTTP semantics for authentication failure)
        if (!result.Success || result.Response == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Authentication failed", detail: result.ErrorMessage);

        SetRefreshTokenCookie(result.RawRefreshToken);
        return Ok(result.Response);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto? body, CancellationToken cancellationToken)
    {
        var rawToken = body?.RefreshToken ?? Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Authentication failed", detail: "No refresh token provided.");
        }

        var result = await authService.RefreshTokenAsync(rawToken, cancellationToken);
        if (!result.Success || result.Response == null)
        {
            Response.Cookies.Delete(RefreshTokenCookieName);
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Authentication failed", detail: result.ErrorMessage);
        }

        SetRefreshTokenCookie(result.RawRefreshToken);
        return Ok(result.Response);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto? body, CancellationToken cancellationToken)
    {
        var rawToken = body?.RefreshToken ?? Request.Cookies[RefreshTokenCookieName];
        if (!string.IsNullOrWhiteSpace(rawToken))
        {
            await authService.RevokeRefreshTokenFamilyAsync(rawToken, cancellationToken);
        }

        Response.Cookies.Delete(RefreshTokenCookieName);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await authService.GetCurrentUserAsync(userId, cancellationToken);
        if (user == null) return Unauthorized();

        return Ok(user);
    }

    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var success = await authService.VerifyEmailAsync(request, cancellationToken);
        if (!success)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Email verification failed", detail: "Invalid user ID or confirmation token.");
        }

        return Ok(new { message = "Email confirmed successfully." });
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ForgotPasswordAsync(request, cancellationToken);
        // Consistent response without leaking email existence
        return Ok(new { message = "If the email address exists in our system, a password reset link has been sent." });
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var success = await authService.ResetPasswordAsync(request, cancellationToken);
        if (!success)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Password reset failed", detail: "Invalid token, email, or password criteria.");
        }

        Response.Cookies.Delete(RefreshTokenCookieName);
        return Ok(new { message = "Password reset successfully. Active sessions have been invalidated." });
    }

    private void SetRefreshTokenCookie(string? rawRefreshToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken)) return;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/api/v1/auth"
        };

        Response.Cookies.Append(RefreshTokenCookieName, rawRefreshToken, cookieOptions);
    }
}

public record RefreshTokenRequestDto(string? RefreshToken);
