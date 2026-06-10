using Client.Authorization;
using Client.Extensions;
using Forum.Application.Commands;
using Forum.Application.Managers;
using Forum.Application.Queries;
using Forum.Domain.ValueObjects;
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

    /// <summary>Per-community mod-queue endpoint matching the frontend's URL shape.</summary>
    [HttpGet("/api/forum/communities/{slug}/mod-queue")]
    public async Task<IActionResult> QueueBySlug(
        [FromRoute] string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _moderationQuery.QueueBySlugAsync(slug, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns reverse-chronological mod-log entries for the named community.</summary>
    [HttpGet("/api/forum/communities/{slug}/mod-log")]
    public async Task<IActionResult> ModLog(
        [FromRoute] string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _moderationQuery.ListLogAsync(slug, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>Report a thread for moderator review.</summary>
    [HttpPost("/api/forum/threads/{threadId:guid}/report")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> ReportThread(
        [FromRoute] Guid threadId,
        [FromBody] ReportContentRequest body,
        CancellationToken cancellationToken)
    {
        var info = await _moderationQuery.GetThreadCommunityIdAsync(threadId, cancellationToken);
        if (info is null)
            return NotFound();

        var reportId = await _moderationManager.ReportContentAsync(
            new ReportContentCommand(
                info.Value.CommunityId,
                ReportTargetType.Thread,
                threadId,
                body.Reason,
                body.Details,
                User.GetRequiredUserId()),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new { reportId });
    }

    /// <summary>Report a comment for moderator review.</summary>
    [HttpPost("/api/forum/comments/{commentId:guid}/report")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> ReportComment(
        [FromRoute] Guid commentId,
        [FromBody] ReportContentRequest body,
        CancellationToken cancellationToken)
    {
        var communityId = await _moderationQuery.GetCommentCommunityIdAsync(commentId, cancellationToken);
        if (communityId is null)
            return NotFound();

        var reportId = await _moderationManager.ReportContentAsync(
            new ReportContentCommand(
                communityId.Value,
                ReportTargetType.Comment,
                commentId,
                body.Reason,
                body.Details,
                User.GetRequiredUserId()),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new { reportId });
    }

    /// <summary>Approve a report (no content removal — acknowledge and close).</summary>
    [HttpPost("/api/forum/communities/{slug}/mod-queue/{reportId:guid}/approve")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> ApproveReport(
        [FromRoute] string slug,
        [FromRoute] Guid reportId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _moderationManager.ApproveReportAsync(
                new ApproveReportCommand(reportId, User.GetRequiredUserId()),
                cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>Remove reported content and close the report.</summary>
    [HttpPost("/api/forum/communities/{slug}/mod-queue/{reportId:guid}/remove")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> RemoveContent(
        [FromRoute] string slug,
        [FromRoute] Guid reportId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _moderationManager.RemoveContentAsync(
                new RemoveContentCommand(reportId, User.GetRequiredUserId()),
                cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>Dismiss a report as unfounded.</summary>
    [HttpPost("/api/forum/communities/{slug}/mod-queue/{reportId:guid}/dismiss")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> DismissReport(
        [FromRoute] string slug,
        [FromRoute] Guid reportId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _moderationManager.DismissReportAsync(
                new DismissReportCommand(reportId, User.GetRequiredUserId()),
                cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}

public sealed record ReportContentRequest(string Reason, string? Details);
