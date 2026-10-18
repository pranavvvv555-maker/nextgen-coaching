namespace NextGenCoaching.Core.Interfaces;

public interface IFileStorage
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string url, CancellationToken ct = default);
    Task DeleteAsync(string url, CancellationToken ct = default);
}
