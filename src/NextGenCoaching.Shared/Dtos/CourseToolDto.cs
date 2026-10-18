namespace NextGenCoaching.Shared.Dtos;

public class CourseToolDto
{
    public string ToolName { get; init; } = string.Empty;
    public string? IconUrl { get; init; }
    public string? Description { get; init; }
    public int SortOrder { get; init; }
}
