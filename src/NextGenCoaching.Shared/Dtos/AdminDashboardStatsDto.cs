namespace NextGenCoaching.Shared.Dtos;

public class AdminDashboardStatsDto
{
    public int TotalStudents { get; init; }
    public int ActiveBatches { get; init; }
    public int TotalCourses { get; init; }
    public int PlacedStudents { get; init; }
    public int OpenLeads { get; init; }
    public IReadOnlyList<PlacementMonthItem> PlacementTrend { get; init; } = Array.Empty<PlacementMonthItem>();

    public IReadOnlyList<LeadsByStatusItem> LeadsByStatus { get; init; } = Array.Empty<LeadsByStatusItem>();
    public IReadOnlyList<RecentLeadItem> RecentLeads { get; init; } = Array.Empty<RecentLeadItem>();
    public IReadOnlyList<UpcomingMockInterviewItem> UpcomingMockInterviews { get; init; } = Array.Empty<UpcomingMockInterviewItem>();
}

public record LeadsByStatusItem(string Status, int Count);
public record PlacementMonthItem(string Month, int Count);

public record RecentLeadItem(Guid Id, string FullName, string CourseInterestTitle, string Status, DateTime CreatedAtUtc);

public record UpcomingMockInterviewItem(Guid Id, string StudentName, string CourseTitle, DateTime ScheduledAtUtc);
