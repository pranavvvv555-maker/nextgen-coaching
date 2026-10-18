namespace NextGenCoaching.Shared.Dtos;

public class CourseJourneyStepDto
{
    public int StepNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Icon { get; init; }
}
