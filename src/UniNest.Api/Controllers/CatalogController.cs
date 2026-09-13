using Microsoft.AspNetCore.Mvc;
using UniNest.Application;

namespace UniNest.Api.Controllers;

[ApiController]
[Route("api/v1/catalog")]
public sealed class CatalogController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet("locations")]
    [ProducesResponseType(typeof(IReadOnlyList<LocationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Locations(CancellationToken cancellationToken)
        => Ok(await catalogService.GetLocationsAsync(cancellationToken));

    [HttpGet("universities")]
    [ProducesResponseType(typeof(IReadOnlyList<UniversityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Universities(CancellationToken cancellationToken)
        => Ok(await catalogService.GetUniversitiesAsync(cancellationToken));

    [HttpGet("amenities")]
    [ProducesResponseType(typeof(IReadOnlyList<AmenityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Amenities(CancellationToken cancellationToken)
        => Ok(await catalogService.GetAmenitiesAsync(cancellationToken));
}
