using NextGenCoaching.Core.Common;
using NextGenCoaching.Core.Enums;
using NextGenCoaching.Core.Identity;

namespace NextGenCoaching.Core.Entities;

public class StudentProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string? GraduationDegree { get; set; }
    public int? GraduationYear { get; set; }
    public string? GraduationCollege { get; set; }
    public string? CurrentRole { get; set; }
    public string? TargetRole { get; set; }
    public string? LinkedinUrl { get; set; }
    public string? GithubUrl { get; set; }
    public string? PortfolioUrl { get; set; }
    public string? Bio { get; set; }
    public StudentStatus Status { get; set; } = StudentStatus.Active;

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Resume> Resumes { get; set; } = new List<Resume>();
    public ICollection<JobReferral> JobReferrals { get; set; } = new List<JobReferral>();
}
