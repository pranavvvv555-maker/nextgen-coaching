namespace NextGenCoaching.Shared.Dtos;

public class ProjectListItemDto
{
    public Guid Id { get; init; }
    public int ProjectNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public int RecommendedDurationWeeks { get; init; }
    public Guid? SubmissionId { get; init; }
    public string Status { get; init; } = "NotStarted";
    public decimal? Score { get; init; }
}
