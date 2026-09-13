namespace UniNest.Application;

public interface IInquiryService
{
    Task<InquiryCommandResult> CreateInquiryAsync(Guid studentUserId, Guid listingId, InquireRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InquirySummaryDto>> GetMineAsync(Guid studentUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InquirySummaryDto>> GetReceivedAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<InquiryMessagesResult> GetMessagesAsync(Guid userId, Guid inquiryId, CancellationToken cancellationToken = default);
    Task<InquiryCommandResult> ReplyAsync(Guid ownerUserId, Guid inquiryId, SendInquiryMessageRequest request, CancellationToken cancellationToken = default);
}
