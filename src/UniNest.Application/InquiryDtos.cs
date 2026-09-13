using System.ComponentModel.DataAnnotations;
using UniNest.Domain;

namespace UniNest.Application;

public sealed record InquireRequest(
    [Required(ErrorMessage = "Message is required.")][MaxLength(4000, ErrorMessage = "Message cannot exceed 4000 characters.")] string Message
);

public sealed record SendInquiryMessageRequest(
    [Required(ErrorMessage = "Message is required.")][MaxLength(4000, ErrorMessage = "Message cannot exceed 4000 characters.")] string Message
);

public sealed record InquiryMessageDto(
    Guid Id,
    Guid InquiryId,
    Guid SenderUserId,
    string SenderDisplayName,
    string Body,
    DateTimeOffset CreatedAt
);

public sealed record InquirySummaryDto(
    Guid Id,
    Guid ListingId,
    string ListingTitleEn,
    Guid StudentUserId,
    string StudentDisplayName,
    Guid OwnerUserId,
    string OwnerDisplayName,
    InquiryStatus Status,
    string? LastMessage,
    DateTimeOffset? LastMessageAt,
    DateTimeOffset CreatedAt
);

public sealed record InquiryCommandResult(
    bool Success,
    InquirySummaryDto? Inquiry = null,
    InquiryMessageDto? Message = null,
    string? ErrorMessage = null,
    int StatusCode = 400
);

public sealed record InquiryMessagesResult(
    bool Success,
    IReadOnlyList<InquiryMessageDto>? Messages = null,
    string? ErrorMessage = null,
    int StatusCode = 200
);
