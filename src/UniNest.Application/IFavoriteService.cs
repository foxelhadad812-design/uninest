namespace UniNest.Application;

public interface IFavoriteService
{
    Task<bool> AddFavoriteAsync(Guid userId, Guid listingId, CancellationToken cancellationToken = default);
    Task<bool> RemoveFavoriteAsync(Guid userId, Guid listingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ListingSummaryDto>> GetMineAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> GetMineListingIdsAsync(Guid userId, CancellationToken cancellationToken = default);
}
