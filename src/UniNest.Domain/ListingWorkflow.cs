namespace UniNest.Domain;

public static class ListingWorkflow
{
    public static bool CanOwnerEdit(ListingStatus status) =>
        status is ListingStatus.Draft or ListingStatus.Rejected;

    public static bool CanSubmitForReview(ListingStatus status) =>
        status is ListingStatus.Draft or ListingStatus.Rejected;

    public static bool CanAdminPublish(ListingStatus status) =>
        status is ListingStatus.PendingReview;

    /// <summary>Admin can suspend a live or pending-review listing.</summary>
    public static bool CanSuspend(ListingStatus status) =>
        status is ListingStatus.Published or ListingStatus.PendingReview;

    /// <summary>
    /// Admin can restore a suspended listing back to Published.
    /// (It was visible before suspension, so restoring to Published is the correct state.)
    /// </summary>
    public static bool CanRestore(ListingStatus status) =>
        status is ListingStatus.Suspended;

    /// <summary>
    /// Admin can send a suspended listing back to Draft so the owner can fix issues and resubmit.
    /// </summary>
    public static bool CanSendBackToDraft(ListingStatus status) =>
        status is ListingStatus.Suspended;

    /// <summary>
    /// Archived is a terminal state reachable from all non-terminal states,
    /// including Suspended — so a moderated listing can always be archived.
    /// </summary>
    public static bool CanArchive(ListingStatus status) =>
        status is ListingStatus.Draft or ListingStatus.PendingReview or ListingStatus.Published
            or ListingStatus.Rejected or ListingStatus.Unavailable or ListingStatus.Suspended;

    public static bool IsPubliclyVisible(ListingStatus status, DateTimeOffset? deletedAt) =>
        status == ListingStatus.Published && deletedAt is null;
}
