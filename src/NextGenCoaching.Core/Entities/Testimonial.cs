using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

public class Testimonial : BaseEntity
{
    public string StudentName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? CurrentRole { get; set; }
    public string? CurrentCompany { get; set; }
    public Guid? CourseId { get; set; }
    public string? BatchName { get; set; }
    public string Quote { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }

    public Course? Course { get; set; }
}
