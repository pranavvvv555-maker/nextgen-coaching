using NextGenCoaching.Core.Common;
using NextGenCoaching.Core.Enums;

namespace NextGenCoaching.Core.Entities;

public class LeadActivity : BaseEntity
{
    public Guid LeadId { get; set; }
    public LeadActivityType ActivityType { get; set; }
    public string? Outcome { get; set; }
    public string? Description { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public DateTime OccurredAtUtc { get; set; }

    public Lead Lead { get; set; } = null!;
}
