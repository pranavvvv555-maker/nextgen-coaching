using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Web.Services;

public class LeadApiClient
{
    private readonly ApiClient _api;
    public LeadApiClient(ApiClient api) => _api = api;

    public Task<ApiResponse<object>?> CreateEnquiryAsync(CreateEnquiryRequest req, CancellationToken ct = default) =>
        _api.PostAsync<CreateEnquiryRequest, object>(ApiRoutes.Public.Enquiries, req, ct);
}
