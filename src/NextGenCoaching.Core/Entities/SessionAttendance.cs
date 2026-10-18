using NextGenCoaching.Core.Common;
using NextGenCoaching.Core.Enums;

namespace NextGenCoaching.Core.Entities;

public class SessionAttendance : BaseEntity
{
    public Guid SessionId { get; set; }
    public Guid EnrollmentId { get; set; }
    public AttendanceStatus Status { get; set; }
    public DateTime? JoinedAtUtc { get; set; }
    public DateTime? LeftAtUtc { get; set; }

    public Session Session { get; set; } = null!;
    public Enrollment Enrollment { get; set; } = null!;
}
