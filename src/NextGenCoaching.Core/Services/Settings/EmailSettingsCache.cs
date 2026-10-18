using NextGenCoaching.Core.Entities;

namespace NextGenCoaching.Core.Services.Settings;

/// <summary>
/// Process-wide cache of the single <c>email_settings</c> row. Registered as a singleton so
/// the value survives across requests; the scoped <see cref="EmailSettingsService"/> populates
/// and reads it. The TTL keeps it eventually-consistent with edits from other instances.
/// </summary>
public sealed class EmailSettingsCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    private readonly object _lock = new();
    private EmailSetting? _value;
    private DateTime _loadedAtUtc;

    public bool TryGet(out EmailSetting value)
    {
        lock (_lock)
        {
            if (_value is not null && DateTime.UtcNow - _loadedAtUtc < Ttl)
            {
                value = _value;
                return true;
            }
        }

        value = new EmailSetting();
        return false;
    }

    public void Set(EmailSetting value)
    {
        lock (_lock)
        {
            _value = value;
            _loadedAtUtc = DateTime.UtcNow;
        }
    }

    public void Invalidate()
    {
        lock (_lock)
        {
            _value = null;
        }
    }
}
