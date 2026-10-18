using NextGenCoaching.Core.Common;
using NextGenCoaching.Core.Enums;
using NextGenCoaching.Shared.Enums;

namespace NextGenCoaching.Core.Entities;

public class Batch : BaseEntity
{
    public Guid CourseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? PrimaryInstructorId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public BatchMode Mode { get; set; }
    public string? ScheduleDescription { get; set; }
    public int? Capacity { get; set; }
    public BatchStatus Status { get; set; } = BatchStatus.Planned;
    public string? MeetingRoomUrl { get; set; }

    public Course Course { get; set; } = null!;
    public InstructorProfile? PrimaryInstructor { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
