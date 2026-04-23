using Forum.Application.Managers;
using Client.Authorization;
using Client.Contracts;
using Client.Extensions;
using Forum.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Client.Controllers;

[ApiController]
[Route("api/forum/votes")]
[EnableRateLimiting("standard")]
public sealed class VotesController : ControllerBase
{
    private readonly IVoteManager _voteManager;

    public VotesController(IVoteManager voteManager)
    {
        _voteManager = voteManager;
    }

    [HttpPost]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Cast([FromBody] CastVoteDto request, CancellationToken cancellationToken)
    {
        var result = await _voteManager.CastAsync(
            new CastVoteRequest(request.TargetType, request.TargetId, User.GetRequiredUserId(), request.Direction),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{voteId:guid}")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Switch([FromRoute] Guid voteId, [FromBody] SwitchVoteDto request, CancellationToken cancellationToken)
    {
        var result = await _voteManager.SwitchAsync(new SwitchVoteRequest(voteId, request.Direction), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{voteId:guid}")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> Retract([FromRoute] Guid voteId, CancellationToken cancellationToken)
    {
        var result = await _voteManager.RetractAsync(new RetractVoteRequest(voteId), cancellationToken);
        return result is null ? NotFound() : NoContent();
    }
}
