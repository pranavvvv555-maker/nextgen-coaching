using System.Security.Cryptography;
using System.Text;
using System.Net;
using Microsoft.AspNetCore.DataProtection;

namespace NextGenCoaching.Api.Validation;

public static class ProductionConfiguration
{
    public static void ValidateDeployment(this WebApplicationBuilder builder)
    {
        var cfg = builder.Configuration;
        var keys = cfg["DataProtection:KeyPath"];
        if (!string.IsNullOrWhiteSpace(keys)) builder.Services.AddDataProtection().SetApplicationName("NextGen.Api").PersistKeysToFileSystem(Directory.CreateDirectory(keys));
        if (builder.Environment.IsDevelopment()) return;
        static bool Unsafe(string? value) => string.IsNullOrWhiteSpace(value) || value.Contains("ChangeMe", StringComparison.OrdinalIgnoreCase) || value.Contains("CHANGE_", StringComparison.OrdinalIgnoreCase) || value.Contains("REPLACE", StringComparison.OrdinalIgnoreCase) || value.Contains("development", StringComparison.OrdinalIgnoreCase);
        if (Unsafe(cfg["Jwt:Secret"]) || Encoding.UTF8.GetByteCount(cfg["Jwt:Secret"]!) < 32) throw new InvalidOperationException("Configure a strong production Jwt__Secret (at least 32 bytes).");
        if (Unsafe(cfg["Seed:AdminPassword"]) || cfg["Seed:AdminPassword"]!.Length < 12) throw new InvalidOperationException("Configure a unique Seed__AdminPassword of at least 12 characters.");
        if (cfg.GetValue<bool>("Seed:IncludeDemoData")) throw new InvalidOperationException("Demo accounts must be disabled outside Development.");
        if (string.IsNullOrWhiteSpace(cfg.GetConnectionString("DefaultConnection")) || cfg.GetConnectionString("DefaultConnection")!.Contains("nextgen_dev_password")) throw new InvalidOperationException("Configure the production database connection.");
        if (string.IsNullOrWhiteSpace(keys)) throw new InvalidOperationException("Configure DataProtection__KeyPath on persistent storage.");
        if (Unsafe(cfg["Api:ProxyKey"]) || cfg["Api:ProxyKey"]!.Length < 32) throw new InvalidOperationException("Configure a shared Api__ProxyKey of at least 32 characters.");
    }

    public static string ClientPartition(HttpContext context, IConfiguration cfg)
    {
        var configured = cfg["Api:ProxyKey"];
        var supplied = context.Request.Headers["X-NextGen-Proxy-Key"].ToString();
        if (!string.IsNullOrEmpty(configured) && supplied.Length <= 256 && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(configured), Encoding.UTF8.GetBytes(supplied))
            && IPAddress.TryParse(context.Request.Headers["X-NextGen-Client-IP"], out var address)) return address.ToString();
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
