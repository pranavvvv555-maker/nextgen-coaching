using NextGenCoaching.Core.Common;
using NextGenCoaching.Core.Enums;

namespace NextGenCoaching.Core.Entities;

public class MockInterview : BaseEntity
{
    public Guid EnrollmentId { get; set; }
    public Guid? InterviewerId { get; set; }
    public int RoundNumber { get; set; }
    public DateTime ScheduledAtUtc { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public string? MeetingUrl { get; set; }
    public MockInterviewStatus Status { get; set; } = MockInterviewStatus.Scheduled;
    public int? TechnicalRating { get; set; }
    public int? CommunicationRating { get; set; }
    public int? ProblemSolvingRating { get; set; }
    public int? OverallRating { get; set; }
    public string? Strengths { get; set; }
    public string? AreasToImprove { get; set; }
    public string? RecordingUrl { get; set; }
    public string? RecommendedNextSteps { get; set; }

    public Enrollment Enrollment { get; set; } = null!;
}
