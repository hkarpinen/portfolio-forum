using Client.Authorization;
using Infrastructure.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Client.Controllers;

[ApiController]
[Route("api/forum/media")]
[EnableRateLimiting("standard")]
public sealed class MediaController : ControllerBase
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif",
    };
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IMediaStore _mediaStore;

    public MediaController(IMediaStore mediaStore)
    {
        _mediaStore = mediaStore;
    }

    [HttpPost("image")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> UploadImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        if (file.Length > MaxBytes)
            return BadRequest(new { error = "File exceeds the 5 MB limit." });

        if (!AllowedTypes.Contains(file.ContentType))
            return BadRequest(new { error = "Unsupported image type. Use JPEG, PNG, WebP, or GIF." });

        await using var stream = file.OpenReadStream();
        var url = await _mediaStore.UploadAsync(stream, file.FileName, file.ContentType, cancellationToken);

        return Ok(new { url });
    }
}
