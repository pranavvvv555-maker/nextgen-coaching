using System.Net;
using System.Net.Http.Headers;

namespace NextGenCoaching.Web.Services;

/// <summary>
/// HTTP message handler that attaches the current JWT to outgoing API requests and
/// transparently refreshes it when it's expired or about to expire.
///
/// IMPORTANT: this handler is constructed manually per circuit in Program.cs (wrapping a
/// shared SocketsHttpHandler) rather than registered via AddHttpMessageHandler. IHttpClientFactory
/// builds handler pipelines in its own DI scope and caches them across circuits, so a
/// factory-registered handler would capture the WRONG AuthStateService instance — at best the
/// bearer token never attaches, at worst one user's token bleeds into another user's requests.
/// </summary>
public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly AuthStateService _state;
    private readonly TokenRefreshService _tokenRefresh;

    public AuthorizationMessageHandler(AuthStateService state, TokenRefreshService tokenRefresh)
    {
        _state = state;
        _tokenRefresh = tokenRefresh;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_state.IsAuthenticated && request.Headers.Authorization is null)
        {
            // Proactive refresh: never send a token we already know is (nearly) dead.
            if (_state.AccessTokenNeedsRefresh)
                await _tokenRefresh.RefreshAsync(cancellationToken);

            if (_state.AccessToken is { Length: > 0 } token)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Safety net: the server rejected a token we thought was fine (revocation, clock drift).
        // Force one refresh and retry — GETs only, so we never have to replay a request body.
        if (response.StatusCode == HttpStatusCode.Unauthorized
            && request.Method == HttpMethod.Get
            && _state.IsAuthenticated
            && await _tokenRefresh.RefreshAsync(cancellationToken)
            && _state.AccessToken is { Length: > 0 } freshToken)
        {
            response.Dispose();
            using var retry = new HttpRequestMessage(HttpMethod.Get, request.RequestUri);
            foreach (var header in request.Headers.Where(h => !string.Equals(h.Key, "Authorization", StringComparison.OrdinalIgnoreCase)))
                retry.Headers.TryAddWithoutValidation(header.Key, header.Value);
            retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", freshToken);
            response = await base.SendAsync(retry, cancellationToken);
        }

        return response;
    }
}
