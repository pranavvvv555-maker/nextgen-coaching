using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Web.Services;

public class WebinarApiClient
{
    private readonly ApiClient _api;
    public WebinarApiClient(ApiClient api) => _api = api;

    public Task<IReadOnlyList<WebinarDto>?> GetUpcomingAsync(int take = 6, CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<WebinarDto>>($"{ApiRoutes.Public.Webinars}/upcoming?take={take}", ct);

    public Task<ApiResponse<object>?> RegisterAsync(Guid webinarId, RegisterWebinarRequest req, CancellationToken ct = default) =>
        _api.PostAsync<RegisterWebinarRequest, object>($"{ApiRoutes.Public.Webinars}/{webinarId}/register", req, ct);
}
