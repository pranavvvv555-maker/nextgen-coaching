using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

namespace NextGenCoaching.Web.Services;

public static class HostingSetup
{
    public static void AddDeploymentServices(this WebApplicationBuilder builder)
    {
        var keys = builder.Configuration["DataProtection:KeyPath"];
        if (!string.IsNullOrWhiteSpace(keys))
            builder.Services.AddDataProtection().SetApplicationName("NextGen.Web")
                .PersistKeysToFileSystem(Directory.CreateDirectory(keys));
        builder.Services.AddHttpContextAccessor();
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var value in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
            {
                if (!IPAddress.TryParse(value, out var address)) throw new InvalidOperationException("A trusted proxy address is invalid.");
                options.KnownProxies.Add(address);
            }
        });
    }
}
