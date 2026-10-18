using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

public class IndustryConnect : BaseEntity
{
    public Guid? BatchId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ExpertName { get; set; } = string.Empty;
    public string? ExpertCompany { get; set; }
    public string? ExpertDesignation { get; set; }
    public string? ExpertLinkedin { get; set; }
    public string? ExpertPhotoUrl { get; set; }
    public string? Topic { get; set; }
    public DateTime ScheduledAtUtc { get; set; }
    public string? MeetingUrl { get; set; }
    public string? RecordingUrl { get; set; }
}
