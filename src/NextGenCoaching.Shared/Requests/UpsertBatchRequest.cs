using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class UpsertBatchRequest
{
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required] public Guid CourseId { get; set; }
    public Guid? PrimaryInstructorId { get; set; }

    [Required] public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    /// <summary>"Weekend", "Daily", or "Hybrid"</summary>
    [Required] public string Mode { get; set; } = "Weekend";

    [StringLength(300)] public string? ScheduleDescription { get; set; }
    [Range(1, 1000)]    public int? Capacity { get; set; }
    [Url, StringLength(500)] public string? MeetingRoomUrl { get; set; }

    /// <summary>"Planned", "Active", "Completed", or "Cancelled". Only used on PUT.</summary>
    public string? Status { get; set; }
}
