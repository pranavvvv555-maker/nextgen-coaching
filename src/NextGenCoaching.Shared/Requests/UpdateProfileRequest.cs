using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class UpdateProfileRequest
{
    [Required, StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string? PhoneNumber { get; set; }

    [StringLength(100)] public string? City { get; set; }
    [StringLength(100)] public string? State { get; set; }

    [Url, StringLength(500)]
    public string? ProfileImageUrl { get; set; }

    [StringLength(200)] public string? GraduationDegree { get; set; }
    [Range(1990, 2099)] public int? GraduationYear { get; set; }
    [StringLength(200)] public string? GraduationCollege { get; set; }
    [StringLength(200)] public string? CurrentRole { get; set; }
    [StringLength(200)] public string? TargetRole { get; set; }

    [Url, StringLength(500)] public string? LinkedinUrl { get; set; }
    [Url, StringLength(500)] public string? GithubUrl { get; set; }
    [Url, StringLength(500)] public string? PortfolioUrl { get; set; }

    [StringLength(1500)] public string? Bio { get; set; }
}
