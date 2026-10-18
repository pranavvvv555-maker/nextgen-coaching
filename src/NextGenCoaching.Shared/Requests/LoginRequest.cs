using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}
