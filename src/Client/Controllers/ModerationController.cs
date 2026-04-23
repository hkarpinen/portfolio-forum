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
[Route("api/forum/moderation")]
[EnableRateLimiting("standard")]
[Authorize(Policy = ForumAuthorizationPolicies.ModeratorOrAdmin)]
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
    public async Task<IActionResult> Ban([FromBody] BanUserDto request, CancellationToken cancellationToken)
    {
        var result = await _moderationManager.BanAsync(
            new BanUserRequest(request.CommunityId, request.UserId, User.GetRequiredUserId(), request.Reason),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("bans/{banId:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Unban([FromRoute] Guid banId, CancellationToken cancellationToken)
    {
        var result = await _moderationManager.UnbanAsync(
            new UnbanUserRequest(banId, User.GetRequiredUserId()),
            cancellationToken);

        return result is null ? NotFound() : NoContent();
    }

    [HttpPost("logs")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> LogAction([FromBody] LogModerationActionDto request, CancellationToken cancellationToken)
    {
        var result = await _moderationManager.LogAsync(
            new LogModerationActionRequest(
                request.CommunityId,
                request.Action,
                User.GetRequiredUserId(),
                request.TargetUserId,
                request.TargetContent),
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
            new ModerationQueueRequest(communityId, page, pageSize),
            cancellationToken);

        return Ok(result);
    }
}

public sealed record BanUserDto(Guid CommunityId, Guid UserId, string? Reason);

public sealed record LogModerationActionDto(
    Guid CommunityId,
    Forum.Domain.ValueObjects.ModerationAction Action,
    Guid? TargetUserId,
    string? TargetContent);
