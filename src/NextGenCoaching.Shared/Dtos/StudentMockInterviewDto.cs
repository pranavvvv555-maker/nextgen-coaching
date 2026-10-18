namespace NextGenCoaching.Shared.Dtos;

public class StudentMockInterviewDto
{
    public Guid Id { get; init; }
    public string CourseTitle { get; init; } = string.Empty;
    public int RoundNumber { get; init; }
    public DateTime ScheduledAtUtc { get; init; }
    public int DurationMinutes { get; init; }
    public string? MeetingUrl { get; init; }
}
