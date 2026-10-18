using System.Net;
using System.Net.Http.Json;
using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Web.Services;

/// <summary>
/// Exchanges the stored refresh token for a new access token via POST /auth/refresh.
/// Scoped per circuit. Single-flight: concurrent callers (e.g. a page load racing the
/// notification poller) share one refresh instead of burning the rotating refresh token twice.
///
/// Uses the handler-free "AuthApi" named HttpClient so refresh calls never pass through
/// AuthorizationMessageHandler (which would recurse back into this service).
/// </summary>
public class TokenRefreshService
{
    private readonly AuthStateService _state;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TokenRefreshService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public const string HttpClientName = "AuthApi";

    public TokenRefreshService(
        AuthStateService state,
        IHttpClientFactory httpClientFactory,
        ILogger<TokenRefreshService> logger)
    {
        _state = state;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Refreshes the token pair. Returns true if the session ends up with a usable access token
    /// (whether refreshed by this call or by a concurrent one). On a definitive rejection
    /// (401/403 — the refresh token was revoked or expired) the user is signed out.
    /// </summary>
    public async Task<bool> RefreshAsync(CancellationToken ct = default)
    {
        var observedToken = _state.AccessToken;
        if (_state.RefreshToken is not { Length: > 0 })
            return false;

        await _gate.WaitAsync(ct);
        try
        {
            // While we waited, another caller may have already rotated the tokens.
            if (!string.Equals(_state.AccessToken, observedToken, StringComparison.Ordinal))
                return _state.IsAuthenticated;

            var refreshToken = _state.RefreshToken;
            if (refreshToken is not { Length: > 0 })
                return false;

            var http = _httpClientFactory.CreateClient(HttpClientName);
            var response = await http.PostAsJsonAsync(
                ApiRoutes.Auth.Refresh,
                new RefreshTokenRequest { RefreshToken = refreshToken },
                ct);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                // The server no longer recognises this refresh token — session is over.
                _logger.LogInformation("Refresh token rejected by the API; signing out.");
                await _state.SignOutAsync();
                return false;
            }

            if (!response.IsSuccessStatusCode)
            {
                // Transient (5xx, network hiccup behind a proxy, …): keep the session, let the caller fail softly.
                _logger.LogWarning("Token refresh failed with status {Status}", (int)response.StatusCode);
                return false;
            }

            var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>(cancellationToken: ct);
            if (envelope is { Success: true, Data: not null })
            {
                await _state.UpdateTokensAsync(envelope.Data);
                return true;
            }

            _logger.LogWarning("Token refresh returned an unexpected payload");
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token refresh failed");
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }
}
