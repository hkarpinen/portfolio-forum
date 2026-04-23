using System.Security.Claims;

namespace Client.Extensions;

public static class ClaimsPrincipalExtensions
{
    private static readonly string[] MemberRoles   = { "Member", "Moderator", "Owner", "Admin" };
    private static readonly string[] ModeratorRoles = { "Moderator", "Owner", "Admin" };
    private static readonly string[] AdminRoles    = { "Admin" };

    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue("sub")
                  ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new InvalidOperationException("Missing user identifier claim.");

        if (!Guid.TryParse(raw, out var userId))
        {
            throw new InvalidOperationException("User identifier claim is not a valid GUID.");
        }

        return userId;
    }

    public static string GetRequiredRole(this ClaimsPrincipal principal) =>
        GetRole(principal) ?? throw new InvalidOperationException("Missing role claim.");

    public static bool IsMemberOrAbove(this ClaimsPrincipal principal) => HasAnyRole(principal, MemberRoles);
    public static bool IsModeratorOrAdmin(this ClaimsPrincipal principal) => HasAnyRole(principal, ModeratorRoles);
    public static bool IsAdmin(this ClaimsPrincipal principal) => HasAnyRole(principal, AdminRoles);

    private static string? GetRole(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Role) ?? principal.FindFirstValue("role");

    private static bool HasAnyRole(ClaimsPrincipal principal, string[] roles)
    {
        var role = GetRole(principal);
        return role is not null && roles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
    }
}
