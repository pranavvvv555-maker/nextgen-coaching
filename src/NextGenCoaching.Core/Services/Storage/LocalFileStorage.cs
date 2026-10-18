using Microsoft.Extensions.Configuration;
using NextGenCoaching.Core.Interfaces;

namespace NextGenCoaching.Core.Services.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;
    private readonly string _publicBaseUrl;

    public LocalFileStorage(IConfiguration config)
    {
        var section = config.GetSection("Storage");
        _rootPath = section["LocalRoot"] ?? Path.Combine(AppContext.BaseDirectory, "uploads");
        _publicBaseUrl = section["PublicBaseUrl"]?.TrimEnd('/') ?? "/uploads";
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var safeName = $"{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(_rootPath, safeName);
        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);
        return $"{_publicBaseUrl}/{safeName}";
    }

    public Task<Stream> DownloadAsync(string url, CancellationToken ct = default)
    {
        var fileName = url.Replace(_publicBaseUrl, string.Empty).TrimStart('/');
        return Task.FromResult<Stream>(File.OpenRead(Path.Combine(_rootPath, fileName)));
    }

    public Task DeleteAsync(string url, CancellationToken ct = default)
    {
        var fileName = url.Replace(_publicBaseUrl, string.Empty).TrimStart('/');
        var fullPath = Path.Combine(_rootPath, fileName);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
