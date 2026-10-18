namespace NextGenCoaching.Shared.Dtos;

public class AdminCourseListItemDto
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Tagline { get; init; }
    public string? Category { get; init; }
    public int DurationMonths { get; init; }
    public decimal Price { get; init; }
    public decimal? DiscountPrice { get; init; }
    public string Currency { get; init; } = "INR";
    public string Level { get; init; } = "beginner";
    public string Mode { get; init; } = "hybrid";
    public bool IsPublished { get; init; }
    public bool IsFlagship { get; init; }
    public int SortOrder { get; init; }
    public int ActiveBatchesCount { get; init; }
    public int EnrolledStudentsCount { get; init; }
}
