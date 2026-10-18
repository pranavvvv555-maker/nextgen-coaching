namespace NextGenCoaching.Core.Services.Settings;

/// <summary>
/// Process-wide cache of the <c>app_settings</c> table. Registered as a <b>singleton</b>
/// so the snapshot survives across requests/circuits, while the scoped
/// <see cref="AppSettingsService"/> (which holds the scoped DbContext) populates and reads it.
/// The TTL keeps the snapshot eventually-consistent with edits made by other instances.
/// </summary>
public sealed class AppSettingsCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    private readonly object _lock = new();
    private IReadOnlyDictionary<string, string?>? _snapshot;
    private DateTime _loadedAtUtc;

    public bool TryGetSnapshot(out IReadOnlyDictionary<string, string?> snapshot)
    {
        lock (_lock)
        {
            if (_snapshot is not null && DateTime.UtcNow - _loadedAtUtc < Ttl)
            {
                snapshot = _snapshot;
                return true;
            }
        }

        snapshot = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        return false;
    }

    public void Set(IReadOnlyDictionary<string, string?> values)
    {
        lock (_lock)
        {
            _snapshot = values;
            _loadedAtUtc = DateTime.UtcNow;
        }
    }

    /// <summary>Force the next read to reload from the database.</summary>
    public void Invalidate()
    {
        lock (_lock)
        {
            _snapshot = null;
        }
    }
}
