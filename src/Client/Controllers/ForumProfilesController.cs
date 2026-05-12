using Client.Authorization;
using Client.Extensions;
using Forum.Application.Commands;
using Forum.Application.Dtos;
using Forum.Application.Managers;
using Forum.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Client.Controllers;

[ApiController]
[Route("api/forum/profiles")]
[EnableRateLimiting("standard")]
public sealed class ForumProfilesController : ControllerBase
{
    private readonly IForumProfileManager _profileManager;
    private readonly IForumProfileQuery _profileQuery;
    private readonly IThreadQuery _threadQuery;
    private readonly ICommentQuery _commentQuery;
    private readonly IMembershipQuery _membershipQuery;

    public ForumProfilesController(
        IForumProfileManager profileManager,
        IForumProfileQuery profileQuery,
        IThreadQuery threadQuery,
        ICommentQuery commentQuery,
        IMembershipQuery membershipQuery)
    {
        _profileManager = profileManager;
        _profileQuery = profileQuery;
        _threadQuery = threadQuery;
        _commentQuery = commentQuery;
        _membershipQuery = membershipQuery;
    }

    [HttpGet("me")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var result = await _profileQuery.GetAsync(new GetForumProfileCommand(userId), cancellationToken);
        return Ok(result ?? new ForumProfileDto(userId, null, null, null, null, DateTime.UtcNow, null));
    }

    [HttpGet("{userId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByUserId([FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        var result = await _profileQuery.GetAsync(new GetForumProfileCommand(userId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{userId:guid}/threads")]
    [AllowAnonymous]
    public async Task<IActionResult> GetThreadsByUser(
        [FromRoute] Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _threadQuery.ListByAuthorAsync(userId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{userId:guid}/comments")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCommentsByUser(
        [FromRoute] Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _commentQuery.ListByAuthorAsync(userId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{userId:guid}/memberships")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMembershipsByUser(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken = default)
    {
        var result = await _membershipQuery.ListByUserAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("me")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    public async Task<IActionResult> UpdateMine([FromBody] UpdateForumProfileCommand request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var result = await _profileManager.UpsertAsync(
            request with { UserId = userId },
            cancellationToken);
        return Ok(result);
    }
}
