using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class UpsertJobOpeningRequest : IValidatableObject
{
    [Required, StringLength(200)] public string CompanyName { get; set; } = string.Empty;
    [Url, StringLength(500)] public string? CompanyLogoUrl { get; set; }
    [Required, StringLength(200)] public string RoleTitle { get; set; } = string.Empty;
    [StringLength(3000)] public string? Description { get; set; }
    public List<string> SkillsRequired { get; set; } = new();
    [Range(0, 60)] public decimal? MinExperienceYears { get; set; }
    [Range(0, 1000)] public decimal? MinCtcLpa { get; set; }
    [Range(0, 1000)] public decimal? MaxCtcLpa { get; set; }
    [StringLength(200)] public string? Location { get; set; }
    [StringLength(100)] public string? WorkMode { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    [Url, StringLength(500)] public string? ApplicationUrl { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinCtcLpa.HasValue && MaxCtcLpa.HasValue && MinCtcLpa > MaxCtcLpa)
            yield return new ValidationResult("Maximum CTC cannot be less than minimum CTC.", new[] { nameof(MaxCtcLpa) });
    }
}
