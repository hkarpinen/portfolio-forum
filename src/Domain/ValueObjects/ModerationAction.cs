namespace Forum.Domain.ValueObjects;

public enum ModerationAction
{
    BanUser,
    UnbanUser,
    DeleteThread,
    DeleteComment,
    LockThread,
    PinThread,
    AppointModerator,
    RemoveModerator,
    ResolveReportApproved,
    ResolveReportRemoved,
    ResolveReportDismissed
}
