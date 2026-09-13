using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UniNest.Application;
using UniNest.Domain;

namespace UniNest.Infrastructure;

public sealed class AuthService(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    RoleManager<AppRole> roleManager,
    UniNestDbContext db,
    IJwtTokenService jwtTokenService,
    IOptions<JwtOptions> jwtOptions,
    IEmailService emailService,
    IOptions<EmailOptions> emailOptions
) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly EmailOptions _emailOptions = emailOptions.Value;

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedRole = request.Role.ToString();

        if (!await roleManager.RoleExistsAsync(normalizedRole))
        {
            await roleManager.CreateAsync(new AppRole { Name = normalizedRole });
        }

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new AuthResult(false, ErrorMessage: "An account with this email address already exists.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName.Trim(),
            Gender = request.Gender,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return new AuthResult(false, ErrorMessage: errors);
        }

        await userManager.AddToRoleAsync(user, normalizedRole);

        var profile = new UserProfile
        {
            UserId = user.Id,
            StudyProgram = request.StudyProgram,
            GraduationYear = request.GraduationYear,
            Bio = request.Bio,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);

        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new AuthResult(false, ErrorMessage: "Invalid email or password.");
        }

        if (!user.IsActive || user.DeletedAt != null)
        {
            return new AuthResult(false, ErrorMessage: "This account has been deactivated or suspended.");
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (signInResult.IsLockedOut)
        {
            return new AuthResult(false, ErrorMessage: "Account is temporarily locked out due to multiple failed login attempts.", IsLockedOut: true);
        }

        if (!signInResult.Succeeded)
        {
            return new AuthResult(false, ErrorMessage: "Invalid email or password.");
        }

        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResult> RefreshTokenAsync(string rawRefreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return new AuthResult(false, ErrorMessage: "Refresh token is required.");
        }

        var tokenHash = jwtTokenService.HashRefreshToken(rawRefreshToken);
        var existingToken = await db.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (existingToken == null)
        {
            return new AuthResult(false, ErrorMessage: "Invalid refresh token.");
        }

        if (existingToken.UsedAt != null)
        {
            await RevokeFamilyInternalAsync(existingToken.TokenFamilyId, revokedReason: 2, cancellationToken);
            return new AuthResult(false, ErrorMessage: "Security compromise detected. All active sessions have been revoked.");
        }

        if (existingToken.RevokedAt != null || existingToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return new AuthResult(false, ErrorMessage: "Refresh token has expired or been revoked.");
        }

        var user = await userManager.FindByIdAsync(existingToken.UserId.ToString());
        if (user == null || !user.IsActive || user.DeletedAt != null)
        {
            return new AuthResult(false, ErrorMessage: "User account is invalid or inactive.");
        }

        var now = DateTimeOffset.UtcNow;
        existingToken.UsedAt = now;

        var newRawRefreshToken = jwtTokenService.GenerateRefreshTokenValue();
        var newTokenHash = jwtTokenService.HashRefreshToken(newRawRefreshToken);

        var childToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenFamilyId = existingToken.TokenFamilyId,
            ParentTokenId = existingToken.Id,
            TokenHash = newTokenHash,
            IssuedAt = now,
            ExpiresAt = now.AddDays(_jwtOptions.RefreshTokenExpiryDays)
        };

        existingToken.ReplacedByTokenId = childToken.Id;

        db.RefreshTokens.Add(childToken);
        await db.SaveChangesAsync(cancellationToken);

        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = jwtTokenService.GenerateAccessToken(user.Id, user.Email!, user.DisplayName, roles);
        var expiresIn = (int)(expiresAt - DateTimeOffset.UtcNow).TotalSeconds;

        var userResponse = new UserResponse(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.Gender,
            user.IsActive,
            roles.ToList(),
            user.CreatedAt
        );

        var authResponse = new AuthResponse(accessToken, expiresIn, userResponse, newRawRefreshToken);
        return new AuthResult(true, Response: authResponse, RawRefreshToken: newRawRefreshToken);
    }

    public async Task RevokeRefreshTokenFamilyAsync(string rawRefreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken)) return;

        var tokenHash = jwtTokenService.HashRefreshToken(rawRefreshToken);
        var existingToken = await db.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (existingToken != null)
        {
            await RevokeFamilyInternalAsync(existingToken.TokenFamilyId, revokedReason: 1, cancellationToken);
        }
    }

    public async Task<UserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsActive || user.DeletedAt != null) return null;

        var roles = await userManager.GetRolesAsync(user);
        return new UserResponse(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.Gender,
            user.IsActive,
            roles.ToList(),
            user.CreatedAt
        );
    }

    public async Task<bool> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null) return false;

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        return result.Succeeded;
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive) return;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        if (string.IsNullOrEmpty(token)) return;

        var resetUrl = BuildResetUrl(token, user.Email!);
        await emailService.SendPasswordResetEmailAsync(user.Email!, user.DisplayName, resetUrl, cancellationToken);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive) return false;

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded) return false;

        await userManager.UpdateSecurityStampAsync(user);

        var activeTokens = await db.RefreshTokens
            .Where(x => x.UserId == user.Id && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var t in activeTokens)
        {
            t.RevokedAt = now;
            t.RevokedReason = 1;
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<AuthResult> GenerateAuthResponseAsync(AppUser user, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = jwtTokenService.GenerateAccessToken(user.Id, user.Email!, user.DisplayName, roles);
        var expiresIn = (int)(expiresAt - DateTimeOffset.UtcNow).TotalSeconds;

        var rawRefreshToken = jwtTokenService.GenerateRefreshTokenValue();
        var tokenHash = jwtTokenService.HashRefreshToken(rawRefreshToken);
        var now = DateTimeOffset.UtcNow;

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenFamilyId = Guid.NewGuid(),
            TokenHash = tokenHash,
            IssuedAt = now,
            ExpiresAt = now.AddDays(_jwtOptions.RefreshTokenExpiryDays)
        };

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(cancellationToken);

        var userResponse = new UserResponse(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.Gender,
            user.IsActive,
            roles.ToList(),
            user.CreatedAt
        );

        var authResponse = new AuthResponse(accessToken, expiresIn, userResponse, rawRefreshToken);
        return new AuthResult(true, Response: authResponse, RawRefreshToken: rawRefreshToken);
    }

    private async Task RevokeFamilyInternalAsync(Guid tokenFamilyId, short revokedReason, CancellationToken cancellationToken)
    {
        var tokens = await db.RefreshTokens
            .Where(x => x.TokenFamilyId == tokenFamilyId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var t in tokens)
        {
            t.RevokedAt = now;
            t.RevokedReason = revokedReason;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private string BuildResetUrl(string token, string email)
    {
        var baseUrl = _emailOptions.BaseUrl ?? "http://localhost:5157";
        var encodedToken = Uri.EscapeDataString(token);
        var encodedEmail = Uri.EscapeDataString(email);
        return $"{baseUrl}/reset-password?token={encodedToken}&email={encodedEmail}";
    }
}
