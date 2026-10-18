using NextGenCoaching.Shared.Constants;
using NextGenCoaching.Shared.Dtos;
using NextGenCoaching.Shared.Requests;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Web.Services;

public class PaymentApiClient
{
    private readonly ApiClient _api;
    public PaymentApiClient(ApiClient api) => _api = api;

    // ----- Public checkout -----
    public Task<ApiResponse<CheckoutInitDto>?> InitCheckoutAsync(CreateCheckoutRequest req, CancellationToken ct = default) =>
        _api.PostAsync<CreateCheckoutRequest, CheckoutInitDto>($"{ApiRoutes.Base}/checkout/init", req, ct);

    public Task<ApiResponse<object>?> VerifyCheckoutAsync(VerifyCheckoutRequest req, CancellationToken ct = default) =>
        _api.PostAsync<VerifyCheckoutRequest, object>($"{ApiRoutes.Base}/checkout/verify", req, ct);

    // ----- Admin -----
    public Task<ApiResponse<ConvertLeadResultDto>?> ConvertLeadAsync(Guid leadId, CancellationToken ct = default) =>
        _api.PostAsync<object, ConvertLeadResultDto>($"{ApiRoutes.Admin.Leads}/{leadId}/convert", new { }, ct);

    public Task<PagedResult<PaymentSummaryDto>?> GetPaymentsAsync(int page = 1, int pageSize = 25, string? status = null, CancellationToken ct = default)
    {
        var qs = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(status)) qs.Add($"status={Uri.EscapeDataString(status)}");
        return _api.GetAsync<PagedResult<PaymentSummaryDto>>($"{ApiRoutes.Admin.Payments}/?{string.Join("&", qs)}", ct);
    }

    public Task<ApiResponse<object>?> RefundAsync(Guid transactionId, RefundPaymentRequest req, CancellationToken ct = default) =>
        _api.PostAsync<RefundPaymentRequest, object>($"{ApiRoutes.Admin.Payments}/{transactionId}/refund", req, ct);

    public string GetAdminInvoiceUrl(Guid transactionId) => $"{ApiRoutes.Admin.Payments}/{transactionId}/invoice";

    // ----- Student -----
    public Task<IReadOnlyList<PaymentSummaryDto>?> GetMyPaymentsAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<PaymentSummaryDto>>(ApiRoutes.Student.Payments, ct);

    public string GetStudentInvoiceUrl(Guid transactionId) => $"{ApiRoutes.Student.Payments}/{transactionId}/invoice";
}
