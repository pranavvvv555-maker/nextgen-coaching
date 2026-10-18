using Microsoft.EntityFrameworkCore;
using NextGenCoaching.Core.Entities;
using NextGenCoaching.Core.Interfaces;
using NextGenCoaching.Core.Persistence;

namespace NextGenCoaching.Core.Services.Settings;

public sealed class EmailSettingsService : IEmailSettingsService
{
    private readonly AppDbContext _db;
    private readonly EmailSettingsCache _cache;

    public EmailSettingsService(AppDbContext db, EmailSettingsCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<EmailSetting> GetAsync(CancellationToken ct = default)
    {
        if (_cache.TryGet(out var cached))
            return cached;

        var row = await _db.EmailSettings.AsNoTracking()
                      .FirstOrDefaultAsync(s => s.Id == EmailSetting.SingletonId, ct)
                  ?? new EmailSetting();

        _cache.Set(row);
        return row;
    }

    public async Task SaveAsync(EmailSetting settings, CancellationToken ct = default)
    {
        var row = await _db.EmailSettings.FirstOrDefaultAsync(s => s.Id == EmailSetting.SingletonId, ct);

        if (row is null)
        {
            settings.Id = EmailSetting.SingletonId;
            settings.UpdatedAtUtc = DateTime.UtcNow;
            _db.EmailSettings.Add(settings);
        }
        else
        {
            row.Enabled = settings.Enabled;
            row.SmtpHost = settings.SmtpHost;
            row.SmtpPort = settings.SmtpPort;
            row.FromEmail = settings.FromEmail;
            row.FromName = settings.FromName;
            row.SmtpUser = settings.SmtpUser;
            row.SmtpPassword = settings.SmtpPassword;
            row.UseStartTls = settings.UseStartTls;
            row.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        _cache.Invalidate();
    }
}
