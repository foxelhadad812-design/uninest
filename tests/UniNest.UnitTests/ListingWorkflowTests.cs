using UniNest.Domain;

namespace UniNest.UnitTests;

public class ListingWorkflowTests
{
    [Theory]
    [InlineData(ListingStatus.Draft, true)]
    [InlineData(ListingStatus.Rejected, true)]
    [InlineData(ListingStatus.PendingReview, false)]
    [InlineData(ListingStatus.Published, false)]
    public void Owner_can_edit_only_draft_or_rejected(ListingStatus status, bool expected)
    {
        Assert.Equal(expected, ListingWorkflow.CanOwnerEdit(status));
    }

    [Theory]
    [InlineData(ListingStatus.Draft, true)]
    [InlineData(ListingStatus.Rejected, true)]
    [InlineData(ListingStatus.Published, false)]
    public void Owner_can_submit_only_draft_or_rejected(ListingStatus status, bool expected)
    {
        Assert.Equal(expected, ListingWorkflow.CanSubmitForReview(status));
    }

    [Fact]
    public void Admin_can_publish_only_pending_review()
    {
        Assert.True(ListingWorkflow.CanAdminPublish(ListingStatus.PendingReview));
        Assert.False(ListingWorkflow.CanAdminPublish(ListingStatus.Draft));
        Assert.False(ListingWorkflow.CanAdminPublish(ListingStatus.Published));
    }

    [Fact]
    public void Published_listings_are_public_until_deleted()
    {
        Assert.True(ListingWorkflow.IsPubliclyVisible(ListingStatus.Published, null));
        Assert.False(ListingWorkflow.IsPubliclyVisible(ListingStatus.Published, DateTimeOffset.UtcNow));
        Assert.False(ListingWorkflow.IsPubliclyVisible(ListingStatus.Draft, null));
        Assert.False(ListingWorkflow.IsPubliclyVisible(ListingStatus.PendingReview, null));
    }

    [Fact]
    public void Admin_can_send_back_suspended_to_draft()
    {
        Assert.True(ListingWorkflow.CanSendBackToDraft(ListingStatus.Suspended));
        Assert.False(ListingWorkflow.CanSendBackToDraft(ListingStatus.Published));
        Assert.False(ListingWorkflow.CanSendBackToDraft(ListingStatus.Draft));
        Assert.False(ListingWorkflow.CanSendBackToDraft(ListingStatus.PendingReview));
        Assert.False(ListingWorkflow.CanSendBackToDraft(ListingStatus.Rejected));
        Assert.False(ListingWorkflow.CanSendBackToDraft(ListingStatus.Archived));
        Assert.False(ListingWorkflow.CanSendBackToDraft(ListingStatus.Unavailable));
    }

    [Fact]
    public void Suspended_listing_has_restore_send_back_and_archive_transitions()
    {
        // From Suspended, the owner can:
        // 1. Edit once sent back to Draft (via CanSendBackToDraft + CanOwnerEdit)
        // 2. Submit for review once in Draft (via CanSubmitForReview)

        Assert.True(ListingWorkflow.CanSendBackToDraft(ListingStatus.Suspended));
        Assert.True(ListingWorkflow.CanRestore(ListingStatus.Suspended));
        Assert.True(ListingWorkflow.CanArchive(ListingStatus.Suspended));

        // After send-back to Draft, owner can edit and submit
        Assert.True(ListingWorkflow.CanOwnerEdit(ListingStatus.Draft));
        Assert.True(ListingWorkflow.CanSubmitForReview(ListingStatus.Draft));
    }
}
