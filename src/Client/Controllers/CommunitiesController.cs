using Forum.Application.Commands;
using Forum.Application.Dtos;
using Forum.Application.Managers;
using Forum.Application.Queries;
using Client.Authorization;
using Client.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Client.Controllers;

[ApiController]
[Route("api/forum/communities")]
[EnableRateLimiting("standard")]
public sealed class CommunitiesController : ControllerBase
{
    private readonly ICommunityWorkflowManager _communityWorkflowManager;
    private readonly IMembershipManager _membershipManager;
    private readonly ICommunityQuery _communityQuery;
    private readonly IMembershipQuery _membershipQuery;

    public CommunitiesController(
        ICommunityWorkflowManager communityWorkflowManager,
        IMembershipManager membershipManager,
        ICommunityQuery communityQuery,
        IMembershipQuery membershipQuery)
    {
        _communityWorkflowManager = communityWorkflowManager;
        _membershipManager = membershipManager;
        _communityQuery = communityQuery;
        _membershipQuery = membershipQuery;
    }

    [HttpPost]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Create([FromBody] CreateCommunityCommand request,
        CancellationToken cancellationToken)
    {
        var created = await _communityWorkflowManager.CreateAsync(
            request with { OwnerId = User.GetRequiredUserId() },
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { communityId = created.CommunityId }, created);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? membership = null,
        CancellationToken cancellationToken = default)
    {
        // `?membership=mine` filters to the caller's joined communities so the
        // "Your communities" sidebar fits in one round-trip. Anonymous users
        // with that filter get an empty list rather than 401 — the page is
        // still public-readable and a friendly empty state is fine.
        Guid? membershipUserId = null;
        if (string.Equals(membership, "mine", StringComparison.OrdinalIgnoreCase))
        {
            membershipUserId = User.GetUserIdOrNull();
            if (membershipUserId is null)
                return Ok(new CommunityListDto(Array.Empty<CommunityDto>(), 0));
        }

        var result = await _communityQuery.ListAsync(
            new ListCommunitiesCommand(page, pageSize, membershipUserId),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{communityId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById([FromRoute] Guid communityId, CancellationToken cancellationToken)
    {
        var result = await _communityQuery.GetDetailAsync(new CommunityDetailCommand(communityId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("by-slug/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySlug([FromRoute] string slug, CancellationToken cancellationToken)
    {
        var result = await _communityQuery.GetBySlugAsync(new CommunityBySlugCommand(slug), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{communityId:guid}")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Update([FromRoute] Guid communityId, [FromBody] UpdateCommunityCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _communityWorkflowManager.UpdateAsync(
                request with
                {
                    CommunityId = communityId,
                    RequestingUserId = User.GetRequiredUserId(),
                    RequestingUserIsAdmin = User.IsAdmin()
                },
                cancellationToken);

            return result is null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{communityId:guid}/membership")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    public async Task<IActionResult> GetMembership([FromRoute] Guid communityId, CancellationToken cancellationToken)
    {
        var (isMember, role) = await _membershipQuery.GetMembershipAsync(communityId, User.GetRequiredUserId(), cancellationToken);
        return Ok(new { isMember, role });
    }

    [HttpPost("{communityId:guid}/join")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Join([FromRoute] Guid communityId, CancellationToken cancellationToken)
    {
        var result = await _membershipManager.JoinAsync(
            new JoinCommunityCommand(communityId, User.GetRequiredUserId()),
            cancellationToken);

        return CreatedAtAction(nameof(GetMembership), new { communityId }, result);
    }

    [HttpPost("{communityId:guid}/ownership")]
    [Authorize(Policy = ForumAuthorizationPolicies.AdminOnly)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> TransferOwnership([FromRoute] Guid communityId,
        [FromBody] TransferCommunityOwnershipCommand request, CancellationToken cancellationToken)
    {
        var result = await _communityWorkflowManager.TransferOwnershipAsync(
            request with { CommunityId = communityId },
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{communityId:guid}")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Delete([FromRoute] Guid communityId, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _communityWorkflowManager.DeleteAsync(
                new DeleteCommunityCommand(communityId, User.GetRequiredUserId(), User.IsAdmin()),
                cancellationToken);

            return deleted ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{communityId:guid}/members")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    public async Task<IActionResult> ListMembers([FromRoute] Guid communityId, CancellationToken cancellationToken)
    {
        var members = await _membershipQuery.ListByCommunityAsync(communityId, cancellationToken);
        return Ok(members);
    }

    [HttpPost("/api/forum/memberships/{membershipId:guid}/moderator")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> AppointModerator([FromRoute] Guid membershipId, CancellationToken cancellationToken)
    {
        var result = await _membershipManager.AppointModeratorAsync(
            new AppointModeratorCommand(membershipId),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("/api/forum/memberships/{membershipId:guid}/moderator")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> RemoveModerator([FromRoute] Guid membershipId, CancellationToken cancellationToken)
    {
        var result = await _membershipManager.RemoveModeratorAsync(
            new RemoveModeratorCommand(membershipId),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }
}
