namespace NextGenCoaching.Core.Security;

public class CaptchaOptions
{
    public const string SectionName = "Captcha";

    /// <summary>Master switch. When false, verification always passes.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>"turnstile" (Cloudflare), "recaptcha", or "hcaptcha". Only turnstile/recaptcha/hcaptcha siteverify shape is supported here.</summary>
    public string Provider { get; set; } = "turnstile";

    public string? SiteKey { get; set; }
    public string? SecretKey { get; set; }

    /// <summary>siteverify endpoint. Defaults to Cloudflare Turnstile.</summary>
    public string VerifyUrl { get; set; } = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
}
