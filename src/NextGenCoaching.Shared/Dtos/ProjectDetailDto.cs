namespace NextGenCoaching.Shared.Dtos;

public class ProjectDetailDto
{
    public Guid Id { get; init; }
    public Guid? EnrollmentId { get; init; }
    public int ProjectNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Brief { get; init; } = string.Empty;
    public string? DatasetUrl { get; init; }
    public string? RubricJson { get; init; }
    public int RecommendedDurationWeeks { get; init; }
    public string CourseTitle { get; init; } = string.Empty;

    public Guid? SubmissionId { get; init; }
    public string Status { get; init; } = "NotStarted";
    public string? RepoUrl { get; init; }
    public string? DeployedUrl { get; init; }
    public string? PresentationUrl { get; init; }
    public string? Description { get; init; }
    public DateTime? SubmittedAtUtc { get; init; }
    public decimal? TotalScore { get; init; }
    public string? Feedback { get; init; }
    public string? EvaluationJson { get; init; }
}
