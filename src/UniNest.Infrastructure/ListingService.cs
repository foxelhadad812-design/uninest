using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniNest.Application;
using UniNest.Domain;

namespace UniNest.Infrastructure;

public sealed class ListingService(UniNestDbContext db, UserManager<AppUser> userManager) : IListingService
{
    public async Task<ListingSearchResult> SearchPublishedAsync(ListingSearchQuery query, CancellationToken cancellationToken = default)
    {
        var take = query.Take is < 1 or > 100 ? 50 : query.Take;
        var skip = query.Skip < 0 ? 0 : query.Skip;

        var listings = db.Listings.AsNoTracking()
            .Where(l => l.Status == ListingStatus.Published && l.DeletedAt == null);

        if (query.LocationId is Guid locationId)
            listings = listings.Where(l => l.LocationId == locationId);

        if (query.ListingType is ListingType listingType)
            listings = listings.Where(l => l.ListingType == listingType);

        if (query.GenderPolicy is GenderPolicy genderPolicy)
            listings = listings.Where(l => l.GenderPolicy == genderPolicy);

        if (query.MaxPrice is decimal maxPrice and > 0)
            listings = listings.Where(l => l.MonthlyRent <= maxPrice);

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            listings =
                from listing in listings
                join location in db.Locations.AsNoTracking() on listing.LocationId equals location.Id
                where listing.TitleEn.Contains(term)
                    || (listing.TitleAr != null && listing.TitleAr.Contains(term))
                    || listing.DescriptionEn.Contains(term)
                    || (listing.DescriptionAr != null && listing.DescriptionAr.Contains(term))
                    || location.NameEn.Contains(term)
                    || location.NameAr.Contains(term)
                select listing;
        }

        var totalCount = await listings.CountAsync(cancellationToken);
        var page = await listings
            .OrderByDescending(l => l.PublishedAt)
            .ThenByDescending(l => l.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var summaries = await MapSummariesAsync(page, includeContactPhone: false, cancellationToken);
        return new ListingSearchResult(summaries, totalCount);
    }

    public async Task<ListingDetailsDto?> GetByIdAsync(Guid listingId, Guid? viewerUserId, bool viewerIsAdmin = false, CancellationToken cancellationToken = default)
    {
        var listing = await db.Listings.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == listingId && l.DeletedAt == null, cancellationToken);
        if (listing is null) return null;

        // M-6: isAdmin is now passed from JWT claims at the controller — no Identity DB round-trips.
        var isOwner = viewerUserId.HasValue && listing.OwnerUserId == viewerUserId.Value;

        if (!ListingWorkflow.IsPubliclyVisible(listing.Status, listing.DeletedAt) && !isOwner && !viewerIsAdmin)
            return null;

