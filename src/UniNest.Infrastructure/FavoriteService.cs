using Microsoft.EntityFrameworkCore;
using UniNest.Application;
using UniNest.Domain;

namespace UniNest.Infrastructure;

public sealed class FavoriteService(UniNestDbContext db) : IFavoriteService
{
    public async Task<bool> AddFavoriteAsync(Guid userId, Guid listingId, CancellationToken cancellationToken = default)
    {
        var listingExists = await db.Listings.AsNoTracking()
            .AnyAsync(l => l.Id == listingId && l.DeletedAt == null, cancellationToken);

        if (!listingExists) return false;

        var existing = await db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ListingId == listingId, cancellationToken);

        if (existing is not null) return true; // Idempotent success

        var favorite = new Favorite
        {
            UserId = userId,
            ListingId = listingId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Favorites.Add(favorite);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveFavoriteAsync(Guid userId, Guid listingId, CancellationToken cancellationToken = default)
    {
        var favorite = await db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ListingId == listingId, cancellationToken);

        if (favorite is null) return true; // Idempotent success

        db.Favorites.Remove(favorite);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ListingSummaryDto>> GetMineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var favorites = await db.Favorites.AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        if (favorites.Count == 0) return Array.Empty<ListingSummaryDto>();

        var listingIds = favorites.Select(f => f.ListingId).ToList();

        var listings = await db.Listings.AsNoTracking()
            .Where(l => listingIds.Contains(l.Id) && l.DeletedAt == null)
            .ToListAsync(cancellationToken);

        if (listings.Count == 0) return Array.Empty<ListingSummaryDto>();

        var ownerIds = listings.Select(l => l.OwnerUserId).Distinct().ToList();
        var locationIds = listings.Select(l => l.LocationId).Distinct().ToList();

        var owners = await db.Users.AsNoTracking()
            .Where(u => ownerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        var locations = await db.Locations.AsNoTracking()
            .Where(l => locationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var primaryImageRows = await (
            from img in db.ListingImages.AsNoTracking()
            join asset in db.MediaAssets.AsNoTracking() on img.MediaAssetId equals asset.Id
            where listingIds.Contains(img.ListingId) && img.DeletedAt == null && asset.DeletedAt == null && img.IsPrimary
            select new { img.ListingId, asset.StorageKey }
        ).ToListAsync(cancellationToken);

        var primaryImages = primaryImageRows.ToDictionary(
            x => x.ListingId,
            x => FormatStorageKey(x.StorageKey)
        );

        var amenityRows = await (
            from link in db.ListingAmenities.AsNoTracking()
            join amenity in db.Amenities.AsNoTracking() on link.AmenityId equals amenity.Id
            where listingIds.Contains(link.ListingId) && amenity.IsActive
            select new { link.ListingId, amenity.NameEn }
        ).ToListAsync(cancellationToken);

        var amenitiesByListing = amenityRows
            .GroupBy(x => x.ListingId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.NameEn).ToList());

        var listingDict = listings.ToDictionary(l => l.Id);

        // Preserve order of user's favorited items
        var results = new List<ListingSummaryDto>();
        foreach (var fav in favorites)
        {
            if (!listingDict.TryGetValue(fav.ListingId, out var listing)) continue;
            var location = locations[listing.LocationId];

            results.Add(new ListingSummaryDto(
                listing.Id,
                listing.TitleEn,
                listing.TitleAr,
                listing.DescriptionEn,
                listing.DescriptionAr,
                listing.MonthlyRent,
                listing.Currency,
                location.NameEn,
                location.NameAr,
                listing.LocationId,
                listing.ListingType,
                listing.GenderPolicy,
                listing.Status,
                listing.RoomCount,
                listing.TotalBeds,
                listing.AvailableBeds,
                amenitiesByListing.GetValueOrDefault(listing.Id, []),
                owners.GetValueOrDefault(listing.OwnerUserId, "Owner"),
                listing.ContactPhone,
                listing.PublishedAt,
                primaryImages.GetValueOrDefault(listing.Id)
            ));
        }

        return results;
    }

    public async Task<IReadOnlySet<Guid>> GetMineListingIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var listingIds = await db.Favorites.AsNoTracking()
            .Where(f => f.UserId == userId)
            .Select(f => f.ListingId)
            .ToListAsync(cancellationToken);

        return listingIds.ToHashSet();
    }

    private static string FormatStorageKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "";
        key = key.Trim();
        if (key.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            key.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return key;
        }
        return "/" + key.TrimStart('/');
    }
}
