using NextGenCoaching.Core.Settings;

namespace NextGenCoaching.Core.Interfaces;

/// <summary>
/// Reads and writes runtime configuration stored in the <c>app_settings</c> table.
/// Values are cached in-process on a short TTL; writes invalidate the cache so changes
/// take effect quickly without an app restart.
/// </summary>
public interface IAppSettingsService
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, string?>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Typed accessor for SMTP configuration.</summary>
    Task<EmailSettings> GetEmailSettingsAsync(CancellationToken ct = default);

    /// <summary>Persists SMTP configuration (password row flagged secret) and invalidates the cache.</summary>
    Task SaveEmailSettingsAsync(EmailSettings settings, CancellationToken ct = default);
}
