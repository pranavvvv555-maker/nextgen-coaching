using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

public class Project : BaseEntity
{
    public Guid CourseId { get; set; }
    public int ProjectNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Brief { get; set; } = string.Empty;
    public string? DatasetUrl { get; set; }
    public string? RubricJson { get; set; }
    public int RecommendedDurationWeeks { get; set; } = 2;

    public Course Course { get; set; } = null!;
    public ICollection<ProjectSubmission> Submissions { get; set; } = new List<ProjectSubmission>();
}
