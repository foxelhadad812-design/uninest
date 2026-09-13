using UniNest.Domain;

namespace UniNest.Application;

public sealed record LocationDto(
    Guid Id,
    LocationType LocationType,
    Guid? ParentLocationId,
    string NameEn,
    string NameAr
);

public sealed record UniversityDto(
    Guid Id,
    Guid LocationId,
    string NameEn,
    string NameAr,
    string? ShortCode
);

public sealed record AmenityDto(
    Guid Id,
    string Code,
    string NameEn,
    string NameAr
);
