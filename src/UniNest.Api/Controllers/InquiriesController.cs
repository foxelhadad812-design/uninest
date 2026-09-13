using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniNest.Application;

namespace UniNest.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class InquiriesController(IInquiryService inquiryService) : ControllerBase
{
    [Authorize]
    [HttpPost("listings/{id:guid}/inquire")]
    [ProducesResponseType(typeof(InquirySummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Inquire(Guid id, [FromBody] InquireRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await inquiryService.CreateInquiryAsync(userId.Value, id, request, cancellationToken);
        if (!result.Success || result.Inquiry is null)
        {
            return Problem(statusCode: result.StatusCode, title: "Inquiry creation failed", detail: result.ErrorMessage);
        }

        return CreatedAtAction(nameof(GetMessages), new { id = result.Inquiry.Id }, result.Inquiry);
    }

    [Authorize]
    [HttpGet("inquiries/mine")]
    [ProducesResponseType(typeof(IReadOnlyList<InquirySummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await inquiryService.GetMineAsync(userId.Value, cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "RequireOwnerRole")]
    [HttpGet("inquiries/received")]
    [ProducesResponseType(typeof(IReadOnlyList<InquirySummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Received(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await inquiryService.GetReceivedAsync(userId.Value, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("inquiries/{id:guid}/messages")]
    [ProducesResponseType(typeof(IReadOnlyList<InquiryMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await inquiryService.GetMessagesAsync(userId.Value, id, cancellationToken);
        if (!result.Success)
        {
            return Problem(statusCode: result.StatusCode, title: "Failed to get messages", detail: result.ErrorMessage);
        }

        return Ok(result.Messages);
    }

    [Authorize]
    [HttpPost("inquiries/{id:guid}/messages")]
    [ProducesResponseType(typeof(InquiryMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reply(Guid id, [FromBody] SendInquiryMessageRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await inquiryService.ReplyAsync(userId.Value, id, request, cancellationToken);
        if (!result.Success || result.Message is null)
        {
            return Problem(statusCode: result.StatusCode, title: "Reply failed", detail: result.ErrorMessage);
        }

        return Ok(result.Message);
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
