using Forum.Application.Managers;
using Forum.Application.Queries;
using Client.Authorization;
using Client.Contracts;
using Client.Extensions;
using Forum.Application.Contracts;
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
    public async Task<IActionResult> Create([FromBody] CreateCommunityDto request, CancellationToken cancellationToken)
    {
        var created = await _communityWorkflowManager.CreateAsync(
            new CreateCommunityRequest(request.Name, request.Visibility, User.GetRequiredUserId(), request.Description, request.ImageUrl),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { communityId = created.CommunityId }, created);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _communityQuery.ListAsync(new ListCommunitiesRequest(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{communityId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById([FromRoute] Guid communityId, CancellationToken cancellationToken)
    {
        var result = await _communityQuery.GetDetailAsync(new CommunityDetailRequest(communityId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("by-name/{name}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByName([FromRoute] string name, CancellationToken cancellationToken)
    {
        var result = await _communityQuery.GetByNameAsync(new CommunityByNameRequest(name), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{communityId:guid}")]
    [Authorize(Policy = ForumAuthorizationPolicies.ModeratorOrAdmin)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Update([FromRoute] Guid communityId, [FromBody] UpdateCommunityDto request, CancellationToken cancellationToken)
    {
        var result = await _communityWorkflowManager.UpdateAsync(
            new UpdateCommunityRequest(communityId, request.Name, request.Visibility, request.Description, request.ImageUrl),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{communityId:guid}/membership")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    public async Task<IActionResult> GetMembership([FromRoute] Guid communityId, CancellationToken cancellationToken)
    {
        var isMember = await _membershipQuery.IsMemberAsync(communityId, User.GetRequiredUserId(), cancellationToken);
        return Ok(new { isMember });
    }

    [HttpPost("{communityId:guid}/join")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Join([FromRoute] Guid communityId, CancellationToken cancellationToken)
    {
        var result = await _membershipManager.JoinAsync(
            new JoinCommunityRequest(communityId, User.GetRequiredUserId()),
            cancellationToken);

        return CreatedAtAction(nameof(GetMembership), new { communityId }, result);
    }

    [HttpPost("{communityId:guid}/ownership")]
    [Authorize(Policy = ForumAuthorizationPolicies.AdminOnly)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> TransferOwnership([FromRoute] Guid communityId, [FromBody] TransferOwnershipDto request, CancellationToken cancellationToken)
    {
        var result = await _communityWorkflowManager.TransferOwnershipAsync(
            new TransferCommunityOwnershipRequest(communityId, request.NewOwnerId),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }
}
