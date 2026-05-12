using Client.Authorization;
using Client.Extensions;
using Forum.Application.Commands;
using Forum.Application.Managers;
using Forum.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Client.Controllers;

[ApiController]
[Route("api/forum/moderation")]
[EnableRateLimiting("standard")]
[Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
public sealed class ModerationController : ControllerBase
{
    private readonly IModerationManager _moderationManager;
    private readonly IModerationQuery _moderationQuery;

    public ModerationController(IModerationManager moderationManager, IModerationQuery moderationQuery)
    {
        _moderationManager = moderationManager;
        _moderationQuery = moderationQuery;
    }

    [HttpPost("bans")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Ban([FromBody] BanUserCommand request, CancellationToken cancellationToken)
    {
        var result = await _moderationManager.BanAsync(
            request with { PerformedByUserId = User.GetRequiredUserId() },
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("bans/{banId:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Unban([FromRoute] Guid banId, CancellationToken cancellationToken)
    {
        var result = await _moderationManager.UnbanAsync(
            new UnbanUserCommand(banId, User.GetRequiredUserId()),
            cancellationToken);

        return result is null ? NotFound() : NoContent();
    }

    [HttpPost("logs")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> LogAction([FromBody] LogModerationActionCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _moderationManager.LogAsync(
            request with { PerformedByUserId = User.GetRequiredUserId() },
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("queue")]
    public async Task<IActionResult> Queue(
        [FromQuery] Guid communityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _moderationQuery.QueueAsync(
            new ModerationQueueCommand(communityId, page, pageSize),
            cancellationToken);

        return Ok(result);
    }
}
