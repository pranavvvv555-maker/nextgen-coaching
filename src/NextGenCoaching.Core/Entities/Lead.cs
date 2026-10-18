using NextGenCoaching.Core.Common;
using NextGenCoaching.Shared.Enums;

namespace NextGenCoaching.Core.Entities;

public class Lead : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Graduation { get; set; }
    public int? YearOfGraduation { get; set; }
    public LeadSource Source { get; set; }
    public string? SourceCampaign { get; set; }
    public Guid? CourseInterestId { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public string? LostReason { get; set; }
    public Guid? AssignedCounsellorId { get; set; }
    public string? Notes { get; set; }
    public DateTime? NextFollowupAtUtc { get; set; }
    public Guid? ConvertedToStudentId { get; set; }

    public Course? CourseInterest { get; set; }
    public ICollection<LeadActivity> Activities { get; set; } = new List<LeadActivity>();
    public ICollection<WebinarRegistration> WebinarRegistrations { get; set; } = new List<WebinarRegistration>();
}
