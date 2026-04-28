namespace Infrastructure.Media;

public interface IMediaStore
{
    Task<string> UploadAsync(
        Stream content,
        string fileName,
        string? contentType = null,
        CancellationToken cancellationToken = default);
}