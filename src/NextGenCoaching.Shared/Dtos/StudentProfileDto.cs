namespace NextGenCoaching.Shared.Dtos;

public class StudentProfileDto
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ProfileImageUrl { get; init; }
    public string? GraduationDegree { get; init; }
    public int? GraduationYear { get; init; }
    public string? GraduationCollege { get; init; }
    public string? CurrentRole { get; init; }
    public string? TargetRole { get; init; }
    public string? LinkedinUrl { get; init; }
    public string? GithubUrl { get; init; }
    public string? PortfolioUrl { get; init; }
    public string? Bio { get; init; }
}
