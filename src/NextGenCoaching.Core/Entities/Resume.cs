using NextGenCoaching.Core.Common;
using NextGenCoaching.Core.Enums;

namespace NextGenCoaching.Core.Entities;

public class Resume : BaseEntity
{
    public Guid StudentProfileId { get; set; }
    public int Version { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public ResumeStatus Status { get; set; } = ResumeStatus.Draft;
    public Guid? ReviewerUserId { get; set; }
    public string? ReviewerNotes { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public bool IsCurrent { get; set; } = true;

    public StudentProfile StudentProfile { get; set; } = null!;
}
