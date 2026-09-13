using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniNest.Application;
using UniNest.Domain;

namespace UniNest.Api.Controllers;

[ApiController]
[Route("api/v1/listings")]
public sealed class ListingsController(IListingService listingService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ListingSearchResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] Guid? locationId,
        [FromQuery] ListingType? listingType,
        [FromQuery] GenderPolicy? genderPolicy,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await listingService.SearchPublishedAsync(
            new ListingSearchQuery(q, locationId, listingType, genderPolicy, maxPrice, skip, take),
            cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "RequireOwnerRole")]
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<ListingSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var listings = await listingService.GetMineAsync(userId.Value, cancellationToken);
        return Ok(listings);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ListingDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        // M-6: read from JWT claims — avoids FindByIdAsync + GetRolesAsync DB round-trips per view
        var isAdmin = User.IsInRole("Admin");
        var listing = await listingService.GetByIdAsync(id, GetUserId(), isAdmin, cancellationToken);
        if (listing is null) return NotFound();
        return Ok(listing);
    }

    [Authorize(Policy = "RequireOwnerRole")]
    [HttpPost]
    [ProducesResponseType(typeof(ListingDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateListingRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await listingService.CreateDraftAsync(userId.Value, request, cancellationToken);
        return ToActionResult(result, created: true);
    }

    [Authorize(Policy = "RequireOwnerRole")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ListingDetailsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateListingRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await listingService.UpdateDraftAsync(userId.Value, id, request, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize(Policy = "RequireOwnerRole")]
    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(typeof(ListingDetailsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await listingService.SubmitForReviewAsync(userId.Value, id, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(ListingDetailsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await listingService.PublishAsync(userId.Value, id, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize]
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(ListingDetailsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var isAdmin = User.IsInRole("Admin");
        var result = await listingService.ArchiveAsync(userId.Value, id, isAdmin, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Admin-only: suspend a Published or PendingReview listing.</summary>
    [Authorize(Policy = "RequireAdminRole")]
    [HttpPost("{id:guid}/suspend")]
    [ProducesResponseType(typeof(ListingDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendListingRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await listingService.SuspendAsync(userId.Value, id, request.Reason, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Admin-only: restore a Suspended listing back to Published.</summary>
    [Authorize(Policy = "RequireAdminRole")]
    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(typeof(ListingDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await listingService.RestoreAsync(userId.Value, id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Admin-only: send a Suspended listing back to Draft so the owner can edit and resubmit.</summary>
    [Authorize(Policy = "RequireAdminRole")]
    [HttpPost("{id:guid}/send-back")]
    [ProducesResponseType(typeof(ListingDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendBackToDraft(Guid id, [FromBody] SendBackToDraftRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await listingService.SendBackToDraftAsync(userId.Value, id, request.Reason, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize]
    [HttpPost("{id:guid}/images")]
    [ProducesResponseType(typeof(ListingImageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddImage(Guid id, [FromBody] AddListingImageRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await listingService.AddImageAsync(userId.Value, id, request, cancellationToken);
        if (result is null)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Failed to add image", detail: "Either listing/media asset was not found or you are not the owner of this listing.");
        }

        return CreatedAtAction(nameof(GetById), new { id }, result);
    }

    [HttpGet("{id:guid}/images")]
    [ProducesResponseType(typeof(IReadOnlyList<ListingImageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImages(Guid id, CancellationToken cancellationToken)
    {
        var images = await listingService.GetImagesAsync(id, cancellationToken);
        return Ok(images);
    }

    [Authorize]
    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveImage(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var success = await listingService.RemoveImageAsync(userId.Value, id, imageId, cancellationToken);
        if (!success)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Failed to remove image", detail: "Either image was not found or you are not the owner of this listing.");
        }

        return NoContent();
    }

    private IActionResult ToActionResult(ListingCommandResult result, bool created = false)
    {
        if (!result.Success || result.Listing is null)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: "Listing request failed",
                detail: result.ErrorMessage);
        }

        if (created)
            return CreatedAtAction(nameof(GetById), new { id = result.Listing.Id }, result.Listing);

        return Ok(result.Listing);
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

/// <summary>Request body for suspending a listing. Reason is required for audit trail.</summary>
public record SuspendListingRequest(string Reason);

/// <summary>Request body for sending a suspended listing back to draft. Reason is required for audit trail.</summary>
public record SendBackToDraftRequest(string Reason);
