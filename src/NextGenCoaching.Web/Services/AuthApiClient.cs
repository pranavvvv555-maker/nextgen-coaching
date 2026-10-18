using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Web.Services;

public class AuthApiClient
{
    private readonly ApiClient _api;
    private readonly AuthStateService _state;

    public AuthApiClient(ApiClient api, AuthStateService state)
    {
        _api = api;
        _state = state;
    }

    public async Task<ApiResponse<AuthResponseDto>?> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var resp = await _api.PostAsync<LoginRequest, AuthResponseDto>(ApiRoutes.Auth.Login, req, ct);
        if (resp is { Success: true, Data: not null })
            await _state.SignInAsync(resp.Data);
        return resp;
    }

    public async Task<ApiResponse<AuthResponseDto>?> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        var resp = await _api.PostAsync<RegisterRequest, AuthResponseDto>(ApiRoutes.Auth.Register, req, ct);
        if (resp is { Success: true, Data: not null })
            await _state.SignInAsync(resp.Data);
        return resp;
    }

    /// <summary>
    /// Signs out locally and (best-effort) revokes the refresh token on the server so the
    /// persisted copy in any other browser is useless.
    /// </summary>
    public async Task LogoutAsync(CancellationToken ct = default)
    {
        try
        {
            await _api.PostAsync<object, object>(ApiRoutes.Auth.Logout, new { }, ct);
        }
        catch
        {
            // Revocation is best-effort — local sign-out must always succeed.
        }

        await _state.SignOutAsync();
    }
}
