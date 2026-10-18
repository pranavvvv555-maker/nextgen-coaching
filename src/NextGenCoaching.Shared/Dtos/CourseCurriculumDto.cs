namespace NextGenCoaching.Shared.Dtos;

/// <summary>Everything editable about a course's curriculum, in one aggregate.</summary>
public class CourseCurriculumDto
{
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = string.Empty;
    public List<CurriculumToolDto> Tools { get; init; } = new();
    public List<CurriculumJourneyStepDto> JourneySteps { get; init; } = new();
    public List<CurriculumFeatureDto> Features { get; init; } = new();
    public List<CurriculumAssignmentDto> Assignments { get; init; } = new();
    public List<CurriculumProjectDto> Projects { get; init; } = new();
}

public class CurriculumToolDto
{
    public Guid Id { get; init; }
    public string ToolName { get; init; } = string.Empty;
    public string? IconUrl { get; init; }
    public string? Description { get; init; }
    public int SortOrder { get; init; }
}

public class CurriculumJourneyStepDto
{
    public Guid Id { get; init; }
    public int StepNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Icon { get; init; }
}

public class CurriculumFeatureDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int SortOrder { get; init; }
}

public class CurriculumAssignmentDto
{
    public Guid Id { get; init; }
    public int AssignmentNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ResourcesUrl { get; init; }
    public int DueOffsetDays { get; init; }
    public int MaxScore { get; init; }
    public bool IsPublished { get; init; }
}

public class CurriculumProjectDto
{
    public Guid Id { get; init; }
    public int ProjectNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Brief { get; init; } = string.Empty;
    public string? DatasetUrl { get; init; }
    public int RecommendedDurationWeeks { get; init; }
}
