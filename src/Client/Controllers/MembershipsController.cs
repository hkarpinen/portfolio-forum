using Client.Authorization;
using Client.Extensions;
using Forum.Application.Contracts;
using Forum.Application.Managers;
using Forum.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Client.Controllers;

[ApiController]
[Route("api/forum")]
[EnableRateLimiting("standard")]
[Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
public sealed class MembershipsController : ControllerBase
{
    private readonly IMembershipManager _membershipManager;
    private readonly IMembershipQuery _membershipQuery;

    public MembershipsController(IMembershipManager membershipManager, IMembershipQuery membershipQuery)
    {
        _membershipManager = membershipManager;
        _membershipQuery = membershipQuery;
    }

    /// <summary>
    /// List all active members of a community, with their roles.
    /// </summary>
    [HttpGet("communities/{communityId:guid}/members")]
    public async Task<IActionResult> ListMembers([FromRoute] Guid communityId, CancellationToken cancellationToken)
    {
        var members = await _membershipQuery.ListByCommunityAsync(communityId, cancellationToken);
        return Ok(members);
    }

    /// <summary>
    /// Appoint a member as moderator. Caller must be community owner or global admin.
    /// </summary>
    [HttpPost("memberships/{membershipId:guid}/moderator")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> AppointModerator([FromRoute] Guid membershipId, CancellationToken cancellationToken)
    {
        var result = await _membershipManager.AppointModeratorAsync(
            new AppointModeratorRequest(membershipId),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Remove moderator status from a member. Caller must be community owner or global admin.
    /// </summary>
    [HttpDelete("memberships/{membershipId:guid}/moderator")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> RemoveModerator([FromRoute] Guid membershipId, CancellationToken cancellationToken)
    {
        var result = await _membershipManager.RemoveModeratorAsync(
            new RemoveModeratorRequest(membershipId),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }
}
