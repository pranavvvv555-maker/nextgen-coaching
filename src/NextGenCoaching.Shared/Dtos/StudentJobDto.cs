namespace NextGenCoaching.Shared.Dtos;

public class StudentJobDto
{
    public Guid Id { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string? CompanyLogoUrl { get; init; }
    public string RoleTitle { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<string> SkillsRequired { get; init; } = new();
    public decimal? MinExperienceYears { get; init; }
    public decimal? MinCtcLpa { get; init; }
    public decimal? MaxCtcLpa { get; init; }
    public string? Location { get; init; }
    public string? WorkMode { get; init; }
    public DateTime PostedAtUtc { get; init; }
    public string? ApplicationUrl { get; init; }
    public bool HasApplied { get; init; }
}
