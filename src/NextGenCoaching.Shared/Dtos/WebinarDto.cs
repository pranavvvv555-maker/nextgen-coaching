namespace NextGenCoaching.Shared.Dtos;

public class WebinarDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? CourseId { get; init; }
    public string? CourseTitle { get; init; }
    public DateTime ScheduledAtUtc { get; init; }
    public int DurationMinutes { get; init; }
    public string? BannerImageUrl { get; init; }
    public string Status { get; init; } = "scheduled";
}
