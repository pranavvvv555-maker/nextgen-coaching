namespace NextGenCoaching.Shared.Dtos;

// ----- Instructor side -----
public class InstructorSessionListItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string BatchName { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public DateTime ScheduledAtUtc { get; init; }
    public int RosterSize { get; init; }
    public int MarkedCount { get; init; }
}

public class AttendanceRosterDto
{
    public Guid SessionId { get; init; }
    public string SessionTitle { get; init; } = string.Empty;
    public string BatchName { get; init; } = string.Empty;
    public DateTime ScheduledAtUtc { get; init; }
    public List<AttendanceRosterRowDto> Rows { get; init; } = new();
}

public class AttendanceRosterRowDto
{
    public Guid EnrollmentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string Status { get; set; } = "Present";   // Present | Absent | Late (mutable: bound in the roster UI)
}

// ----- Student side -----
public class StudentAttendanceDto
{
    public int TotalSessions { get; init; }
    public int Present { get; init; }
    public int Late { get; init; }
    public int Absent { get; init; }
    public int PercentPresent { get; init; }
    public List<StudentAttendanceItemDto> Items { get; init; } = new();
}

public class StudentAttendanceItemDto
{
    public string SessionTitle { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public DateTime ScheduledAtUtc { get; init; }
    public string Status { get; init; } = string.Empty;
}
