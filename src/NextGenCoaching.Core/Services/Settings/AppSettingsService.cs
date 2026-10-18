using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Entities;
using NextGenCoaching.Core.Interfaces;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Core.Settings;

namespace NextGenCoaching.Core.Services.Settings;

public sealed class AppSettingsService : IAppSettingsService
{
    private readonly AppDbContext _db;
    private readonly AppSettingsCache _cache;

    public AppSettingsService(AppDbContext db, AppSettingsCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyDictionary<string, string?>> GetAllAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetSnapshot(out var cached))
            return cached;

        var rows = await _db.AppSettings.AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase, ct);

        _cache.Set(rows);
        return rows;
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.TryGetValue(key, out var value) ? value : null;
    }

    public async Task<EmailSettings> GetEmailSettingsAsync(CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        string? Get(string key) => all.TryGetValue(key, out var v) ? v : null;

        var s = new EmailSettings
        {
            Enabled     = bool.TryParse(Get(EmailSettings.Keys.Enabled), out var enabled) && enabled,
            SmtpHost    = Get(EmailSettings.Keys.SmtpHost) is { Length: > 0 } host ? host : "localhost",
            FromEmail   = Get(EmailSettings.Keys.FromEmail) is { Length: > 0 } from ? from : "noreply@nextgencoaching.local",
            FromName    = Get(EmailSettings.Keys.FromName) is { Length: > 0 } name ? name : "NextGen Coaching",
            SmtpUser    = Get(EmailSettings.Keys.SmtpUser),
            SmtpPassword = Get(EmailSettings.Keys.SmtpPassword)
        };

        if (int.TryParse(Get(EmailSettings.Keys.SmtpPort), out var port))
            s.SmtpPort = port;

        var tls = Get(EmailSettings.Keys.UseStartTls);
        if (tls is not null)
            s.UseStartTls = bool.TryParse(tls, out var useTls) && useTls;

        return s;
    }

    public async Task SaveEmailSettingsAsync(EmailSettings settings, CancellationToken ct = default)
    {
        var map = new (string Key, string? Value, bool Secret)[]
        {
            (EmailSettings.Keys.Enabled,      settings.Enabled.ToString(),     false),
            (EmailSettings.Keys.SmtpHost,     settings.SmtpHost,               false),
            (EmailSettings.Keys.SmtpPort,     settings.SmtpPort.ToString(),    false),
            (EmailSettings.Keys.FromEmail,    settings.FromEmail,              false),
            (EmailSettings.Keys.FromName,     settings.FromName,               false),
            (EmailSettings.Keys.SmtpUser,     settings.SmtpUser,               false),
            (EmailSettings.Keys.SmtpPassword, settings.SmtpPassword,           true),
            (EmailSettings.Keys.UseStartTls,  settings.UseStartTls.ToString(), false),
        };

        var keys = map.Select(m => m.Key).ToList();
        var existing = await _db.AppSettings
            .Where(s => keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, ct);

        foreach (var (key, value, secret) in map)
        {
            if (existing.TryGetValue(key, out var row))
            {
                row.Value = value;
                row.IsSecret = secret;
            }
            else
            {
                _db.AppSettings.Add(new AppSetting { Key = key, Value = value, IsSecret = secret });
            }
        }

        await _db.SaveChangesAsync(ct);
        _cache.Invalidate();
    }
}
