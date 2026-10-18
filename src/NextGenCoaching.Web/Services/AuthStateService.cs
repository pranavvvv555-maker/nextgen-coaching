using System.Security.Cryptography;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using NextGenCoaching.Shared.Dtos;

namespace NextGenCoaching.Web.Services;

/// <summary>
/// Scoped per Blazor circuit. Holds the JWT + claims that came back from the auth API
/// and persists them to ProtectedLocalStorage (encrypted with the server's Data Protection
/// keys) so a page refresh or browser restart no longer signs the user out.
///
/// Lifecycle:
///   - <see cref="InitializeAsync"/> is called once per circuit (by AuthSessionRestore in
///     Routes.razor) to rehydrate the session from browser storage.
///   - <see cref="SignInAsync"/> / <see cref="UpdateTokensAsync"/> / <see cref="SignOutAsync"/>
///     keep the in-memory copy and browser storage in sync.
/// </summary>
public class AuthStateService
{
    private const string StorageKey = "nextgen.auth.session";

    // Mirrors the API's 14-day refresh-token lifetime (AuthEndpoints), minus a safety margin
    // so we never present a refresh token the server is about to reject.
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(14) - TimeSpan.FromHours(1);

    // Refresh the access token when it has less than this much life left. The API issues
    // 30-minute tokens and validates with 1 minute of clock skew, so 90 seconds is plenty.
    private static readonly TimeSpan AccessTokenRefreshSkew = TimeSpan.FromSeconds(90);

    private readonly ProtectedLocalStorage _storage;
    private readonly ILogger<AuthStateService> _logger;
    private PersistedAuthSession? _session;

    public AuthStateService(ProtectedLocalStorage storage, ILogger<AuthStateService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    /// <summary>True once the session has been (attempted to be) restored from browser storage.</summary>
    public bool Initialized { get; private set; }

    public AuthResponseDto? Current => _session?.Auth;
    public string? AccessToken => _session?.Auth.AccessToken;
    public string? RefreshToken => _session?.Auth.RefreshToken;

    /// <summary>
    /// "Signed in" now means "we hold a refresh token that should still be valid".
    /// The access token may be expired — AuthorizationMessageHandler refreshes it transparently.
    /// </summary>
    public bool IsAuthenticated =>
        _session is not null && _session.RefreshTokenExpiresAtUtc > DateTime.UtcNow;

    /// <summary>The access token is missing, expired, or about to expire.</summary>
    public bool AccessTokenNeedsRefresh =>
        _session is null || _session.Auth.ExpiresAtUtc <= DateTime.UtcNow.Add(AccessTokenRefreshSkew);

    public IReadOnlyList<string> Roles => _session?.Auth.Roles ?? Array.Empty<string>();
    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public event Action? StateChanged;

    /// <summary>
    /// Restores the session from ProtectedLocalStorage. Safe to call multiple times; only the
    /// first call does work. Must run inside an active circuit (it uses JS interop).
    /// </summary>
    public async Task InitializeAsync()
    {
        if (Initialized) return;

        try
        {
            var stored = await _storage.GetAsync<PersistedAuthSession>(StorageKey);
            if (stored.Success && stored.Value is { } session)
            {
                if (session.RefreshTokenExpiresAtUtc > DateTime.UtcNow)
                {
                    _session = session;
                }
                else
                {
                    // Session is stale beyond recovery — clean it up so we don't retry every visit.
                    await _storage.DeleteAsync(StorageKey);
                }
            }
        }
        catch (CryptographicException)
        {
            // Data Protection keys rotated (or app moved hosts) — stored blob is unreadable. Start clean.
            _logger.LogInformation("Stored auth session could not be decrypted; clearing it.");
            await TryDeleteStoredSessionAsync();
        }
        catch (Exception ex)
        {
            // JS interop unavailable or storage blocked — degrade to signed-out rather than crash the circuit.
            _logger.LogWarning(ex, "Could not restore auth session from browser storage");
        }
        finally
        {
            Initialized = true;
            StateChanged?.Invoke();
        }
    }

    public Task SignInAsync(AuthResponseDto auth) => SetSessionAsync(auth);

    /// <summary>Called after a successful token refresh — same persistence path as sign-in.</summary>
    public Task UpdateTokensAsync(AuthResponseDto auth) => SetSessionAsync(auth);

    public async Task SignOutAsync()
    {
        _session = null;
        await TryDeleteStoredSessionAsync();
        StateChanged?.Invoke();
    }

    private async Task SetSessionAsync(AuthResponseDto auth)
    {
        _session = new PersistedAuthSession
        {
            Auth = auth,
            RefreshTokenExpiresAtUtc = DateTime.UtcNow.Add(RefreshTokenLifetime)
        };

        try
        {
            await _storage.SetAsync(StorageKey, _session);
        }
        catch (Exception ex)
        {
            // Persistence is best-effort: the user stays signed in for this circuit either way.
            _logger.LogWarning(ex, "Could not persist auth session to browser storage");
        }

        StateChanged?.Invoke();
    }

    private async Task TryDeleteStoredSessionAsync()
    {
        try { await _storage.DeleteAsync(StorageKey); }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not delete stored auth session"); }
    }

    /// <summary>What we persist to the browser. RefreshTokenExpiresAtUtc is computed client-side
    /// because the API doesn't return it; it mirrors the server's 14-day window.</summary>
    public sealed class PersistedAuthSession
    {
        public AuthResponseDto Auth { get; init; } = new();
        public DateTime RefreshTokenExpiresAtUtc { get; init; }
    }
}
