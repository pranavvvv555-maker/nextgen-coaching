using NextGenCoaching.Core.Common;
using NextGenCoaching.Shared.Enums;

namespace NextGenCoaching.Core.Entities;

public class Webinar : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CourseId { get; set; }
    public Guid? HostUserId { get; set; }
    public DateTime ScheduledAtUtc { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public string? JoinUrl { get; set; }
    public string? RecordingUrl { get; set; }
    public int? Capacity { get; set; }
    public WebinarStatus Status { get; set; } = WebinarStatus.Scheduled;
    public string? BannerImageUrl { get; set; }

    public Course? Course { get; set; }
    public ICollection<WebinarRegistration> Registrations { get; set; } = new List<WebinarRegistration>();
}
