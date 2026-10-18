namespace NextGenCoaching.Shared.Dtos;

/// <summary>
/// Shared shape for both assignment and project grading queues.
/// `Kind` distinguishes the type for the per-submission route.
/// </summary>
public class GradingQueueItemDto
{
    public Guid SubmissionId { get; init; }
    public string Kind { get; init; } = "Assignment";   // Assignment | Project
    public string Title { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public string BatchName { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public DateTime SubmittedAtUtc { get; init; }
    public int? AgeHours { get; init; }
    public string Status { get; init; } = "Submitted";
    public int MaxScore { get; init; }
}
