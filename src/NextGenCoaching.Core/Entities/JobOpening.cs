using NextGenCoaching.Core.Common;
using NextGenCoaching.Core.Enums;

namespace NextGenCoaching.Core.Entities;

public class JobOpening : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyLogoUrl { get; set; }
    public string RoleTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> SkillsRequired { get; set; } = new();
    public decimal? MinExperienceYears { get; set; }
    public decimal? MinCtcLpa { get; set; }
    public decimal? MaxCtcLpa { get; set; }
    public string? Location { get; set; }
    public string? WorkMode { get; set; }
    public Guid? PostedByUserId { get; set; }
    public DateTime PostedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string? ApplicationUrl { get; set; }
    public JobOpeningStatus Status { get; set; } = JobOpeningStatus.Open;

    public ICollection<JobReferral> Referrals { get; set; } = new List<JobReferral>();
}
