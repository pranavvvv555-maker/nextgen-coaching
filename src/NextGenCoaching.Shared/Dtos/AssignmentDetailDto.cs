namespace NextGenCoaching.Shared.Dtos;

public class AssignmentDetailDto
{
    public Guid Id { get; init; }
    public Guid? EnrollmentId { get; init; }
    public int AssignmentNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ResourcesUrl { get; init; }
    public int MaxScore { get; init; }
    public DateTime DueAtUtc { get; init; }
    public string CourseTitle { get; init; } = string.Empty;
    public string? ToolName { get; init; }

    // Submission view (null if not submitted)
    public Guid? SubmissionId { get; init; }
    public string Status { get; init; } = "NotStarted";   // NotStarted | Submitted | Graded
    public string? SubmissionUrl { get; init; }
    public string? SubmissionText { get; init; }
    public DateTime? SubmittedAtUtc { get; init; }
    public decimal? Score { get; init; }
    public string? Feedback { get; init; }
}
