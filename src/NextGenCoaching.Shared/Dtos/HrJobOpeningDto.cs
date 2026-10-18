namespace NextGenCoaching.Shared.Dtos;

public class HrJobOpeningDto
{
    public Guid Id { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string? CompanyLogoUrl { get; init; }
    public string RoleTitle { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IReadOnlyList<string> SkillsRequired { get; init; } = Array.Empty<string>();
    public decimal? MinExperienceYears { get; init; }
    public decimal? MinCtcLpa { get; init; }
    public decimal? MaxCtcLpa { get; init; }
    public string? Location { get; init; }
    public string? WorkMode { get; init; }
    public DateTime PostedAtUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public string? ApplicationUrl { get; init; }
    public string Status { get; init; } = "Open";
    public int ReferralsCount { get; init; }
    public int PlacedCount { get; init; }
}
