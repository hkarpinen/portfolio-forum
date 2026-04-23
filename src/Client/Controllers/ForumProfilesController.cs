using Client.Authorization;
using Client.Contracts;
using Client.Extensions;
using Forum.Application.Contracts;
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

    public ForumProfilesController(IForumProfileManager profileManager, IForumProfileQuery profileQuery)
    {
        _profileManager = profileManager;
        _profileQuery = profileQuery;
    }

    [HttpGet("me")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var result = await _profileQuery.GetAsync(new GetForumProfileRequest(userId), cancellationToken);
        return Ok(result ?? new ForumProfileResponse(userId, null, null, DateTime.UtcNow, null));
    }

    [HttpGet("{userId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByUserId([FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        var result = await _profileQuery.GetAsync(new GetForumProfileRequest(userId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("me")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    public async Task<IActionResult> UpdateMine([FromBody] UpdateForumProfileDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var result = await _profileManager.UpsertAsync(
            new UpdateForumProfileRequest(userId, request.Bio, request.Signature),
            cancellationToken);
        return Ok(result);
    }
}
