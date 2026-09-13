using Microsoft.EntityFrameworkCore;
using UniNest.Application;

namespace UniNest.Infrastructure;

public sealed class CatalogService(UniNestDbContext db) : ICatalogService
{
    public async Task<IReadOnlyList<LocationDto>> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        return await db.Locations.AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.NameEn)
            .Select(l => new LocationDto(l.Id, l.LocationType, l.ParentLocationId, l.NameEn, l.NameAr))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UniversityDto>> GetUniversitiesAsync(CancellationToken cancellationToken = default)
    {
        return await db.Universities.AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.NameEn)
            .Select(u => new UniversityDto(u.Id, u.LocationId, u.NameEn, u.NameAr, u.ShortCode))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AmenityDto>> GetAmenitiesAsync(CancellationToken cancellationToken = default)
    {
        return await db.Amenities.AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.SortOrder)
            .Select(a => new AmenityDto(a.Id, a.Code, a.NameEn, a.NameAr))
            .ToListAsync(cancellationToken);
    }
}
