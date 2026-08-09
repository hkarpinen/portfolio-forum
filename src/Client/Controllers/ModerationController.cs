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
    private readonly ICommunityQuery _communityQuery;
    private readonly IMembershipQuery _membershipQuery;

    public ModerationController(
        IModerationManager moderationManager,
        IModerationQuery moderationQuery,
        ICommunityQuery communityQuery,
        IMembershipQuery membershipQuery)
    {
        _moderationManager = moderationManager;
        _moderationQuery = moderationQuery;
        _communityQuery = communityQuery;
        _membershipQuery = membershipQuery;
    }

    /// <summary>
    /// True when the caller moderates THIS community.
    ///
    /// The class-level policy only establishes "a forum user in good standing", which is every
    /// signed-in account — not nearly enough for a community's moderation surface. Without this
    /// check anyone could read any community's report queue and moderation history, including who
    /// removed what and which members were targeted.
    /// </summary>
    private async Task<bool> ModeratesAsync(string slug, CancellationToken cancellationToken)
    {
        if (User.IsAdmin()) return true;

        var community = await _communityQuery.GetBySlugAsync(new CommunityBySlugCommand(slug), cancellationToken);
        if (community is null) return false;

        var (_, role) = await _membershipQuery.GetMembershipAsync(
            community.CommunityId, User.GetRequiredUserId(), cancellationToken);

        return string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "Moderator", StringComparison.OrdinalIgnoreCase);
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

    [HttpGet("/api/forum/communities/{slug}/mod-queue")]
    public async Task<IActionResult> QueueBySlug(
        [FromRoute] string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // NotFound rather than Forbid: a stranger should not be able to tell a community's queue
        // apart from one that does not exist.
        if (!await ModeratesAsync(slug, cancellationToken)) return NotFound();

        var result = await _moderationQuery.QueueBySlugAsync(slug, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("/api/forum/communities/{slug}/mod-log")]
    public async Task<IActionResult> ModLog(
        [FromRoute] string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!await ModeratesAsync(slug, cancellationToken)) return NotFound();

        var result = await _moderationQuery.ListLogAsync(slug, page, pageSize, cancellationToken);
        return Ok(result);
    }

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
