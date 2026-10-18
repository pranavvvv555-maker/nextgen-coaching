namespace NextGenCoaching.Shared.Dtos;

public class InstructorDashboardDto
{
    public string FullName { get; init; } = string.Empty;
    public InstructorStatsDto Stats { get; init; } = new();
    public IReadOnlyList<InstructorBatchSummaryDto> Batches { get; init; } = Array.Empty<InstructorBatchSummaryDto>();
    public IReadOnlyList<InstructorTodaySessionDto> TodaysSessions { get; init; } = Array.Empty<InstructorTodaySessionDto>();
    public IReadOnlyList<GradingQueueItemDto> PendingAssignments { get; init; } = Array.Empty<GradingQueueItemDto>();
    public IReadOnlyList<GradingQueueItemDto> PendingProjects { get; init; } = Array.Empty<GradingQueueItemDto>();
}

public class InstructorStatsDto
{
    public int ActiveBatches { get; init; }
    public int TotalStudents { get; init; }
    public int PendingAssignmentGrades { get; init; }
    public int PendingProjectGrades { get; init; }
}

public class InstructorBatchSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public int StudentCount { get; init; }
    public int CompletedSessions { get; init; }
    public int TotalSessions { get; init; }
}

public class InstructorTodaySessionDto
{
    public Guid Id { get; init; }
    public Guid BatchId { get; init; }
    public string BatchName { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DateTime ScheduledAtUtc { get; init; }
    public int DurationMinutes { get; init; }
    public string? MeetingUrl { get; init; }
}
