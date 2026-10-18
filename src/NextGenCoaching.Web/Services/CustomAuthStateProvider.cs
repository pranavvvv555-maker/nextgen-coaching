using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace NextGenCoaching.Web.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly AuthStateService _state;
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public CustomAuthStateProvider(AuthStateService state)
    {
        _state = state;
        _state.StateChanged += OnAuthChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_state.IsAuthenticated || _state.Current is null)
            return Task.FromResult(new AuthenticationState(Anonymous));

        var current = _state.Current;
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, current.FullName),
            new(ClaimTypes.Email, current.Email),
            new("access_token", current.AccessToken)
        };
        claims.AddRange(current.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt", nameType: ClaimTypes.Name, roleType: ClaimTypes.Role);
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    private void OnAuthChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public void Dispose() => _state.StateChanged -= OnAuthChanged;
}
