using Forum.Application.Commands;
using Forum.Application.Managers;
using Forum.Application.Queries;
using Client.Authorization;
using Client.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Client.Controllers;

[ApiController]
[Route("api/forum/threads")]
public sealed class ThreadsController : ControllerBase
{
    private readonly IThreadWorkflowManager _threadWorkflowManager;
    private readonly IThreadQuery _threadQuery;
    private readonly ICommunityQuery _communityQuery;

    public ThreadsController(
        IThreadWorkflowManager threadWorkflowManager,
        IThreadQuery threadQuery,
        ICommunityQuery communityQuery)
    {
        _threadWorkflowManager = threadWorkflowManager;
        _threadQuery = threadQuery;
        _communityQuery = communityQuery;
    }

    [HttpPost]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    public async Task<IActionResult> Create([FromBody] CreateThreadCommand request,
        CancellationToken cancellationToken)
    {
        var community = await _communityQuery.GetBySlugAsync(
            new CommunityBySlugCommand(request.CommunitySlug), cancellationToken);
        if (community is null)
            return Problem(detail: "Community not found.", statusCode: StatusCodes.Status404NotFound);

        var created = await _threadWorkflowManager.CreateAsync(
            request with { CommunityId = community.CommunityId, AuthorId = User.GetRequiredUserId() },
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.ThreadId }, created);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List([FromQuery] Guid communityId, [FromQuery] string sort = "new",
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (communityId == Guid.Empty)
            return Problem(detail: "Query parameter 'communityId' is required.", statusCode: StatusCodes.Status400BadRequest);

        var result = await _threadQuery.ListAsync(new ListThreadsCommand(communityId, sort, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("feed")]
    [AllowAnonymous]
    public async Task<IActionResult> Feed([FromQuery] string sort = "new", [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _threadQuery.ListFeedAsync(new FeedCommand(sort, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _threadQuery.GetDetailAsync(new ThreadDetailCommand(id, User.GetUserIdOrNull()), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    public async Task<IActionResult> Edit([FromRoute] Guid id, [FromBody] EditThreadCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _threadWorkflowManager.EditAsync(request with { ThreadId = id }, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _threadWorkflowManager.DeleteAsync(
                new DeleteThreadCommand(id, User.GetRequiredUserId()), cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{id:guid}/lock")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    public async Task<IActionResult> Lock([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _threadWorkflowManager.LockAsync(
                new LockThreadCommand(id, User.GetRequiredUserId()), cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{id:guid}/pin")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    public async Task<IActionResult> Pin([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _threadWorkflowManager.PinAsync(
                new PinThreadCommand(id, User.GetRequiredUserId()), cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("/api/forum/search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] SearchQueryCommand request, CancellationToken cancellationToken)
    {
        var result = await _threadQuery.SearchAsync(request, cancellationToken);
        return Ok(result);
    }
}
