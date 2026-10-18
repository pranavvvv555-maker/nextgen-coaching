using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

public class CourseJourneyStep : BaseEntity
{
    public Guid CourseId { get; set; }
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Icon { get; set; }

    public Course Course { get; set; } = null!;
}
