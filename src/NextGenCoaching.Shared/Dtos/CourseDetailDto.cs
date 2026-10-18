namespace NextGenCoaching.Shared.Dtos;

public class CourseDetailDto : CourseSummaryDto
{
    public string? Description { get; init; }
    public int? TotalHours { get; init; }
    public string? BrochureUrl { get; init; }
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }
    public IReadOnlyList<CourseJourneyStepDto> JourneySteps { get; init; } = Array.Empty<CourseJourneyStepDto>();
    public IReadOnlyList<CourseToolDto> Tools { get; init; } = Array.Empty<CourseToolDto>();
    public IReadOnlyList<CourseFeatureDto> Features { get; init; } = Array.Empty<CourseFeatureDto>();
}
