using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace NextGenCoaching.Web.Services;

// Hash once per file revision, rather than reading the entire stylesheet on every page request.
public sealed class AssetVersions(IWebHostEnvironment environment, IMemoryCache cache)
{
    public string Url(string path)
    {
        var relative = path.TrimStart('/');
        var version = cache.GetOrCreate($"asset-version:{relative}", entry =>
        {
            entry.AddExpirationToken(environment.WebRootFileProvider.Watch(relative));
            using var stream = environment.WebRootFileProvider.GetFileInfo(relative).CreateReadStream();
            return Convert.ToHexString(SHA256.HashData(stream))[..16];
        });
        return $"/{relative}?v={version}";
    }
}
