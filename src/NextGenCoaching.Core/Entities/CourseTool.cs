using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

public class CourseTool : BaseEntity
{
    public Guid CourseId { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }

    public Course Course { get; set; } = null!;
}
