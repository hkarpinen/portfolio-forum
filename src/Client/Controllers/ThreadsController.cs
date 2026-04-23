using System.Text.Json;
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
[Route("api/forum/threads")]
[EnableRateLimiting("standard")]
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
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Create([FromBody] CreateThreadDto request, CancellationToken cancellationToken)
    {
        var community = await _communityQuery.GetByNameAsync(
            new CommunityByNameRequest(request.CommunitySlug), cancellationToken);
        if (community is null)
            return NotFound(new { error = "Community not found." });

        var created = await _threadWorkflowManager.CreateAsync(
            new CreateThreadRequest(community.CommunityId, User.GetRequiredUserId(), request.Title, request.Content),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.ThreadId }, created);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List([FromQuery] Guid communityId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (communityId == Guid.Empty)
        {
            return BadRequest(new { error = "Query parameter 'communityId' is required." });
        }

        var result = await _threadQuery.ListAsync(new ListThreadsRequest(communityId, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _threadQuery.GetDetailAsync(new ThreadDetailRequest(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Edit([FromRoute] Guid id, [FromBody] EditThreadDto request, CancellationToken cancellationToken)
    {
        var result = await _threadWorkflowManager.EditAsync(new EditThreadRequest(id, request.Title, request.Content), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = ForumAuthorizationPolicies.ModeratorOrAdmin)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _threadWorkflowManager.DeleteAsync(new DeleteThreadRequest(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/lock")]
    [Authorize(Policy = ForumAuthorizationPolicies.ModeratorOrAdmin)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Lock([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _threadWorkflowManager.LockAsync(new LockThreadRequest(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/pin")]
    [Authorize(Policy = ForumAuthorizationPolicies.ModeratorOrAdmin)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Pin([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _threadWorkflowManager.PinAsync(new PinThreadRequest(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/events")]
    [AllowAnonymous]
    [EnableRateLimiting("sse")]
    public async Task StreamThreadEvents([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var payload = JsonSerializer.Serialize(new
                {
                    threadId = id,
                    eventType = "thread.ping",
                    occurredAt = DateTime.UtcNow
                });

                await Response.WriteAsync("event: thread.ping\n", cancellationToken);
                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected.
        }
    }
}
