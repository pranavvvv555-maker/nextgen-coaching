namespace NextGenCoaching.Shared.Requests;

public class SaveAttendanceRequest
{
    public List<AttendanceEntryDto> Entries { get; set; } = new();
}

public class AttendanceEntryDto
{
    public Guid EnrollmentId { get; set; }
    public string Status { get; set; } = "Present";   // Present | Absent | Late
}
