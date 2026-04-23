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
[Route("api/forum/comments")]
[EnableRateLimiting("standard")]
public sealed class CommentsController : ControllerBase
{
    private readonly ICommentWorkflowManager _commentWorkflowManager;
    private readonly ICommentQuery _commentQuery;

    public CommentsController(ICommentWorkflowManager commentWorkflowManager, ICommentQuery commentQuery)
    {
        _commentWorkflowManager = commentWorkflowManager;
        _commentQuery = commentQuery;
    }

    [HttpPost]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Create([FromBody] CreateCommentDto request, CancellationToken cancellationToken)
    {
        var result = await _commentWorkflowManager.CreateAsync(
            new CreateCommentRequest(request.ThreadId, User.GetRequiredUserId(), request.Content, request.ParentCommentId),
            cancellationToken);

        return CreatedAtAction(nameof(ListTree), new { threadId = request.ThreadId }, result);
    }

    [HttpPut("{commentId:guid}")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Edit([FromRoute] Guid commentId, [FromBody] EditCommentDto request, CancellationToken cancellationToken)
    {
        var result = await _commentWorkflowManager.EditAsync(new EditCommentRequest(commentId, request.Content), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{commentId:guid}")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Delete([FromRoute] Guid commentId, CancellationToken cancellationToken)
    {
        var result = await _commentWorkflowManager.DeleteAsync(new DeleteCommentRequest(commentId), cancellationToken);
        return result is null ? NotFound() : NoContent();
    }

    [HttpGet("thread/{threadId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> ListTree([FromRoute] Guid threadId, CancellationToken cancellationToken)
    {
        var result = await _commentQuery.ListTreeAsync(new ListCommentTreeRequest(threadId), cancellationToken);
        return Ok(result);
    }
}
