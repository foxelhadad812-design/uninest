using Microsoft.AspNetCore.Identity;
using UniNest.Domain;

namespace UniNest.Infrastructure;

public sealed class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = null!;
    public Gender Gender { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset? ErasedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}

public sealed class AppRole : IdentityRole<Guid> { }
