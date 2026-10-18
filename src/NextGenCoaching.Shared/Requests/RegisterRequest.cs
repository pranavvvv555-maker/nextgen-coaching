using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class RegisterRequest
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "Passwords don't match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [StringLength(100)] public string? City { get; set; }

    /// <summary>Optional course slug they're interested in. Captured for counsellor follow-up.</summary>
    [StringLength(120)] public string? InterestedInCourseSlug { get; set; }
}
