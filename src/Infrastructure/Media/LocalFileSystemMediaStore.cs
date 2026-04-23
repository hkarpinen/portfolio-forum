using Microsoft.Extensions.Configuration;

namespace Infrastructure.Media;

internal sealed class LocalFileSystemMediaStore : IMediaStore
{
    private readonly string _basePath;

    public LocalFileSystemMediaStore(IConfiguration configuration)
    {
        _basePath = configuration["Media:RootPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "uploads", "forum");

        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(
        Stream content,
        string fileName,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        var safeName = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "-", StringComparison.Ordinal)
            .ToLowerInvariant();

        var extension = Path.GetExtension(fileName);
        var storedName = $"{safeName}-{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_basePath, storedName);

        await using var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fileStream, cancellationToken);

        _ = contentType; // reserved for future content-type validation
        return storedName;
    }
}