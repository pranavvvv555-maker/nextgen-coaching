using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class UpsertJobOpeningRequest
{
    [Required, StringLength(200)] public string CompanyName { get; set; } = string.Empty;
    [StringLength(500)] public string? CompanyLogoUrl { get; set; }
    [Required, StringLength(200)] public string RoleTitle { get; set; } = string.Empty;
    [StringLength(3000)] public string? Description { get; set; }
    public List<string> SkillsRequired { get; set; } = new();
    public decimal? MinExperienceYears { get; set; }
    public decimal? MinCtcLpa { get; set; }
    public decimal? MaxCtcLpa { get; set; }
    [StringLength(200)] public string? Location { get; set; }
    [StringLength(100)] public string? WorkMode { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    [Url, StringLength(500)] public string? ApplicationUrl { get; set; }
}
