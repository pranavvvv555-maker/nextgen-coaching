namespace NextGenCoaching.Shared.Dtos;

public class AssignmentGradingDto
{
    public Guid SubmissionId { get; init; }
    public Guid AssignmentId { get; init; }
    public int AssignmentNumber { get; init; }
    public string AssignmentTitle { get; init; } = string.Empty;
    public string AssignmentBrief { get; init; } = string.Empty;
    public int MaxScore { get; init; }
    public string CourseTitle { get; init; } = string.Empty;
    public string BatchName { get; init; } = string.Empty;

    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;

    public string? SubmissionUrl { get; init; }
    public string? SubmissionText { get; init; }
    public DateTime SubmittedAtUtc { get; init; }

    public string Status { get; init; } = "Submitted";
    public decimal? Score { get; init; }
    public string? Feedback { get; init; }
    public DateTime? GradedAtUtc { get; init; }
}
