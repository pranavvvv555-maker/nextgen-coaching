using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

public class Assignment : BaseEntity
{
    public Guid CourseId { get; set; }
    public Guid? CourseToolId { get; set; }
    public int AssignmentNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ResourcesUrl { get; set; }
    public int DueOffsetDays { get; set; }
    public int MaxScore { get; set; } = 100;
    public bool IsPublished { get; set; } = true;

    public Course Course { get; set; } = null!;
    public CourseTool? CourseTool { get; set; }
    public ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
}
