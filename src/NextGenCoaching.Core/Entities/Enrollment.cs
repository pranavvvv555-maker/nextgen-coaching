using NextGenCoaching.Core.Common;
using NextGenCoaching.Shared.Enums;

namespace NextGenCoaching.Core.Entities;

public class Enrollment : BaseEntity
{
    public Guid StudentProfileId { get; set; }
    public Guid BatchId { get; set; }
    public Guid? PaymentTransactionId { get; set; }
    public DateTime EnrolledAtUtc { get; set; }
    public decimal AmountPaid { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public decimal CompletionPercentage { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CertificateUrl { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;
    public Batch Batch { get; set; } = null!;
    public PaymentTransaction? PaymentTransaction { get; set; }
    public ICollection<SessionAttendance> Attendances { get; set; } = new List<SessionAttendance>();
    public ICollection<AssignmentSubmission> AssignmentSubmissions { get; set; } = new List<AssignmentSubmission>();
    public ICollection<ProjectSubmission> ProjectSubmissions { get; set; } = new List<ProjectSubmission>();
    public ICollection<MockInterview> MockInterviews { get; set; } = new List<MockInterview>();
}
