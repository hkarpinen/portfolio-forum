namespace Infrastructure.Messaging.Events;

/// <summary>Must match the flat camelCase JSON the outbox produces.</summary>
/// 
public sealed record ForumThreadCreatedEvent(
    Guid ThreadId,
    Guid CommunityId,
    string CommunitySlug,
    Guid AuthorId,
    string Title,
    DateTime CreatedAt);

public sealed record ForumCommentCreatedEvent(
    Guid CommentId,
    Guid ThreadId,
    Guid AuthorId,
    string Content,
    DateTime CreatedAt,
    Guid? ParentCommentId);

public sealed record ForumMembershipInvitedEvent(
    Guid MembershipId,
    Guid CommunityId,
    Guid InvitedUserId,
    Guid InvitedByUserId,
    DateTime InvitedAt);

public sealed record ForumMembershipJoinedEvent(
    Guid MembershipId,
    Guid CommunityId,
    Guid UserId,
    DateTime JoinedAt);

public sealed record ForumModeratorAppointedEvent(
    Guid CommunityId,
    Guid UserId,
    DateTime AppointedAt);

public sealed record ForumModeratorRemovedEvent(
    Guid CommunityId,
    Guid UserId,
    DateTime RemovedAt);

public sealed record ForumUserBannedEvent(
    Guid BanId,
    Guid CommunityId,
    Guid UserId,
    DateTime BannedAt,
    string? Reason);

public sealed record ForumUserUnbannedEvent(
    Guid BanId,
    Guid CommunityId,
    Guid UserId,
    DateTime UnbannedAt);

public sealed record ForumThreadLockedEvent(
    Guid ThreadId,
    DateTime LockedAt);

public sealed record ForumThreadPinnedEvent(
    Guid ThreadId,
    DateTime PinnedAt);

public sealed record ForumCommunityOwnershipTransferredEvent(
    Guid CommunityId,
    Guid PreviousOwnerId,
    Guid NewOwnerId,
    DateTime TransferredAt);

public sealed record ForumModerationActionLoggedEvent(
    Guid LogId,
    Guid CommunityId,
    string Action,
    Guid PerformedBy,
    Guid? TargetUserId,
    string? TargetContent,
    DateTime PerformedAt);
