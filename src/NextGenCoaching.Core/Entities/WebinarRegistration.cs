using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

public class WebinarRegistration : BaseEntity
{
    public Guid WebinarId { get; set; }
    public Guid LeadId { get; set; }
    public DateTime RegisteredAtUtc { get; set; }
    public bool Attended { get; set; }
    public int? FeedbackRating { get; set; }
    public string? FeedbackComment { get; set; }

    public Webinar Webinar { get; set; } = null!;
    public Lead Lead { get; set; } = null!;
}
