using NextGenCoaching.Core.Common;
using NextGenCoaching.Core.Enums;

namespace NextGenCoaching.Core.Entities;

public class Session : BaseEntity
{
    public Guid BatchId { get; set; }
    public SessionType SessionType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ScheduledAtUtc { get; set; }
    public int DurationMinutes { get; set; } = 90;
    public Guid? InstructorId { get; set; }
    public string? MeetingUrl { get; set; }
    public string? RecordingUrl { get; set; }
    public string? ResourcesUrl { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

    public Batch Batch { get; set; } = null!;
    public ICollection<SessionAttendance> Attendances { get; set; } = new List<SessionAttendance>();
}
