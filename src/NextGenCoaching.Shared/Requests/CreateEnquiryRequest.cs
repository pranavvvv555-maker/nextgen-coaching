using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class CreateEnquiryRequest
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? Phone { get => _phone; set => _phone = string.IsNullOrWhiteSpace(value) ? null : value.Trim(); }
    private string? _phone;

    [StringLength(200)]
    public string? Subject { get; set; }

    public Guid? CourseInterestId { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(150)]
    public string? Graduation { get; set; }

    [Required, StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    [StringLength(200)]
    public string? SourcePage { get; set; }

    /// <summary>Optional human-verification token (Turnstile/reCAPTCHA/hCaptcha). Verified server-side; ignored when CAPTCHA is disabled.</summary>
    public string? CaptchaToken { get; set; }
}
