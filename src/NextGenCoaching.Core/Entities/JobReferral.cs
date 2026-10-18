using NextGenCoaching.Core.Common;
using NextGenCoaching.Core.Enums;

namespace NextGenCoaching.Core.Entities;

public class JobReferral : BaseEntity
{
    public Guid JobOpeningId { get; set; }
    public Guid StudentProfileId { get; set; }
    public Guid? ReferredByUserId { get; set; }
    public DateTime ReferredAtUtc { get; set; }
    public JobReferralStatus Status { get; set; } = JobReferralStatus.Referred;
    public DateTime? StatusUpdatedAtUtc { get; set; }
    public decimal? OfferedCtcLpa { get; set; }
    public DateOnly? JoiningDate { get; set; }
    public string? Notes { get; set; }

    public JobOpening JobOpening { get; set; } = null!;
    public StudentProfile StudentProfile { get; set; } = null!;
}
