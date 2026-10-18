using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class RegisterWebinarRequest
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(150)]
    public string? Graduation { get; set; }

    public int? YearOfGraduation { get; set; }

    /// <summary>Optional human-verification token (Turnstile/reCAPTCHA/hCaptcha). Verified server-side; ignored when CAPTCHA is disabled.</summary>
    public string? CaptchaToken { get; set; }
}