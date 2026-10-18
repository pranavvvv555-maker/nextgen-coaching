namespace NextGenCoaching.Shared.Dtos;

public class AdminReportDto
{
    public int TotalStudents { get; init; }
    public int ActiveEnrollments { get; init; }
    public decimal TotalRevenue { get; init; }
    public int Placements { get; init; }              // referrals in Joined state
    public int OpenJobs { get; init; }
    public List<RevenueByCourseRow> RevenueByCourse { get; init; } = new();
    public List<EnrollmentsByMonthRow> EnrollmentsByMonth { get; init; } = new();
    public List<PlacementsByStatusRow> PlacementsByStatus { get; init; } = new();
}

public class RevenueByCourseRow
{
    public string CourseTitle { get; init; } = string.Empty;
    public int Enrollments { get; init; }
    public decimal Revenue { get; init; }
}

public class EnrollmentsByMonthRow
{
    public string Month { get; init; } = string.Empty;   // yyyy-MM
    public int Count { get; init; }
}

public class PlacementsByStatusRow
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
}
