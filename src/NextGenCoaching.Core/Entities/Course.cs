using NextGenCoaching.Core.Common;

namespace NextGenCoaching.Core.Entities;

public class Course : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Tagline { get; set; }
    public string? Description { get; set; }
    public int DurationMonths { get; set; }
    public int? TotalHours { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string Currency { get; set; } = "INR";
    public string Level { get; set; } = "beginner";
    public string Mode { get; set; } = "hybrid";
    public string? HeroImageUrl { get; set; }
    public string? BrochureUrl { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFlagship { get; set; }
    public string? Category { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public int SortOrder { get; set; }

    public ICollection<CourseTool> Tools { get; set; } = new List<CourseTool>();
    public ICollection<CourseJourneyStep> JourneySteps { get; set; } = new List<CourseJourneyStep>();
    public ICollection<CourseFeature> Features { get; set; } = new List<CourseFeature>();
    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
