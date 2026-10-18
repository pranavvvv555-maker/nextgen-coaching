using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

// IMPORTANT: per-course placement tracking — unique on (student_profile_id, course_id)
public class Placement : BaseEntity
{
    public Guid StudentProfileId { get; set; }
    public Guid CourseId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyLogoUrl { get; set; }
    public string RoleTitle { get; set; } = string.Empty;
    public decimal? CtcLpa { get; set; }
    public DateOnly PlacementDate { get; set; }
    public string? Testimonial { get; set; }
    public bool IsFeatured { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
