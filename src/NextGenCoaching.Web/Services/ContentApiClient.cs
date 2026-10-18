using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;

namespace NextGenCoaching.Web.Services;

public class ContentApiClient
{
    private readonly ApiClient _api;
    public ContentApiClient(ApiClient api) => _api = api;

    public Task<IReadOnlyList<TestimonialDto>?> GetFeaturedTestimonialsAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<TestimonialDto>>($"{ApiRoutes.Public.Testimonials}/featured", ct);

    public Task<IReadOnlyList<FaqDto>?> GetFaqsAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<FaqDto>>(ApiRoutes.Public.Faqs, ct);
}
