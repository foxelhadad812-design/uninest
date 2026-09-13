using System.ComponentModel.DataAnnotations;
using UniNest.Domain;

namespace UniNest.Application;

/// <summary>
/// L-2: Using an enum instead of a raw string prevents arbitrary role names at the API boundary.
/// JsonStringEnumConverter (configured in Program.cs) means the wire format stays "Student"/"Owner".
/// </summary>
public enum RegistrationRole { Student, Owner }

public record RegisterRequest(
    [Required][EmailAddress][MaxLength(256)] string Email,
    [Required][MinLength(12)][MaxLength(128)] string Password,
    [Required][MaxLength(120)] string DisplayName,
    Gender Gender,
    [Required] RegistrationRole Role,
    [MaxLength(150)] string? StudyProgram,
    short? GraduationYear,
    [MaxLength(1000)] string? Bio
);

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password
);

public record AuthResponse(
    string AccessToken,
    int ExpiresIn,
    UserResponse User,
    string RefreshToken
);

public record UserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    Gender Gender,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt
);

public record VerifyEmailRequest(
    Guid UserId,
    string Token
);

public record ForgotPasswordRequest(
    [Required][EmailAddress] string Email
);

public record ResetPasswordRequest(
    [Required][EmailAddress] string Email,
    [Required] string Token,
    [Required][MinLength(12)][MaxLength(128)] string NewPassword
);
