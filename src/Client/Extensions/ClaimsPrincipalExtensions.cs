using System.Security.Claims;

namespace Client.Extensions;

public static class ClaimsPrincipalExtensions
{
    // "Demo" must be listed: it is a real signed-in role, and omitting it 403s every
    // write for demo sessions. Admin is deliberately not a member role.
    private static readonly string[] MemberRoles = { "Member", "Moderator", "Owner", "Admin", "Demo" };
    private static readonly string[] AdminRoles  = { "Admin" };

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

    /// <summary>Null when anonymous — for endpoints open to everyone that specialise
    /// when signed in.</summary>
    public static Guid? GetUserIdOrNull(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue("sub")
                  ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(raw)) return null;
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }

    public static string GetRequiredRole(this ClaimsPrincipal principal) =>
        GetRole(principal) ?? throw new InvalidOperationException("Missing role claim.");

    public static bool IsMemberOrAbove(this ClaimsPrincipal principal) => HasAnyRole(principal, MemberRoles);
    public static bool IsAdmin(this ClaimsPrincipal principal) => HasAnyRole(principal, AdminRoles);

    private static string? GetRole(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Role) ?? principal.FindFirstValue("role");

    private static bool HasAnyRole(ClaimsPrincipal principal, string[] roles)
    {
        var role = GetRole(principal);
        return role is not null && roles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
    }
}
