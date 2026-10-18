namespace NextGenCoaching.Shared.Requests;

public class CreateInstructorRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Headline { get; set; }
    public string? Bio { get; set; }
    public List<string> ExpertiseTags { get; set; } = new();
    public int? YearsExperience { get; set; }
    public string? LinkedinUrl { get; set; }
    public decimal? HourlyRate { get; set; }
    public bool IsPublic { get; set; } = true;
}
