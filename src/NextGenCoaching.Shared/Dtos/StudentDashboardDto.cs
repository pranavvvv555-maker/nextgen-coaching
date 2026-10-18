namespace NextGenCoaching.Shared.Dtos;

public class StudentDashboardDto
{
    public StudentProfileSummary Profile { get; init; } = new();
    public StudentStatsDto Stats { get; init; } = new();
    public IReadOnlyList<StudentEnrollmentDto> Enrollments { get; init; } = Array.Empty<StudentEnrollmentDto>();
    public IReadOnlyList<StudentSessionDto> UpcomingSessions { get; init; } = Array.Empty<StudentSessionDto>();
    public IReadOnlyList<StudentAssignmentDto> PendingAssignments { get; init; } = Array.Empty<StudentAssignmentDto>();
    public IReadOnlyList<StudentMockInterviewDto> UpcomingMockInterviews { get; init; } = Array.Empty<StudentMockInterviewDto>();
    public IReadOnlyList<StudentAnnouncementDto> Announcements { get; init; } = Array.Empty<StudentAnnouncementDto>();
}

public class StudentProfileSummary
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? CurrentRole { get; init; }
    public string? TargetRole { get; init; }
    public string? ProfileImageUrl { get; init; }
}

public class StudentStatsDto
{
    public int CoursesEnrolled { get; init; }
    public int AssignmentsPending { get; init; }
    public int MockInterviewsUpcoming { get; init; }
    public decimal AttendancePercentage { get; init; }
}
