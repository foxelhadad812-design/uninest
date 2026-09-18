using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using UniNest.Domain;
using UniNest.Infrastructure;

namespace UniNest.Api.Hubs;

[Authorize]
public class ChatHub(UniNestDbContext db) : Hub
{
    public async Task JoinInquiryGroup(string inquiryId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, inquiryId);
    }

    public async Task LeaveInquiryGroup(string inquiryId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, inquiryId);
    }

    public async Task SendMessage(string inquiryIdStr, string messageBody)
    {
        if (!Guid.TryParse(inquiryIdStr, out var inquiryId)) return;
        if (string.IsNullOrWhiteSpace(messageBody)) return;

        var userIdStr = Context.UserIdentifier;
        if (!Guid.TryParse(userIdStr, out var senderUserId)) return;

        var inquiry = await db.Inquiries.FirstOrDefaultAsync(i => i.Id == inquiryId);
        if (inquiry == null) return;

        if (inquiry.StudentUserId != senderUserId && inquiry.OwnerUserId != senderUserId) return;

        var msg = new InquiryMessage
        {
            Id = Guid.NewGuid(),
            InquiryId = inquiryId,
            SenderUserId = senderUserId,
            Body = messageBody.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        inquiry.LastMessageAt = DateTimeOffset.UtcNow;
        db.InquiryMessages.Add(msg);
        await db.SaveChangesAsync();

        await Clients.Group(inquiryIdStr).SendAsync("ReceiveMessage", new
        {
            msg.Id,
            msg.InquiryId,
            msg.SenderUserId,
            msg.Body,
            msg.CreatedAt
        });
    }
}
