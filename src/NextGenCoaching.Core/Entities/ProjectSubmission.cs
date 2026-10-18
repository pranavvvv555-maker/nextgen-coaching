using NextGenCoaching.Core.Common;
using NextGenCoaching.Core.Enums;

namespace NextGenCoaching.Core.Entities;

public class ProjectSubmission : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid EnrollmentId { get; set; }
    public string? RepoUrl { get; set; }
    public string? DeployedUrl { get; set; }
    public string? PresentationUrl { get; set; }
    public string? Description { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public string? EvaluationJson { get; set; }
    public decimal? TotalScore { get; set; }
    public string? Feedback { get; set; }
    public Guid? GradedByUserId { get; set; }
    public DateTime? GradedAtUtc { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

    public Project Project { get; set; } = null!;
    public Enrollment Enrollment { get; set; } = null!;
}