        var showPhone = viewerIsAdmin || isOwner;
        return await MapDetailsAsync(listing, showPhone, cancellationToken);
    }

    public async Task<IReadOnlyList<ListingSummaryDto>> GetMineAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var listings = await db.Listings.AsNoTracking()
            .Where(l => l.OwnerUserId == ownerUserId && l.DeletedAt == null)
            .OrderByDescending(l => l.UpdatedAt)
            .ToListAsync(cancellationToken);

        return await MapSummariesAsync(listings, includeContactPhone: true, cancellationToken);
    }

    public async Task<ListingCommandResult> CreateDraftAsync(Guid ownerUserId, CreateListingRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateListingInput(
            request.TitleEn, request.DescriptionEn, request.MonthlyRent,
            request.RoomCount, request.TotalBeds, request.AvailableBeds, request.ContactPhone);
        if (validationError is not null)
            return new ListingCommandResult(false, ErrorMessage: validationError);

        var locationOk = await db.Locations.AnyAsync(l => l.Id == request.LocationId && l.IsActive, cancellationToken);
        if (!locationOk)
            return new ListingCommandResult(false, ErrorMessage: "Location was not found or is inactive.");

        if (request.UniversityId is Guid universityId)
        {
            var universityOk = await db.Universities.AnyAsync(u => u.Id == universityId && u.IsActive, cancellationToken);
            if (!universityOk)
                return new ListingCommandResult(false, ErrorMessage: "University was not found or is inactive.");
        }

        var amenityIds = request.AmenityIds?.Distinct().ToList() ?? [];
        var amenityCheck = await ValidateAmenitiesAsync(amenityIds, cancellationToken);
        if (amenityCheck is not null)
            return new ListingCommandResult(false, ErrorMessage: amenityCheck);

        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            LocationId = request.LocationId,
            UniversityId = request.UniversityId,
            TitleEn = request.TitleEn.Trim(),
            TitleAr = TrimToNull(request.TitleAr),
            DescriptionEn = request.DescriptionEn.Trim(),
            DescriptionAr = TrimToNull(request.DescriptionAr),
            ListingType = request.ListingType,
            GenderPolicy = request.GenderPolicy,
            MonthlyRent = request.MonthlyRent,
            Currency = "EGP",
            RoomCount = request.RoomCount,
            TotalBeds = request.TotalBeds,
            AvailableBeds = request.AvailableBeds,
            Status = ListingStatus.Draft,
            ContactPhone = TrimToNull(request.ContactPhone)
        };

        db.Listings.Add(listing);
        AttachAmenities(listing.Id, amenityIds);
        await db.SaveChangesAsync(cancellationToken);

        var created = await MapDetailsAsync(listing, includeContactPhone: true, cancellationToken);
        return new ListingCommandResult(true, created);
    }

    public async Task<ListingCommandResult> UpdateDraftAsync(Guid ownerUserId, Guid listingId, UpdateListingRequest request, CancellationToken cancellationToken = default)
    {
        var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == listingId && l.DeletedAt == null, cancellationToken);
        if (listing is null)
            return new ListingCommandResult(false, ErrorMessage: "Listing was not found.", StatusCode: 404);

        if (listing.OwnerUserId != ownerUserId)
            return new ListingCommandResult(false, ErrorMessage: "You can only update your own listings.", StatusCode: 403);

        if (!ListingWorkflow.CanOwnerEdit(listing.Status))
            return new ListingCommandResult(false, ErrorMessage: "Only draft or rejected listings can be edited.");

        var validationError = ValidateListingInput(
            request.TitleEn, request.DescriptionEn, request.MonthlyRent,
            request.RoomCount, request.TotalBeds, request.AvailableBeds, request.ContactPhone);
        if (validationError is not null)
            return new ListingCommandResult(false, ErrorMessage: validationError);

        var locationOk = await db.Locations.AnyAsync(l => l.Id == request.LocationId && l.IsActive, cancellationToken);
        if (!locationOk)
            return new ListingCommandResult(false, ErrorMessage: "Location was not found or is inactive.");

        if (request.UniversityId is Guid universityId)
        {
            var universityOk = await db.Universities.AnyAsync(u => u.Id == universityId && u.IsActive, cancellationToken);
            if (!universityOk)
                return new ListingCommandResult(false, ErrorMessage: "University was not found or is inactive.");
        }

        var amenityIds = request.AmenityIds?.Distinct().ToList() ?? [];
        var amenityCheck = await ValidateAmenitiesAsync(amenityIds, cancellationToken);
        if (amenityCheck is not null)
            return new ListingCommandResult(false, ErrorMessage: amenityCheck);

        listing.LocationId = request.LocationId;
        listing.UniversityId = request.UniversityId;
        listing.TitleEn = request.TitleEn.Trim();
        listing.TitleAr = TrimToNull(request.TitleAr);
        listing.DescriptionEn = request.DescriptionEn.Trim();
        listing.DescriptionAr = TrimToNull(request.DescriptionAr);
        listing.ListingType = request.ListingType;
        listing.GenderPolicy = request.GenderPolicy;
        listing.MonthlyRent = request.MonthlyRent;
        listing.RoomCount = request.RoomCount;
        listing.TotalBeds = request.TotalBeds;
        listing.AvailableBeds = request.AvailableBeds;
        listing.ContactPhone = TrimToNull(request.ContactPhone);

        var existingAmenities = db.ListingAmenities.Where(x => x.ListingId == listing.Id);
        db.ListingAmenities.RemoveRange(existingAmenities);
        AttachAmenities(listing.Id, amenityIds);
        await db.SaveChangesAsync(cancellationToken);

        var updated = await MapDetailsAsync(listing, includeContactPhone: true, cancellationToken);
        return new ListingCommandResult(true, updated);
    }

    public async Task<ListingCommandResult> SubmitForReviewAsync(Guid ownerUserId, Guid listingId, CancellationToken cancellationToken = default)
    {
        var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == listingId && l.DeletedAt == null, cancellationToken);
        if (listing is null)
            return new ListingCommandResult(false, ErrorMessage: "Listing was not found.", StatusCode: 404);

        if (listing.OwnerUserId != ownerUserId)
            return new ListingCommandResult(false, ErrorMessage: "You can only submit your own listings.", StatusCode: 403);

        if (!ListingWorkflow.CanSubmitForReview(listing.Status))
            return new ListingCommandResult(false, ErrorMessage: "Only draft or rejected listings can be submitted for review.");

        var fromStatus = listing.Status;
        listing.Status = ListingStatus.PendingReview;
        db.ListingModerationActions.Add(new ListingModerationAction
        {
            Id = Guid.NewGuid(),
            ListingId = listing.Id,
            PerformedByUserId = ownerUserId,
            Action = ModerationAction.Submitted,
            FromStatus = fromStatus,
            ToStatus = ListingStatus.PendingReview,
            Reason = "Owner submitted listing for review."
        });

        await db.SaveChangesAsync(cancellationToken);
        var dto = await MapDetailsAsync(listing, includeContactPhone: true, cancellationToken);
        return new ListingCommandResult(true, dto);
    }

    public async Task<ListingCommandResult> PublishAsync(Guid adminUserId, Guid listingId, CancellationToken cancellationToken = default)
    {
        var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == listingId && l.DeletedAt == null, cancellationToken);
        if (listing is null)
            return new ListingCommandResult(false, ErrorMessage: "Listing was not found.", StatusCode: 404);

        if (!ListingWorkflow.CanAdminPublish(listing.Status))
            return new ListingCommandResult(false, ErrorMessage: "Only listings pending review can be published.");

        var now = DateTimeOffset.UtcNow;
        var fromStatus = listing.Status;
        listing.Status = ListingStatus.Published;
        listing.PublishedAt = now;
        listing.ModeratedByUserId = adminUserId;
        listing.ModeratedAt = now;
        listing.ModerationReason = null;

        db.ListingModerationActions.Add(new ListingModerationAction
        {
            Id = Guid.NewGuid(),
            ListingId = listing.Id,
            PerformedByUserId = adminUserId,
            Action = ModerationAction.Approved,
            FromStatus = fromStatus,
            ToStatus = ListingStatus.Published,
            Reason = "Admin published listing."
        });

        await db.SaveChangesAsync(cancellationToken);
        var dto = await MapDetailsAsync(listing, includeContactPhone: true, cancellationToken);
        return new ListingCommandResult(true, dto);
    }

    public async Task<ListingCommandResult> ArchiveAsync(Guid actorUserId, Guid listingId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == listingId && l.DeletedAt == null, cancellationToken);
        if (listing is null)
            return new ListingCommandResult(false, ErrorMessage: "Listing was not found.", StatusCode: 404);

        if (!isAdmin && listing.OwnerUserId != actorUserId)
            return new ListingCommandResult(false, ErrorMessage: "You can only archive your own listings.", StatusCode: 403);

        if (!ListingWorkflow.CanArchive(listing.Status))
            return new ListingCommandResult(false, ErrorMessage: "This listing cannot be archived in its current status.");

        var fromStatus = listing.Status;
        listing.Status = ListingStatus.Archived;
        listing.ArchivedAt = DateTimeOffset.UtcNow;

        db.ListingModerationActions.Add(new ListingModerationAction
        {
            Id = Guid.NewGuid(),
            ListingId = listing.Id,
            PerformedByUserId = actorUserId,
            Action = ModerationAction.Archived,
            FromStatus = fromStatus,
            ToStatus = ListingStatus.Archived
        });

        await db.SaveChangesAsync(cancellationToken);
        var dto = await MapDetailsAsync(listing, includeContactPhone: isAdmin || listing.OwnerUserId == actorUserId, cancellationToken);
        return new ListingCommandResult(true, dto);
    }

    public async Task<ListingCommandResult> SuspendAsync(Guid adminUserId, Guid listingId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return new ListingCommandResult(false, ErrorMessage: "A suspension reason is required.");

        var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == listingId && l.DeletedAt == null, cancellationToken);
        if (listing is null)
            return new ListingCommandResult(false, ErrorMessage: "Listing was not found.", StatusCode: 404);

        if (!ListingWorkflow.CanSuspend(listing.Status))
            return new ListingCommandResult(false, ErrorMessage: $"A listing in '{listing.Status}' status cannot be suspended.");

        var fromStatus = listing.Status;
        listing.Status = ListingStatus.Suspended;
        listing.ModeratedByUserId = adminUserId;
        listing.ModeratedAt = DateTimeOffset.UtcNow;
        listing.ModerationReason = reason.Trim();

        db.ListingModerationActions.Add(new ListingModerationAction
        {
            Id = Guid.NewGuid(),
            ListingId = listing.Id,
            PerformedByUserId = adminUserId,
            Action = ModerationAction.Suspended,
            FromStatus = fromStatus,
            ToStatus = ListingStatus.Suspended,
            Reason = reason.Trim()
        });

        await db.SaveChangesAsync(cancellationToken);
        var dto = await MapDetailsAsync(listing, includeContactPhone: true, cancellationToken);
        return new ListingCommandResult(true, dto);
    }

    public async Task<ListingCommandResult> RestoreAsync(Guid adminUserId, Guid listingId, CancellationToken cancellationToken = default)
    {
        var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == listingId && l.DeletedAt == null, cancellationToken);
        if (listing is null)
            return new ListingCommandResult(false, ErrorMessage: "Listing was not found.", StatusCode: 404);

        if (!ListingWorkflow.CanRestore(listing.Status))
            return new ListingCommandResult(false, ErrorMessage: $"A listing in '{listing.Status}' status cannot be restored. Only Suspended listings can be restored.");

        // Restore to Published — it was publicly visible before suspension.
        listing.Status = ListingStatus.Published;
        listing.ModerationReason = null;

        db.ListingModerationActions.Add(new ListingModerationAction
        {
            Id = Guid.NewGuid(),
            ListingId = listing.Id,
            PerformedByUserId = adminUserId,
            Action = ModerationAction.Restored,
            FromStatus = ListingStatus.Suspended,
            ToStatus = ListingStatus.Published,
            Reason = "Admin restored suspended listing."
        });

        await db.SaveChangesAsync(cancellationToken);
        var dto = await MapDetailsAsync(listing, includeContactPhone: true, cancellationToken);
        return new ListingCommandResult(true, dto);
    }

    public async Task<ListingCommandResult> SendBackToDraftAsync(Guid adminUserId, Guid listingId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return new ListingCommandResult(false, ErrorMessage: "A send-back reason is required.");

        var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == listingId && l.DeletedAt == null, cancellationToken);
        if (listing is null)
            return new ListingCommandResult(false, ErrorMessage: "Listing was not found.", StatusCode: 404);

        if (!ListingWorkflow.CanSendBackToDraft(listing.Status))
            return new ListingCommandResult(false, ErrorMessage: $"A listing in '{listing.Status}' status cannot be sent back to draft. Only Suspended listings can be sent back.");

        var fromStatus = listing.Status;
        listing.Status = ListingStatus.Draft;
        listing.ModerationReason = reason.Trim();

        db.ListingModerationActions.Add(new ListingModerationAction
        {
            Id = Guid.NewGuid(),
            ListingId = listing.Id,
            PerformedByUserId = adminUserId,
            Action = ModerationAction.SentBack,
            FromStatus = fromStatus,
            ToStatus = ListingStatus.Draft,
            Reason = reason.Trim()
        });

        await db.SaveChangesAsync(cancellationToken);
        var dto = await MapDetailsAsync(listing, includeContactPhone: true, cancellationToken);
        return new ListingCommandResult(true, dto);
    }

    private async Task<IReadOnlyList<ListingSummaryDto>> MapSummariesAsync(
        IReadOnlyList<Listing> listings,
        bool includeContactPhone,
        CancellationToken cancellationToken)
    {
        if (listings.Count == 0) return [];

        var listingIds = listings.Select(l => l.Id).ToList();
        var ownerIds = listings.Select(l => l.OwnerUserId).Distinct().ToList();
        var locationIds = listings.Select(l => l.LocationId).Distinct().ToList();

        var owners = await db.Users.AsNoTracking()
            .Where(u => ownerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        var locations = await db.Locations.AsNoTracking()
            .Where(l => locationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var primaryImages = await (
            from img in db.ListingImages.AsNoTracking()
            join asset in db.MediaAssets.AsNoTracking() on img.MediaAssetId equals asset.Id
            where listingIds.Contains(img.ListingId) && img.DeletedAt == null && asset.DeletedAt == null && img.IsPrimary
            select new { img.ListingId, StorageKey = "/" + asset.StorageKey.TrimStart('/') }
        ).ToDictionaryAsync(x => x.ListingId, x => x.StorageKey, cancellationToken);

        var amenityRows = await (
            from link in db.ListingAmenities.AsNoTracking()
            join amenity in db.Amenities.AsNoTracking() on link.AmenityId equals amenity.Id
            where listingIds.Contains(link.ListingId) && amenity.IsActive
            select new { link.ListingId, amenity.NameEn }
        ).ToListAsync(cancellationToken);

        var amenitiesByListing = amenityRows
            .GroupBy(x => x.ListingId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.NameEn).ToList());

        return listings.Select(listing =>
        {
            var location = locations[listing.LocationId];
            return new ListingSummaryDto(
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
                includeContactPhone ? listing.ContactPhone : null,
                listing.PublishedAt,
                primaryImages.GetValueOrDefault(listing.Id)
            );
        }).ToList();
    }

    private async Task<ListingDetailsDto> MapDetailsAsync(Listing listing, bool includeContactPhone, CancellationToken cancellationToken)
    {
        var location = await db.Locations.AsNoTracking().FirstAsync(l => l.Id == listing.LocationId, cancellationToken);
        var ownerName = await db.Users.AsNoTracking()
            .Where(u => u.Id == listing.OwnerUserId)
            .Select(u => u.DisplayName)
            .FirstAsync(cancellationToken);

        var amenities = await (
            from link in db.ListingAmenities.AsNoTracking()
            join amenity in db.Amenities.AsNoTracking() on link.AmenityId equals amenity.Id
            where link.ListingId == listing.Id && amenity.IsActive
            orderby amenity.SortOrder
            select new AmenityDto(amenity.Id, amenity.Code, amenity.NameEn, amenity.NameAr)
        ).ToListAsync(cancellationToken);

        var images = await GetImagesAsync(listing.Id, cancellationToken);
        var primaryUrl = images.FirstOrDefault(i => i.IsPrimary)?.Url ?? images.FirstOrDefault()?.Url;

        return new ListingDetailsDto(
            listing.Id,
            listing.TitleEn,
            listing.TitleAr,
            listing.DescriptionEn,
            listing.DescriptionAr,
            listing.MonthlyRent,
            listing.Currency,
            listing.LocationId,
            location.NameEn,
            location.NameAr,
            listing.UniversityId,
            listing.ListingType,
            listing.GenderPolicy,
            listing.Status,
            listing.RoomCount,
            listing.TotalBeds,
            listing.AvailableBeds,
            includeContactPhone ? listing.ContactPhone : null,
            amenities,
            listing.OwnerUserId,
            ownerName,
            listing.CreatedAt,
            listing.PublishedAt,
            primaryUrl,
            images
        );
    }

    public async Task<ListingImageDto?> AddImageAsync(Guid ownerUserId, Guid listingId, AddListingImageRequest request, CancellationToken cancellationToken = default)
    {
        var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == listingId && l.DeletedAt == null, cancellationToken);
        if (listing is null || listing.OwnerUserId != ownerUserId)
            return null;

        var asset = await db.MediaAssets.AsNoTracking().FirstOrDefaultAsync(m => m.Id == request.MediaAssetId && m.DeletedAt == null, cancellationToken);
        if (asset is null)
            return null;

        var existingImages = await db.ListingImages
            .Where(i => i.ListingId == listingId && i.DeletedAt == null)
            .ToListAsync(cancellationToken);

        // Enforcement: First image is automatically primary, or if request explicitly sets IsPrimary
        bool isPrimary = existingImages.Count == 0 || request.IsPrimary == true;

        if (isPrimary)
        {
            foreach (var img in existingImages.Where(i => i.IsPrimary))
            {
                img.IsPrimary = false;
            }
        }

        short sortOrder = (short)(existingImages.Count > 0 ? existingImages.Max(i => i.SortOrder) + 1 : 1);

        var listingImage = new ListingImage
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            MediaAssetId = request.MediaAssetId,
            Status = ImageStatus.Approved,
            SortOrder = sortOrder,
            IsPrimary = isPrimary,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ListingImages.Add(listingImage);
        await db.SaveChangesAsync(cancellationToken);

        var imageUrl = "/" + asset.StorageKey.TrimStart('/');
        return new ListingImageDto(listingImage.Id, listingId, request.MediaAssetId, imageUrl, isPrimary, sortOrder);
    }

    public async Task<IReadOnlyList<ListingImageDto>> GetImagesAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var images = await (
            from img in db.ListingImages.AsNoTracking()
            join asset in db.MediaAssets.AsNoTracking() on img.MediaAssetId equals asset.Id
            where img.ListingId == listingId && img.DeletedAt == null && asset.DeletedAt == null
            orderby img.IsPrimary descending, img.SortOrder ascending, img.CreatedAt ascending
            select new ListingImageDto(
                img.Id,
                img.ListingId,
                img.MediaAssetId,
                "/" + asset.StorageKey.TrimStart('/'),
                img.IsPrimary,
                img.SortOrder
            )
        ).ToListAsync(cancellationToken);

        return images;
    }

    public async Task<bool> RemoveImageAsync(Guid ownerUserId, Guid listingId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var listing = await db.Listings.AsNoTracking().FirstOrDefaultAsync(l => l.Id == listingId && l.DeletedAt == null, cancellationToken);
        if (listing is null || listing.OwnerUserId != ownerUserId)
            return false;

        var image = await db.ListingImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ListingId == listingId && i.DeletedAt == null, cancellationToken);
        if (image is null)
            return false;

        var wasPrimary = image.IsPrimary;
        image.DeletedAt = DateTimeOffset.UtcNow;
        image.IsPrimary = false;

        if (wasPrimary)
        {
            var nextImage = await db.ListingImages
                .Where(i => i.ListingId == listingId && i.Id != imageId && i.DeletedAt == null)
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextImage is not null)
            {
                nextImage.IsPrimary = true;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool CanSeeContactPhone(Listing listing, AppUser? viewer, bool isAdmin, bool isOwner)
    {
        if (isAdmin || isOwner) return true;
        if (viewer is null) return false;
        return listing.GenderPolicy switch
        {
            GenderPolicy.Any => true,
            GenderPolicy.MaleOnly => viewer.Gender == Gender.Male,
            GenderPolicy.FemaleOnly => viewer.Gender == Gender.Female,
            _ => false
        };
    }

    private async Task<string?> ValidateAmenitiesAsync(IReadOnlyList<Guid> amenityIds, CancellationToken cancellationToken)
    {
        if (amenityIds.Count == 0) return null;
        var count = await db.Amenities.CountAsync(a => amenityIds.Contains(a.Id) && a.IsActive, cancellationToken);
        return count == amenityIds.Count ? null : "One or more amenities were not found.";
    }

    private void AttachAmenities(Guid listingId, IReadOnlyList<Guid> amenityIds)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var amenityId in amenityIds)
        {
            db.ListingAmenities.Add(new ListingAmenity
            {
                ListingId = listingId,
                AmenityId = amenityId,
                CreatedAt = now
            });
        }
    }

    private static string? ValidateListingInput(
        string titleEn,
        string descriptionEn,
        decimal monthlyRent,
        short roomCount,
        short totalBeds,
        short availableBeds,
        string? contactPhone)
    {
        if (string.IsNullOrWhiteSpace(titleEn) || titleEn.Trim().Length > 200)
            return "Title is required and must be at most 200 characters.";
        if (string.IsNullOrWhiteSpace(descriptionEn) || descriptionEn.Trim().Length > 4000)
            return "Description is required and must be at most 4000 characters.";
        if (monthlyRent <= 0)
            return "Monthly rent must be greater than zero.";
        if (roomCount < 1)
            return "Room count must be at least 1.";
        if (totalBeds < 1 || availableBeds < 0 || availableBeds > totalBeds)
            return "Bed counts are invalid.";
        if (contactPhone is { Length: > 30 })
            return "Contact phone is too long.";
        return null;
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
