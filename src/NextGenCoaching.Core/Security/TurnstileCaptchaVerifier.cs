using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextGenCoaching.Core.Interfaces;

namespace NextGenCoaching.Core.Security;

/// <summary>
/// Verifies tokens against a Cloudflare-Turnstile / reCAPTCHA / hCaptcha style
/// "siteverify" endpoint (they share the same request/response shape:
/// POST form {secret, response, remoteip} -> JSON {"success": bool}).
/// Inert when disabled or no secret is configured.
/// </summary>
public class TurnstileCaptchaVerifier : ICaptchaVerifier
{
    private readonly HttpClient _http;
    private readonly CaptchaOptions _opts;
    private readonly ILogger<TurnstileCaptchaVerifier> _log;

    public TurnstileCaptchaVerifier(HttpClient http, IOptions<CaptchaOptions> opts, ILogger<TurnstileCaptchaVerifier> log)
    {
        _http = http;
        _opts = opts.Value;
        _log = log;
    }

    public async Task<bool> VerifyAsync(string? token, string? remoteIp = null, CancellationToken ct = default)
    {
        // Safe-by-default: disabled or unconfigured => pass through.
        if (!_opts.Enabled || string.IsNullOrWhiteSpace(_opts.SecretKey))
            return true;

        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var form = new Dictionary<string, string>
            {
                ["secret"] = _opts.SecretKey!,
                ["response"] = token!
            };
            if (!string.IsNullOrWhiteSpace(remoteIp)) form["remoteip"] = remoteIp!;

            using var resp = await _http.PostAsync(_opts.VerifyUrl, new FormUrlEncodedContent(form), ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean();
        }
        catch (Exception ex)
        {
            // Fail closed on verification errors, but never throw into the request pipeline.
            _log.LogWarning(ex, "CAPTCHA verification failed to reach the provider.");
            return false;
        }
    }
}
