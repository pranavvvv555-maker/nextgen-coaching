namespace NextGenCoaching.Shared.Dtos;

public class StudentAssignmentDto
{
    public Guid AssignmentId { get; init; }
    public Guid? EnrollmentId { get; init; }
    public Guid? SubmissionId { get; init; }
    public string CourseTitle { get; init; } = string.Empty;
    public string? CourseSlug { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? ToolName { get; init; }
    public int MaxScore { get; init; }
    public DateTime DueAtUtc { get; init; }
    public string Status { get; init; } = "NotStarted";   // NotStarted, Submitted, Graded
    public decimal? Score { get; init; }
}
