using Forum.Application.Contracts;
using Forum.Application.Queries;
using Client.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Client.Controllers;

[ApiController]
[Route("api/forum/search")]
[EnableRateLimiting("search")]
public sealed class SearchController : ControllerBase
{
    private readonly ISearchQuery _searchQuery;

    public SearchController(ISearchQuery searchQuery)
    {
        _searchQuery = searchQuery;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Query([FromQuery] SearchQueryDto request, CancellationToken cancellationToken)
    {
        var result = await _searchQuery.QueryAsync(
            new SearchQueryRequest(request.Query, request.Scope, request.Sort, request.Page, request.PageSize),
            cancellationToken);

        return Ok(result);
    }
}
