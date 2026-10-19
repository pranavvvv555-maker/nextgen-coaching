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
        _rootPath = string.IsNullOrWhiteSpace(section["LocalRoot"]) ? Path.Combine(AppContext.BaseDirectory, "uploads") : section["LocalRoot"]!;
        _publicBaseUrl = string.IsNullOrWhiteSpace(section["PublicBaseUrl"]) ? "/uploads" : section["PublicBaseUrl"]!.TrimEnd('/');
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var original = Path.GetFileName(fileName.Replace('\\', '/'));
        original = System.Text.RegularExpressions.Regex.Replace(original, @"[^\p{L}\p{N}._-]", "_");
        var extension = Path.GetExtension(original);
        var safeName = $"{Guid.NewGuid():N}{(extension.Length <= 12 ? extension : string.Empty)}";
        var fullPath = Path.Combine(_rootPath, safeName);
        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);
        return $"{_publicBaseUrl}/{safeName}";
    }

    public Task<Stream> DownloadAsync(string url, CancellationToken ct = default)
    {
        return Task.FromResult<Stream>(File.OpenRead(ResolvePath(url)));
    }

    public Task DeleteAsync(string url, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(url);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    private string ResolvePath(string url)
    {
        var prefix = _publicBaseUrl + "/";
        if (!url.StartsWith(prefix, StringComparison.Ordinal)) throw new ArgumentException("Invalid file reference.");
        var name = Uri.UnescapeDataString(url[prefix.Length..]);
        if (string.IsNullOrWhiteSpace(name) || name.Contains('/') || name.Contains('\\') || name.Contains(':') || name.Contains('\0') || name is "." or "..")
            throw new ArgumentException("Invalid file reference.");
        return Path.Combine(_rootPath, name);
    }
}
