using Microsoft.Extensions.Configuration;

namespace Infrastructure.Media;

internal sealed class LocalFileSystemMediaStore : IMediaStore
{
    private readonly string _basePath;
    private readonly string _publicBaseUrl;

    public LocalFileSystemMediaStore(IConfiguration configuration)
    {
        _basePath = configuration["Media:RootPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "uploads", "forum");

        // Set per environment: stored URLs are absolute, so this is what lets them survive
        // a move to object storage without rewriting rows.
        _publicBaseUrl = (configuration["Media:PublicBaseUrl"] ?? "/uploads/forum").TrimEnd('/');

        Directory.CreateDirectory(_basePath);
    }

    private static readonly Dictionary<string, string> MimeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"]  = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"]  = ".gif",
    };

    public async Task<string> UploadAsync(
        Stream content,
        string fileName,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        // Use the validated MIME type to derive the extension so an attacker cannot
        // upload an .html (or any other non-image) file by setting a benign Content-Type.
        var extension = contentType is not null && MimeToExtension.TryGetValue(contentType, out var ext)
            ? ext
            : ".bin";

        var safeName = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "-", StringComparison.Ordinal)
            .ToLowerInvariant();

        var storedName = $"{safeName}-{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_basePath, storedName);

        await using var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fileStream, cancellationToken);

        _ = contentType; // nothing validates content-type yet
        return $"{_publicBaseUrl}/{storedName}";
    }
}