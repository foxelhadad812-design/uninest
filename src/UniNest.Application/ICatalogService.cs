namespace UniNest.Application;

public interface ICatalogService
{
    Task<IReadOnlyList<LocationDto>> GetLocationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UniversityDto>> GetUniversitiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AmenityDto>> GetAmenitiesAsync(CancellationToken cancellationToken = default);
}
