using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
