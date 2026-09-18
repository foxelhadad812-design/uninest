using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniNest.Domain;
using UniNest.Infrastructure;

namespace UniNest.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ReviewsController(UniNestDbContext db) : ControllerBase
{
    public record CreateReviewDto(short Rating, string Comment);

    [HttpGet("listings/{id:guid}/reviews")]
    public async Task<IActionResult> GetReviews(Guid id, CancellationToken cancellationToken)
    {
        var reviews = await db.Reviews
            .AsNoTracking()
            .Where(r => r.ListingId == id && r.DeletedAt == null)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.Rating,
                Comment = r.Body,
                r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new { items = reviews, count = reviews.Count });
    }

    [Authorize]
    [HttpPost("listings/{id:guid}/reviews")]
    public async Task<IActionResult> AddReview(Guid id, [FromBody] CreateReviewDto dto, CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var listingExists = await db.Listings.AnyAsync(l => l.Id == id && l.DeletedAt == null, cancellationToken);
        if (!listingExists) return NotFound(new { message = "Listing not found" });

        if (dto.Rating < 1 || dto.Rating > 5) return BadRequest(new { message = "Rating must be between 1 and 5 stars" });

        var review = new Review
        {
            Id = Guid.NewGuid(),
            ListingId = id,
            AuthorUserId = userId,
            Rating = dto.Rating,
            Body = dto.Comment,
            Status = ReviewStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Reviews.Add(review);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Review published successfully", reviewId = review.Id });
    }
}
