using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

public class CourseFeature : BaseEntity
{
    public Guid CourseId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }

    public Course Course { get; set; } = null!;
}
