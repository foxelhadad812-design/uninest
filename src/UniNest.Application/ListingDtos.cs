using System.ComponentModel.DataAnnotations;
using UniNest.Domain;

namespace UniNest.Application;

public sealed record ListingSearchQuery(
    string? Q,
    Guid? LocationId,
    ListingType? ListingType,
    GenderPolicy? GenderPolicy,
    decimal? MaxPrice,
    int Skip = 0,
    int Take = 50
);

public sealed record ListingSearchResult(
    IReadOnlyList<ListingSummaryDto> Items,
    int TotalCount
);

public sealed record ListingImageDto(
    Guid Id,
    Guid ListingId,
    Guid MediaAssetId,
    string Url,
    bool IsPrimary,
    short SortOrder
);

public sealed record AddListingImageRequest(
    [Required] Guid MediaAssetId,
    bool? IsPrimary = null
);

public sealed record ListingSummaryDto(
    Guid Id,
    string TitleEn,
    string? TitleAr,
    string DescriptionEn,      // M-4: non-nullable — matches Listing.DescriptionEn entity field
    string? DescriptionAr,
    decimal MonthlyRent,
    string Currency,
    string LocationNameEn,
    string LocationNameAr,
    Guid LocationId,
    ListingType ListingType,
    GenderPolicy GenderPolicy,
    ListingStatus Status,
    short RoomCount,
    short TotalBeds,
    short AvailableBeds,
    IReadOnlyList<string> Amenities,
    string OwnerDisplayName,
    string? ContactPhone,
    DateTimeOffset? PublishedAt,
    string? PrimaryImageUrl = null
);

public sealed record ListingDetailsDto(
    Guid Id,
    string TitleEn,
    string? TitleAr,
    string DescriptionEn,
    string? DescriptionAr,
    decimal MonthlyRent,
    string Currency,
    Guid LocationId,
    string LocationNameEn,
    string LocationNameAr,
    Guid? UniversityId,
    ListingType ListingType,
    GenderPolicy GenderPolicy,
    ListingStatus Status,
    short RoomCount,
    short TotalBeds,
    short AvailableBeds,
    string? ContactPhone,
    IReadOnlyList<AmenityDto> Amenities,
    Guid OwnerUserId,
    string OwnerDisplayName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    string? PrimaryImageUrl = null,
    IReadOnlyList<ListingImageDto>? Images = null
);

/// <summary>M-2: All required fields validated at the DTO boundary before reaching the service layer.</summary>
public sealed record CreateListingRequest(
    [Required][MaxLength(200)] string TitleEn,
    [MaxLength(200)] string? TitleAr,
    [Required][MaxLength(4000)] string DescriptionEn,
    [MaxLength(4000)] string? DescriptionAr,
    [Required] ListingType ListingType,
    [Required] GenderPolicy GenderPolicy,
    [Range(1, 1_000_000, ErrorMessage = "MonthlyRent must be between 1 and 1,000,000.")]
    decimal MonthlyRent,
    [Range(1, 100, ErrorMessage = "RoomCount must be between 1 and 100.")] short RoomCount,
    [Range(1, 500, ErrorMessage = "TotalBeds must be between 1 and 500.")] short TotalBeds,
    [Range(0, 500, ErrorMessage = "AvailableBeds must be between 0 and TotalBeds.")] short AvailableBeds,
    [Required] Guid LocationId,
    Guid? UniversityId,
    [MaxLength(30)] string? ContactPhone,
    IReadOnlyList<Guid>? AmenityIds
);

/// <summary>M-2: Identical validation constraints as CreateListingRequest.</summary>
public sealed record UpdateListingRequest(
    [Required][MaxLength(200)] string TitleEn,
    [MaxLength(200)] string? TitleAr,
    [Required][MaxLength(4000)] string DescriptionEn,
    [MaxLength(4000)] string? DescriptionAr,
    [Required] ListingType ListingType,
    [Required] GenderPolicy GenderPolicy,
    [Range(1, 1_000_000, ErrorMessage = "MonthlyRent must be between 1 and 1,000,000.")]
    decimal MonthlyRent,
    [Range(1, 100, ErrorMessage = "RoomCount must be between 1 and 100.")] short RoomCount,
    [Range(1, 500, ErrorMessage = "TotalBeds must be between 1 and 500.")] short TotalBeds,
    [Range(0, 500, ErrorMessage = "AvailableBeds must be between 0 and TotalBeds.")] short AvailableBeds,
    [Required] Guid LocationId,
    Guid? UniversityId,
    [MaxLength(30)] string? ContactPhone,
    IReadOnlyList<Guid>? AmenityIds
);

public sealed record ListingCommandResult(
    bool Success,
    ListingDetailsDto? Listing = null,
    string? ErrorMessage = null,
    int StatusCode = 400
);
