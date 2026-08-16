using System.Security.Claims;

namespace Client.Extensions;

public static class ClaimsPrincipalExtensions
{
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

    /// <summary>
    /// Asks the only question forum actually had: is there a signed-in person behind this request.
    ///
    /// This used to allow-list identity's role names — "Member", "Moderator", "Owner", "Admin",
    /// "Demo" — two of which identity has never issued, and one of which was missing long enough
    /// to 403 every write from a demo session. Forum does not own that vocabulary and has no
    /// business tracking it. What forum owns is <c>CommunityMembership.Role</c>, and that is where
    /// every real forum permission is decided.
    /// </summary>
    public static bool IsMemberOrAbove(this ClaimsPrincipal principal) =>
        principal.GetUserIdOrNull() is not null;

    /// <summary>Platform administration, which is identity's fact about the account, not forum's.</summary>
    public static bool IsAdmin(this ClaimsPrincipal principal) => principal.HasClaim("admin", "true");
}
