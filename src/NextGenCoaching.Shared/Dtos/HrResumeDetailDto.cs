namespace NextGenCoaching.Shared.Dtos;

public class HrResumeDetailDto
{
    public Guid Id { get; init; }
    public Guid StudentProfileId { get; init; }
    public int Version { get; init; }
    public string FileUrl { get; init; } = string.Empty;
    public string Status { get; init; } = "Draft";
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public string? StudentPhone { get; init; }
    public string? TargetRole { get; init; }
    public string? CurrentRole { get; init; }
    public string? Bio { get; init; }
    public string? LinkedinUrl { get; init; }
    public string? GithubUrl { get; init; }
    public string? PortfolioUrl { get; init; }
    public string? GraduationDegree { get; init; }
    public int? GraduationYear { get; init; }
    public string? GraduationCollege { get; init; }
    public string? ReviewerNotes { get; init; }
    public DateTime? ReviewedAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
