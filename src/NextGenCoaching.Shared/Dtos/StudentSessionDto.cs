namespace NextGenCoaching.Shared.Dtos;

public class StudentSessionDto
{
    public Guid SessionId { get; init; }
    public Guid CourseId { get; init; }
    public string CourseSlug { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public string SessionType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DateTime ScheduledAtUtc { get; init; }
    public int DurationMinutes { get; init; }
    public string? MeetingUrl { get; init; }
    public string? RecordingUrl { get; init; }
    public string Status { get; init; } = string.Empty;
}
