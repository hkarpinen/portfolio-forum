namespace Infrastructure.Media;

internal interface IMediaStore
{
    Task<string> UploadAsync(
        Stream content,
        string fileName,
        string? contentType = null,
        CancellationToken cancellationToken = default);
}