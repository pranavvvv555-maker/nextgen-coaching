using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NextGenCoaching.Core.Payments;

/// <summary>
/// Direct HttpClient wrapper around Razorpay's REST API. We don't use the .NET SDK
/// because it lags badly behind their Node/PHP SDKs and ships its own auth helper
/// that's harder to test. Auth here is HTTP Basic with KeyId:KeySecret.
/// </summary>
public class RazorpayClient
{
    private readonly HttpClient _http;
    private readonly RazorpayOptions _opts;
    private readonly ILogger<RazorpayClient> _log;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public RazorpayClient(HttpClient http, IOptions<RazorpayOptions> opts, ILogger<RazorpayClient> log)
    {
        _http = http;
        _opts = opts.Value;
        _log = log;

        if (_opts.IsConfigured)
        {
            _http.BaseAddress = new Uri(_opts.ApiBaseUrl);
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_opts.KeyId}:{_opts.KeySecret}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
        }
    }

    public bool IsConfigured => _opts.IsConfigured;
    public string KeyId => _opts.KeyId;
    public string Currency => _opts.Currency;

    // ---------- Orders ----------

    public async Task<RazorpayOrder?> CreateOrderAsync(
        long amountInPaise,
        string receipt,
        IDictionary<string, string>? notes = null,
        CancellationToken ct = default)
    {
        var body = new
        {
            amount = amountInPaise,
            currency = _opts.Currency,
            receipt,
            notes = notes ?? new Dictionary<string, string>(),
            payment_capture = 1
        };

        var resp = await _http.PostAsJsonAsync("orders", body, JsonOpts, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var errBody = await resp.Content.ReadAsStringAsync(ct);
            _log.LogError("Razorpay CreateOrder failed: {Status} {Body}", resp.StatusCode, errBody);
            return null;
        }
        return await resp.Content.ReadFromJsonAsync<RazorpayOrder>(JsonOpts, ct);
    }

    public async Task<RazorpayPayment?> GetPaymentAsync(string paymentId, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"payments/{paymentId}", ct);
        if (!resp.IsSuccessStatusCode)
        {
            _log.LogWarning("Razorpay GetPayment {Id} returned {Status}", paymentId, resp.StatusCode);
            return null;
        }
        return await resp.Content.ReadFromJsonAsync<RazorpayPayment>(JsonOpts, ct);
    }

    // ---------- Refunds ----------

    public async Task<RazorpayRefund?> RefundPaymentAsync(
        string paymentId,
        long? amountInPaise,
        IDictionary<string, string>? notes = null,
        CancellationToken ct = default)
    {
        var body = new Dictionary<string, object?>
        {
            ["notes"] = notes ?? new Dictionary<string, string>()
        };
        if (amountInPaise.HasValue) body["amount"] = amountInPaise.Value;

        var resp = await _http.PostAsJsonAsync($"payments/{paymentId}/refund", body, JsonOpts, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var errBody = await resp.Content.ReadAsStringAsync(ct);
            _log.LogError("Razorpay Refund {Id} failed: {Status} {Body}", paymentId, resp.StatusCode, errBody);
            return null;
        }
        return await resp.Content.ReadFromJsonAsync<RazorpayRefund>(JsonOpts, ct);
    }

    // ---------- Signature verification ----------

    /// <summary>
    /// Verifies the signature returned by the Razorpay checkout modal.
    /// Formula: HMAC_SHA256(orderId + "|" + paymentId, KeySecret).
    /// </summary>
    public bool VerifyCheckoutSignature(string orderId, string paymentId, string signature)
    {
        var expected = HmacSha256Hex($"{orderId}|{paymentId}", _opts.KeySecret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(signature));
    }

    /// <summary>
    /// Verifies an incoming webhook's X-Razorpay-Signature header against the raw request body
    /// using the webhook secret (different from the API key secret).
    /// </summary>
    public bool VerifyWebhookSignature(string rawBody, string signature)
    {
        if (string.IsNullOrEmpty(_opts.WebhookSecret))
        {
            _log.LogError("Webhook signature requested but WebhookSecret is not configured");
            return false;
        }
        var expected = HmacSha256Hex(rawBody, _opts.WebhookSecret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(signature));
    }

    private static string HmacSha256Hex(string payload, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

// ---------- Response DTOs (only the fields we care about) ----------

public class RazorpayOrder
{
    [JsonPropertyName("id")]       public string Id       { get; set; } = string.Empty;
    [JsonPropertyName("entity")]   public string Entity   { get; set; } = string.Empty;
    [JsonPropertyName("amount")]   public long   Amount   { get; set; }
    [JsonPropertyName("currency")] public string Currency { get; set; } = string.Empty;
    [JsonPropertyName("receipt")]  public string? Receipt { get; set; }
    [JsonPropertyName("status")]   public string Status   { get; set; } = string.Empty;
    [JsonPropertyName("created_at")] public long CreatedAt { get; set; }
}

public class RazorpayPayment
{
    [JsonPropertyName("id")]              public string Id { get; set; } = string.Empty;
    [JsonPropertyName("order_id")]        public string? OrderId { get; set; }
    [JsonPropertyName("amount")]          public long Amount { get; set; }
    [JsonPropertyName("currency")]        public string Currency { get; set; } = string.Empty;
    [JsonPropertyName("status")]          public string Status { get; set; } = string.Empty;     // captured | authorized | failed
    [JsonPropertyName("method")]          public string? Method { get; set; }
    [JsonPropertyName("captured")]        public bool Captured { get; set; }
    [JsonPropertyName("email")]           public string? Email { get; set; }
    [JsonPropertyName("contact")]         public string? Contact { get; set; }
    [JsonPropertyName("error_code")]      public string? ErrorCode { get; set; }
    [JsonPropertyName("error_description")] public string? ErrorDescription { get; set; }
}

public class RazorpayRefund
{
    [JsonPropertyName("id")]         public string Id { get; set; } = string.Empty;
    [JsonPropertyName("payment_id")] public string PaymentId { get; set; } = string.Empty;
    [JsonPropertyName("amount")]     public long Amount { get; set; }
    [JsonPropertyName("status")]     public string Status { get; set; } = string.Empty;          // processed | pending | failed
}
