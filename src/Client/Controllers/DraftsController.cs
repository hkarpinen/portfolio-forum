using Client.Extensions;
using Forum.Application.Commands;
using Forum.Application.Managers;
using Forum.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Client.Controllers;

/// <summary>A separate URL surface over the SAME aggregate — a draft is a thread
/// in Draft status.</summary>
[ApiController]
[Route("api/forum/drafts")]
[Authorize]
[EnableRateLimiting("standard")]
public sealed class DraftsController : ControllerBase
{
    private readonly IThreadWorkflowManager _threads;
    private readonly IThreadQuery _query;
    private readonly ICommunityQuery _communities;

    public DraftsController(
        IThreadWorkflowManager threads,
        IThreadQuery query,
        ICommunityQuery communities)
    {
        _threads = threads;
        _query = query;
        _communities = communities;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await _query.ListDraftsByAuthorAsync(User.GetRequiredUserId(), ct));

    [HttpPost]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Begin([FromBody] BeginDraftCommand request, CancellationToken ct)
    {
        var community = await _communities.GetBySlugAsync(
            new CommunityBySlugCommand(request.CommunitySlug), ct);
        if (community is null)
            return Problem(detail: "Community not found.", statusCode: StatusCodes.Status404NotFound);

        var draft = await _threads.BeginDraftAsync(
            request with { CommunityId = community.CommunityId, AuthorId = User.GetRequiredUserId() }, ct);
        return CreatedAtAction(nameof(GetById), new { draftId = draft.ThreadId }, draft);
    }

    [HttpGet("{draftId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid draftId, CancellationToken ct)
    {
        var draft = await _query.GetDraftByIdAsync(User.GetRequiredUserId(), draftId, ct);
        return draft is null ? NotFound() : Ok(draft);
    }

    [HttpPut("{draftId:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Revise([FromRoute] Guid draftId, [FromBody] ReviseDraftCommand request, CancellationToken ct)
    {
        var result = await _threads.ReviseDraftAsync(
            request with { ThreadId = draftId, AuthorId = User.GetRequiredUserId() }, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{draftId:guid}/publish")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Publish([FromRoute] Guid draftId, CancellationToken ct)
    {
        var result = await _threads.PublishDraftAsync(
            new PublishDraftCommand(draftId, User.GetRequiredUserId()), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{draftId:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Abandon([FromRoute] Guid draftId, CancellationToken ct)
    {
        var ok = await _threads.AbandonDraftAsync(
            new AbandonDraftCommand(draftId, User.GetRequiredUserId()), ct);
        return ok ? NoContent() : NotFound();
    }
}
