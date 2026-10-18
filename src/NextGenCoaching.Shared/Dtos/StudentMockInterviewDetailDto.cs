namespace NextGenCoaching.Shared.Dtos;

/// <summary>Full mock-interview record for the student view, including post-session feedback.</summary>
public class StudentMockInterviewDetailDto
{
    public Guid Id { get; init; }
    public string CourseTitle { get; init; } = string.Empty;
    public int RoundNumber { get; init; }
    public DateTime ScheduledAtUtc { get; init; }
    public int DurationMinutes { get; init; }
    public string? MeetingUrl { get; init; }
    public string Status { get; init; } = string.Empty;

    public int? TechnicalRating { get; init; }
    public int? CommunicationRating { get; init; }
    public int? ProblemSolvingRating { get; init; }
    public int? OverallRating { get; init; }
    public string? Strengths { get; init; }
    public string? AreasToImprove { get; init; }
    public string? RecordingUrl { get; init; }
    public string? RecommendedNextSteps { get; init; }
}
