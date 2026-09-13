namespace UniNest.Domain;

public enum Gender : short { Unspecified, Male, Female, PreferNotToSay }
public enum LocationType : short { Governorate = 1, City, District, Neighborhood }
public enum ListingType : short { EntireApartment = 1, PrivateRoom, SharedBed }
public enum GenderPolicy : short { Any, MaleOnly, FemaleOnly }
public enum ListingStatus : short { Draft = 1, PendingReview, Published, Rejected, Suspended, Archived, Unavailable }
public enum ImageStatus : short { PendingScan = 1, Approved, Rejected, Removed }
public enum InquiryStatus : short { Open = 1, OwnerResponded, Closed, Declined, Spam }
public enum ReviewStatus : short { PendingModeration = 1, Published, Rejected, Hidden }
public enum DiscountStatus : short { Submitted = 1, UnderReview, Approved, Rejected, Withdrawn, Expired }
public enum DocumentScanStatus : short { Pending = 1, Clean, Rejected, Quarantined, Deleted }
public enum ReportStatus : short { Open = 1, UnderReview, Resolved, Dismissed }
public enum ModerationAction : short { Submitted = 1, Approved, Rejected, Suspended, Restored, Archived, SentBack = 7 }
public enum ReviewEligibilitySource : short { AdminGranted = 1, OwnerConfirmedStay, FutureReservation }
public enum SensitiveDocumentType : short { UniversityId = 1, NationalId }

/// <summary>M-8: Replaces raw short DocumentType on TermsDocument with a proper domain enum.</summary>
public enum TermsDocumentType : short { TermsOfService = 1, PrivacyPolicy = 2 }

public abstract class AuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class UserProfile : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid? UniversityId { get; set; }
    public string? StudyProgram { get; set; }
    public short? GraduationYear { get; set; }
    public string? Bio { get; set; }
    public Guid? AvatarMediaId { get; set; }
}

public sealed class Location : AuditableEntity
{
    public Guid Id { get; set; }
    public LocationType LocationType { get; set; }
    public Guid? ParentLocationId { get; set; }
    public LocationType? ParentLocationType { get; set; }
    public string NameEn { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class University : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string NameEn { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? ShortCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Listing : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid LocationId { get; set; }
    public Guid? UniversityId { get; set; }
    public Guid? ModeratedByUserId { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string TitleEn { get; set; } = null!;
    public string? TitleAr { get; set; }
    public string DescriptionEn { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public ListingType ListingType { get; set; }
    public GenderPolicy GenderPolicy { get; set; }
    public decimal MonthlyRent { get; set; }
    public string Currency { get; set; } = "EGP";
    public short RoomCount { get; set; }
    public short TotalBeds { get; set; }
    public short AvailableBeds { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.Draft;
    public string? ContactPhone { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ModeratedAt { get; set; }
    public string? ModerationReason { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public DateTimeOffset? UnavailableAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public sealed class Amenity : AuditableEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public short SortOrder { get; set; }
}

// L-3: Expanded from single-line to multi-line for readability and safer code review.

public sealed class ListingAmenity
{
    public Guid ListingId { get; set; }
    public Guid AmenityId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class Favorite
{
    public Guid UserId { get; set; }
    public Guid ListingId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class MediaAsset
{
    public Guid Id { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public string StorageKey { get; set; } = null!;
    public string OriginalFileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long ByteSize { get; set; }
    public string ChecksumSha256 { get; set; } = null!;
    public DocumentScanStatus ScanStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public sealed class ListingImage
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid MediaAssetId { get; set; }
    public Guid? ModeratedByUserId { get; set; }
    public short SortOrder { get; set; }
    public ImageStatus Status { get; set; }
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public sealed class SensitiveDocument
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid UploadedMediaAssetId { get; set; }
    public SensitiveDocumentType DocumentType { get; set; }
    public DateTimeOffset RetentionUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public sealed class ListingModerationAction
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public ModerationAction Action { get; set; }
    public ListingStatus? FromStatus { get; set; }
    public ListingStatus ToStatus { get; set; }
    public string? Reason { get; set; }
    public string? InternalNotes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TokenFamilyId { get; set; }
    public Guid? ParentTokenId { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public byte[] TokenHash { get; set; } = null!;
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public short? RevokedReason { get; set; }
}

public sealed class Inquiry : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid StudentUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public InquiryStatus Status { get; set; }
    public DateTimeOffset? LastMessageAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}

public sealed class InquiryMessage
{
    public Guid Id { get; set; }
    public Guid InquiryId { get; set; }
    public Guid SenderUserId { get; set; }
    public string Body { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? ModerationReason { get; set; }
}

public sealed class ReviewEligibility
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ListingId { get; set; }
    public Guid? GrantedByUserId { get; set; }
    public Guid? SourceInquiryId { get; set; }
    public ReviewEligibilitySource Source { get; set; }
    public DateTimeOffset GrantedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? RevokedByUserId { get; set; }
    public string? Reason { get; set; }
}

public sealed class Review : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid AuthorUserId { get; set; }
    public Guid ReviewEligibilityId { get; set; }
    public Guid? ModeratedByUserId { get; set; }
    public short Rating { get; set; }
    public ReviewStatus Status { get; set; }
    public string? Body { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public sealed class DiscountApplication : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid StudentUserId { get; set; }
    public Guid ListingId { get; set; }
    public Guid UniversityId { get; set; }
    public Guid UniversityIdDocumentId { get; set; }
    public Guid NationalIdDocumentId { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DiscountStatus Status { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? DecisionReason { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? WithdrawnAt { get; set; }
}

public sealed class Report : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid ReporterUserId { get; set; }
    public Guid? ReportedListingId { get; set; }
    public Guid? ReportedReviewId { get; set; }
    public Guid? ReportedUserId { get; set; }
    public Guid? AssignedAdminUserId { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public short ReasonCode { get; set; }
    public string? Description { get; set; }
    public ReportStatus Status { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}

/// <summary>M-8: DocumentType is now TermsDocumentType enum (was raw short).</summary>
public sealed class TermsDocument
{
    public Guid Id { get; set; }
    public TermsDocumentType DocumentType { get; set; }   // M-8: was short
    public string Version { get; set; } = null!;
    public string ContentHash { get; set; } = null!;
    public DateTimeOffset EffectiveAt { get; set; }
    public string ContentUrl { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SupersededAt { get; set; }
}

/// <summary>
/// M-9: Added DeletedAt to support GDPR erasure of consent records.
/// The unique index on (UserId, TermsDocumentId) is filtered by DeletedAt IS NULL,
/// so a soft-deleted record allows re-acceptance of the same document version.
/// </summary>
public sealed class TermsAcceptance
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TermsDocumentId { get; set; }
    public DateTimeOffset AcceptedAt { get; set; }
    public string AcceptedRoleName { get; set; } = null!;
    public string? IpHash { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }   // M-9: enables filtered unique index
}

public sealed class AuditLog
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public Guid? EntityId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Metadata { get; set; }
    public string? IpHash { get; set; }
    public string? UserAgent { get; set; }
    public Guid? CorrelationId { get; set; }
}
