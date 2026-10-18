namespace NextGenCoaching.Shared.Dtos;

public class ProjectGradingDto
{
    public Guid SubmissionId { get; init; }
    public Guid ProjectId { get; init; }
    public int ProjectNumber { get; init; }
    public string ProjectTitle { get; init; } = string.Empty;
    public string ProjectBrief { get; init; } = string.Empty;
    public string? RubricJson { get; init; }
    public string CourseTitle { get; init; } = string.Empty;
    public string BatchName { get; init; } = string.Empty;

    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;

    public string? RepoUrl { get; init; }
    public string? DeployedUrl { get; init; }
    public string? PresentationUrl { get; init; }
    public string? Description { get; init; }
    public DateTime SubmittedAtUtc { get; init; }

    public string Status { get; init; } = "Submitted";
    public decimal? TotalScore { get; init; }
    public string? Feedback { get; init; }
    public string? EvaluationJson { get; init; }
    public DateTime? GradedAtUtc { get; init; }
}
