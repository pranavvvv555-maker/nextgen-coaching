using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class SaveAttendanceRequest
{
    [Required, MaxLength(5000)]
    public List<AttendanceEntryDto> Entries { get; set; } = new();
}

public class AttendanceEntryDto
{
    public Guid EnrollmentId { get; set; }
    public string Status { get; set; } = "Present";   // Present | Absent | Late
}
