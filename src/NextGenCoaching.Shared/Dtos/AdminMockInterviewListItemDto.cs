namespace NextGenCoaching.Shared.Dtos;

public class AdminMockInterviewListItemDto
{
    public Guid Id { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public int RoundNumber { get; init; }
    public DateTime ScheduledAtUtc { get; init; }
    public int DurationMinutes { get; init; }
    public string? InterviewerName { get; init; }
    public string Status { get; init; } = string.Empty;
    public int? OverallRating { get; init; }
}
