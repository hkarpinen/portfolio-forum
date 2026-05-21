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
    private const long MaxBytes = 5 * 1024 * 1024;

    // Magic byte signatures keyed by MIME type — guards against content-type spoofing
    private static readonly Dictionary<string, byte[][]> MagicBytes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = [new byte[] { 0xFF, 0xD8, 0xFF }],
        ["image/png"]  = [new byte[] { 0x89, 0x50, 0x4E, 0x47 }],
        ["image/gif"]  = [new byte[] { 0x47, 0x49, 0x46, 0x38 }],
        ["image/webp"] = [new byte[] { 0x52, 0x49, 0x46, 0x46 }],
    };

    private static bool HasValidMagicBytes(Stream stream, string contentType)
    {
        if (!MagicBytes.TryGetValue(contentType, out var signatures)) return false;
        Span<byte> header = stackalloc byte[8];
        var read = stream.Read(header);
        stream.Position = 0;
        var headerSlice = header[..read].ToArray();
        return signatures.Any(sig => headerSlice.AsSpan().StartsWith(sig));
    }

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

        if (!HasValidMagicBytes(stream, file.ContentType))
            return BadRequest(new { error = "File content does not match the declared type." });

        var url = await _mediaStore.UploadAsync(stream, file.FileName, file.ContentType, cancellationToken);

        return Ok(new { url });
    }
}
