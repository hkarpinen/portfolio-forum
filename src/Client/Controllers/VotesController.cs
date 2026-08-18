using Forum.Application.Commands;
using Forum.Application.Managers;
using Client.Authorization;
using Client.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Client.Controllers;

[ApiController]
[Route("api/forum/votes")]
public sealed class VotesController : ControllerBase
{
    private readonly IVoteManager _voteManager;

    public VotesController(IVoteManager voteManager)
    {
        _voteManager = voteManager;
    }

    [HttpPost]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    public async Task<IActionResult> Cast([FromBody] CastVoteCommand request, CancellationToken cancellationToken)
    {
        var result = await _voteManager.CastAsync(
            request with { UserId = User.GetRequiredUserId() },
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{voteId:guid}")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    public async Task<IActionResult> Switch([FromRoute] Guid voteId, [FromBody] SwitchVoteCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _voteManager.SwitchAsync(request with { VoteId = voteId }, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{voteId:guid}")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    public async Task<IActionResult> Retract([FromRoute] Guid voteId, CancellationToken cancellationToken)
    {
        var result = await _voteManager.RetractAsync(new RetractVoteCommand(voteId), cancellationToken);
        return result is null ? NotFound() : NoContent();
    }
}
