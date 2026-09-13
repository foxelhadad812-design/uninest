using Microsoft.EntityFrameworkCore;
using UniNest.Application;
using UniNest.Domain;

namespace UniNest.Infrastructure;

public sealed class InquiryService(UniNestDbContext db) : IInquiryService
{
    public async Task<InquiryCommandResult> CreateInquiryAsync(Guid studentUserId, Guid listingId, InquireRequest request, CancellationToken cancellationToken = default)
    {
        var listing = await db.Listings.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == listingId && l.DeletedAt == null, cancellationToken);

        if (listing is null)
        {
            return new InquiryCommandResult(false, ErrorMessage: "Listing not found.", StatusCode: 404);
        }

        if (listing.OwnerUserId == studentUserId)
        {
            return new InquiryCommandResult(false, ErrorMessage: "You cannot send an inquiry on your own listing.", StatusCode: 400);
        }

        // Check if active inquiry already exists for this student and listing
        var existingInquiry = await db.Inquiries.AsNoTracking()
            .FirstOrDefaultAsync(i => i.StudentUserId == studentUserId 
                                   && i.ListingId == listingId 
                                   && (i.Status == InquiryStatus.Open || i.Status == InquiryStatus.OwnerResponded), cancellationToken);

        if (existingInquiry is not null)
        {
            return new InquiryCommandResult(false, ErrorMessage: "You already have an active inquiry for this listing.", StatusCode: 409);
        }

        var now = DateTimeOffset.UtcNow;
        var inquiry = new Inquiry
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            StudentUserId = studentUserId,
            OwnerUserId = listing.OwnerUserId,
            Status = InquiryStatus.Open,
            LastMessageAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        var message = new InquiryMessage
        {
            Id = Guid.NewGuid(),
            InquiryId = inquiry.Id,
            SenderUserId = studentUserId,
            Body = request.Message.Trim(),
            CreatedAt = now
        };

