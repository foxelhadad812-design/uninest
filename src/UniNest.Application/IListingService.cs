namespace UniNest.Application;

public interface IListingService
{
    Task<ListingSearchResult> SearchPublishedAsync(ListingSearchQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// M-6: viewerIsAdmin is read from JWT claims at the controller level — avoids two Identity DB
    /// round-trips (FindByIdAsync + GetRolesAsync) just to decide contact phone visibility.
    /// </summary>
    Task<ListingDetailsDto?> GetByIdAsync(Guid listingId, Guid? viewerUserId, bool viewerIsAdmin = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ListingSummaryDto>> GetMineAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<ListingCommandResult> CreateDraftAsync(Guid ownerUserId, CreateListingRequest request, CancellationToken cancellationToken = default);
    Task<ListingCommandResult> UpdateDraftAsync(Guid ownerUserId, Guid listingId, UpdateListingRequest request, CancellationToken cancellationToken = default);
    Task<ListingCommandResult> SubmitForReviewAsync(Guid ownerUserId, Guid listingId, CancellationToken cancellationToken = default);
    Task<ListingCommandResult> PublishAsync(Guid adminUserId, Guid listingId, CancellationToken cancellationToken = default);
    Task<ListingCommandResult> ArchiveAsync(Guid actorUserId, Guid listingId, bool isAdmin, CancellationToken cancellationToken = default);

    /// <summary>Admin-only: suspend a Published or PendingReview listing.</summary>
    Task<ListingCommandResult> SuspendAsync(Guid adminUserId, Guid listingId, string reason, CancellationToken cancellationToken = default);

    /// <summary>Admin-only: restore a Suspended listing back to Published.</summary>
    Task<ListingCommandResult> RestoreAsync(Guid adminUserId, Guid listingId, CancellationToken cancellationToken = default);
    Task<ListingCommandResult> SendBackToDraftAsync(Guid adminUserId, Guid listingId, string reason, CancellationToken cancellationToken = default);

    // Image management methods
    Task<ListingImageDto?> AddImageAsync(Guid ownerUserId, Guid listingId, AddListingImageRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ListingImageDto>> GetImagesAsync(Guid listingId, CancellationToken cancellationToken = default);
    Task<bool> RemoveImageAsync(Guid ownerUserId, Guid listingId, Guid imageId, CancellationToken cancellationToken = default);
}
