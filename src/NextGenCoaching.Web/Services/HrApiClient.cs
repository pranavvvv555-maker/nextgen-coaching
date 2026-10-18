using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Web.Services;

public class HrApiClient
{
    private readonly ApiClient _api;
    public HrApiClient(ApiClient api) => _api = api;

    public Task<HrDashboardDto?> GetDashboardAsync(CancellationToken ct = default) =>
        _api.GetAsync<HrDashboardDto>(ApiRoutes.Hr.Dashboard, ct);

    // Resumes
    public Task<IReadOnlyList<HrResumeListItemDto>?> GetPendingResumesAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<HrResumeListItemDto>>($"{ApiRoutes.Hr.Resumes}/pending", ct);

    public Task<HrResumeDetailDto?> GetResumeAsync(Guid id, CancellationToken ct = default) =>
        _api.GetAsync<HrResumeDetailDto>($"{ApiRoutes.Hr.Resumes}/{id}", ct);

    public Task<ApiResponse<object>?> ReviewResumeAsync(Guid id, ReviewResumeRequest req, CancellationToken ct = default) =>
        _api.PostAsync<ReviewResumeRequest, object>($"{ApiRoutes.Hr.Resumes}/{id}/review", req, ct);

    // Jobs
    public Task<IReadOnlyList<HrJobOpeningDto>?> GetJobsAsync(string? status = null, CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<HrJobOpeningDto>>(
            string.IsNullOrEmpty(status) ? ApiRoutes.Hr.Jobs : $"{ApiRoutes.Hr.Jobs}?status={Uri.EscapeDataString(status)}", ct);

    public Task<ApiResponse<Guid>?> CreateJobAsync(UpsertJobOpeningRequest req, CancellationToken ct = default) =>
        _api.PostAsync<UpsertJobOpeningRequest, Guid>(ApiRoutes.Hr.Jobs, req, ct);

    public Task<ApiResponse<Guid>?> UpdateJobAsync(Guid id, UpsertJobOpeningRequest req, CancellationToken ct = default) =>
        _api.PutAsync<UpsertJobOpeningRequest, Guid>($"{ApiRoutes.Hr.Jobs}/{id}", req, ct);

    public Task<ApiResponse<object>?> CloseJobAsync(Guid id, CancellationToken ct = default) =>
        _api.PostAsync<object, object>($"{ApiRoutes.Hr.Jobs}/{id}/close", new { }, ct);

    // Referrals
    public Task<IReadOnlyList<HrReferralDto>?> GetReferralsAsync(string? status = null, CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<HrReferralDto>>(
            string.IsNullOrEmpty(status) ? ApiRoutes.Hr.Referrals : $"{ApiRoutes.Hr.Referrals}?status={Uri.EscapeDataString(status)}", ct);

    public Task<ApiResponse<object>?> UpdateReferralStatusAsync(Guid id, UpdateReferralStatusRequest req, CancellationToken ct = default) =>
        _api.PostAsync<UpdateReferralStatusRequest, object>($"{ApiRoutes.Hr.Referrals}/{id}/status", req, ct);

    // ---------- My profile ----------
    public Task<HrProfileDto?> GetProfileAsync(CancellationToken ct = default) =>
        _api.GetAsync<HrProfileDto>(ApiRoutes.Hr.Profile, ct);

    public Task<ApiResponse<object>?> UpdateProfileAsync(UpdateAccountRequest req, CancellationToken ct = default) =>
        _api.PutAsync<UpdateAccountRequest, object>(ApiRoutes.Hr.Profile, req, ct);
}
