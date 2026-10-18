using NextGenCoaching.Core.Entities;

namespace NextGenCoaching.Core.Interfaces;

/// <summary>
/// Reads and writes SMTP configuration stored in the dedicated <c>email_settings</c> table.
/// The current value is cached in-process on a short TTL; saves invalidate the cache.
/// </summary>
public interface IEmailSettingsService
{
    Task<EmailSetting> GetAsync(CancellationToken ct = default);
    Task SaveAsync(EmailSetting settings, CancellationToken ct = default);
}
