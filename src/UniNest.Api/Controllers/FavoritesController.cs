using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniNest.Application;

namespace UniNest.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class FavoritesController(IFavoriteService favoriteService) : ControllerBase
{
    [Authorize]
    [HttpPost("listings/{id:guid}/favorite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFavorite(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var success = await favoriteService.AddFavoriteAsync(userId.Value, id, cancellationToken);
        if (!success)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Favorite failed", detail: "Listing was not found.");
        }

        return Ok(new { message = "Listing added to favorites successfully." });
    }

    [Authorize]
    [HttpDelete("listings/{id:guid}/favorite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveFavorite(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        await favoriteService.RemoveFavoriteAsync(userId.Value, id, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("favorites/mine")]
    [ProducesResponseType(typeof(IReadOnlyList<ListingSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var favorites = await favoriteService.GetMineAsync(userId.Value, cancellationToken);
        return Ok(favorites);
    }

    [Authorize]
    [HttpGet("favorites/ids")]
    [ProducesResponseType(typeof(IReadOnlySet<Guid>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MineIds(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var ids = await favoriteService.GetMineListingIdsAsync(userId.Value, cancellationToken);
        return Ok(ids);
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
