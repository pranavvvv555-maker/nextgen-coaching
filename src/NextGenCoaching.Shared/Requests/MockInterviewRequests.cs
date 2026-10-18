namespace NextGenCoaching.Shared.Requests;

public class ScheduleMockInterviewRequest
{
    public Guid EnrollmentId { get; set; }
    public Guid? InterviewerId { get; set; }
    public int RoundNumber { get; set; } = 1;
    public DateTime ScheduledAtUtc { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public string? MeetingUrl { get; set; }
}

public class RecordMockInterviewFeedbackRequest
{
    public string Status { get; set; } = "Completed";   // Completed | NoShow | Cancelled
    public int? TechnicalRating { get; set; }
    public int? CommunicationRating { get; set; }
    public int? ProblemSolvingRating { get; set; }
    public int? OverallRating { get; set; }
    public string? Strengths { get; set; }
    public string? AreasToImprove { get; set; }
    public string? RecordingUrl { get; set; }
    public string? RecommendedNextSteps { get; set; }
}
