using System.Security.Claims;
using UniNest.Domain;

namespace UniNest.Application;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(Guid userId, string email, string displayName, IEnumerable<string> roles);
    string GenerateRefreshTokenValue();
    byte[] HashRefreshToken(string refreshToken);
}

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> RefreshTokenAsync(string rawRefreshToken, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenFamilyAsync(string rawRefreshToken, CancellationToken cancellationToken = default);
    Task<UserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}

public record AuthResult(
    bool Success,
    AuthResponse? Response = null,
    string? RawRefreshToken = null,
    string? ErrorMessage = null,
    bool IsLockedOut = false
);