        db.Inquiries.Add(inquiry);
        db.InquiryMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        var student = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == studentUserId, cancellationToken);
        var owner = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == listing.OwnerUserId, cancellationToken);

        var summaryDto = new InquirySummaryDto(
            inquiry.Id,
            listing.Id,
            listing.TitleEn,
            studentUserId,
            student?.DisplayName ?? "Student",
            listing.OwnerUserId,
            owner?.DisplayName ?? "Owner",
            inquiry.Status,
            message.Body,
            inquiry.LastMessageAt,
            inquiry.CreatedAt
        );

        var messageDto = new InquiryMessageDto(
            message.Id,
            message.InquiryId,
            message.SenderUserId,
            student?.DisplayName ?? "Student",
            message.Body,
            message.CreatedAt
        );

        return new InquiryCommandResult(true, Inquiry: summaryDto, Message: messageDto, StatusCode: 201);
    }

    public async Task<IReadOnlyList<InquirySummaryDto>> GetMineAsync(Guid studentUserId, CancellationToken cancellationToken = default)
    {
        var inquiries = await db.Inquiries.AsNoTracking()
            .Where(i => i.StudentUserId == studentUserId)
            .OrderByDescending(i => i.LastMessageAt ?? i.CreatedAt)
            .ToListAsync(cancellationToken);

        if (inquiries.Count == 0) return Array.Empty<InquirySummaryDto>();

        var listingIds = inquiries.Select(i => i.ListingId).Distinct().ToList();
        var userIds = inquiries.Select(i => i.StudentUserId)
            .Concat(inquiries.Select(i => i.OwnerUserId))
            .Distinct().ToList();
        var inquiryIds = inquiries.Select(i => i.Id).ToList();

        var listings = await db.Listings.AsNoTracking()
            .Where(l => listingIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.TitleEn, cancellationToken);

        var users = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        var lastMessages = await db.InquiryMessages.AsNoTracking()
            .Where(m => inquiryIds.Contains(m.InquiryId) && m.DeletedAt == null)
            .OrderByDescending(m => m.CreatedAt)
            .GroupBy(m => m.InquiryId)
            .Select(g => new { InquiryId = g.Key, LastMessage = g.First().Body })
            .ToDictionaryAsync(x => x.InquiryId, x => x.LastMessage, cancellationToken);

        return inquiries.Select(i => new InquirySummaryDto(
            i.Id,
            i.ListingId,
            listings.GetValueOrDefault(i.ListingId, "Listing"),
            i.StudentUserId,
            users.GetValueOrDefault(i.StudentUserId, "Student"),
            i.OwnerUserId,
            users.GetValueOrDefault(i.OwnerUserId, "Owner"),
            i.Status,
            lastMessages.GetValueOrDefault(i.Id),
            i.LastMessageAt,
            i.CreatedAt
        )).ToList();
    }

    public async Task<IReadOnlyList<InquirySummaryDto>> GetReceivedAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var inquiries = await db.Inquiries.AsNoTracking()
            .Where(i => i.OwnerUserId == ownerUserId)
            .OrderByDescending(i => i.LastMessageAt ?? i.CreatedAt)
            .ToListAsync(cancellationToken);

        if (inquiries.Count == 0) return Array.Empty<InquirySummaryDto>();

        var listingIds = inquiries.Select(i => i.ListingId).Distinct().ToList();
        var userIds = inquiries.Select(i => i.StudentUserId)
            .Concat(inquiries.Select(i => i.OwnerUserId))
            .Distinct().ToList();
        var inquiryIds = inquiries.Select(i => i.Id).ToList();

        var listings = await db.Listings.AsNoTracking()
            .Where(l => listingIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.TitleEn, cancellationToken);

        var users = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        var lastMessages = await db.InquiryMessages.AsNoTracking()
            .Where(m => inquiryIds.Contains(m.InquiryId) && m.DeletedAt == null)
            .OrderByDescending(m => m.CreatedAt)
            .GroupBy(m => m.InquiryId)
            .Select(g => new { InquiryId = g.Key, LastMessage = g.First().Body })
            .ToDictionaryAsync(x => x.InquiryId, x => x.LastMessage, cancellationToken);

        return inquiries.Select(i => new InquirySummaryDto(
            i.Id,
            i.ListingId,
            listings.GetValueOrDefault(i.ListingId, "Listing"),
            i.StudentUserId,
            users.GetValueOrDefault(i.StudentUserId, "Student"),
            i.OwnerUserId,
            users.GetValueOrDefault(i.OwnerUserId, "Owner"),
            i.Status,
            lastMessages.GetValueOrDefault(i.Id),
            i.LastMessageAt,
            i.CreatedAt
        )).ToList();
    }

    public async Task<InquiryMessagesResult> GetMessagesAsync(Guid userId, Guid inquiryId, CancellationToken cancellationToken = default)
    {
        var inquiry = await db.Inquiries.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == inquiryId, cancellationToken);

        if (inquiry is null)
        {
            return new InquiryMessagesResult(false, ErrorMessage: "Inquiry not found.", StatusCode: 404);
        }

        if (inquiry.StudentUserId != userId && inquiry.OwnerUserId != userId)
        {
            return new InquiryMessagesResult(false, ErrorMessage: "You are not authorized to view messages for this inquiry.", StatusCode: 403);
        }

        var messages = await db.InquiryMessages.AsNoTracking()
            .Where(m => m.InquiryId == inquiryId && m.DeletedAt == null)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        var senderUserIds = messages.Select(m => m.SenderUserId).Distinct().ToList();
        var users = await db.Users.AsNoTracking()
            .Where(u => senderUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        var messageDtos = messages.Select(m => new InquiryMessageDto(
            m.Id,
            m.InquiryId,
            m.SenderUserId,
            users.GetValueOrDefault(m.SenderUserId, "User"),
            m.Body,
            m.CreatedAt
        )).ToList();

        return new InquiryMessagesResult(true, Messages: messageDtos, StatusCode: 200);
    }

    public async Task<InquiryCommandResult> ReplyAsync(Guid ownerUserId, Guid inquiryId, SendInquiryMessageRequest request, CancellationToken cancellationToken = default)
    {
        var inquiry = await db.Inquiries.FirstOrDefaultAsync(i => i.Id == inquiryId, cancellationToken);

        if (inquiry is null)
        {
            return new InquiryCommandResult(false, ErrorMessage: "Inquiry not found.", StatusCode: 404);
        }

        if (inquiry.OwnerUserId != ownerUserId)
        {
            return new InquiryCommandResult(false, ErrorMessage: "Only the listing owner can reply to this inquiry.", StatusCode: 403);
        }

        var now = DateTimeOffset.UtcNow;
        var message = new InquiryMessage
        {
            Id = Guid.NewGuid(),
            InquiryId = inquiryId,
            SenderUserId = ownerUserId,
            Body = request.Message.Trim(),
            CreatedAt = now
        };

        inquiry.Status = InquiryStatus.OwnerResponded;
        inquiry.LastMessageAt = now;
        inquiry.UpdatedAt = now;

        db.InquiryMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        var owner = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == ownerUserId, cancellationToken);

        var messageDto = new InquiryMessageDto(
            message.Id,
            message.InquiryId,
            message.SenderUserId,
            owner?.DisplayName ?? "Owner",
            message.Body,
            message.CreatedAt
        );

        return new InquiryCommandResult(true, Message: messageDto, StatusCode: 200);
    }
}
