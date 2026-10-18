using NextGenCoaching.Core.Common;
using NextGenCoaching.Core.Enums;

namespace NextGenCoaching.Core.Entities;

public class AssignmentSubmission : BaseEntity
{
    public Guid AssignmentId { get; set; }
    public Guid EnrollmentId { get; set; }
    public string? SubmissionUrl { get; set; }
    public string? SubmissionText { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public decimal? Score { get; set; }
    public string? Feedback { get; set; }
    public Guid? GradedByUserId { get; set; }
    public DateTime? GradedAtUtc { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

    public Assignment Assignment { get; set; } = null!;
    public Enrollment Enrollment { get; set; } = null!;
}
