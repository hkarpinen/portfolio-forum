namespace Client.Authorization;

public static class ForumAuthorizationPolicies
{
    public const string MemberOrAbove = nameof(MemberOrAbove);
    public const string ModeratorOrAdmin = nameof(ModeratorOrAdmin);
    public const string AdminOnly = nameof(AdminOnly);
}
