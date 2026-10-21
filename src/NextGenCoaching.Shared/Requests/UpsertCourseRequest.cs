using System.ComponentModel.DataAnnotations;

namespace NextGenCoaching.Shared.Requests;

public class UpsertCourseRequest
{
    [Required, StringLength(120)]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug must be lowercase letters, numbers, and hyphens only.")]
    public string Slug { get; set; } = string.Empty;

    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [StringLength(300)]            public string? Tagline { get; set; }
    [StringLength(8000)]           public string? Description { get; set; }

    [Range(1, 36)]   public int DurationMonths { get; set; } = 6;
    [Range(1, 1000)] public int? TotalHours { get; set; }

    [Required, Range(0, 1_000_000)] public decimal Price { get; set; }
    [Range(0, 1_000_000)]            public decimal? DiscountPrice { get; set; }

    [Required, StringLength(10)]
    public string Currency { get; set; } = "INR";

    [Required] public string Level { get; set; } = "beginner";  // beginner / intermediate / advanced
    [Required] public string Mode { get; set; } = "hybrid";     // online / hybrid / onsite

    [RegularExpression(@"^(?:https?://[^\s/]+(?:/\S*)?|/(?!/)\S*)$", ErrorMessage = "Use an http(s) image URL or a site-relative path starting with a single slash.")]
    [StringLength(500)] public string? HeroImageUrl { get; set; }
    [Url, StringLength(500)] public string? BrochureUrl { get; set; }

    public bool IsPublished { get; set; }
    public bool IsFlagship { get; set; }

    [StringLength(100)]  public string? Category { get; set; }
    [StringLength(200)]  public string? SeoTitle { get; set; }
    [StringLength(300)]  public string? SeoDescription { get; set; }

    [Range(0, 1000)] public int SortOrder { get; set; }
}
