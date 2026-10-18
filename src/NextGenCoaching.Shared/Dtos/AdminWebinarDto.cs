namespace NextGenCoaching.Shared.Dtos;

public class AdminWebinarDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? CourseId { get; init; }
    public string? CourseTitle { get; init; }
    public DateTime ScheduledAtUtc { get; init; }
    public int DurationMinutes { get; init; }
    public string? JoinUrl { get; init; }
    public string? RecordingUrl { get; init; }
    public int? Capacity { get; init; }
    public string Status { get; init; } = "Scheduled";
    public string? BannerImageUrl { get; init; }
    public int RegistrationsCount { get; init; }
    public int AttendedCount { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
