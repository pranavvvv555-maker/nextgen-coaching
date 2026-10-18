namespace NextGenCoaching.Shared.Requests;

public class UpsertCurriculumToolRequest
{
    public string ToolName { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public class UpsertCurriculumJourneyStepRequest
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Icon { get; set; }
}

public class UpsertCurriculumFeatureRequest
{
    public string Label { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
}

public class UpsertCurriculumAssignmentRequest
{
    public int AssignmentNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ResourcesUrl { get; set; }
    public int DueOffsetDays { get; set; }
    public int MaxScore { get; set; } = 100;
    public bool IsPublished { get; set; } = true;
}

public class UpsertCurriculumProjectRequest
{
    public int ProjectNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Brief { get; set; } = string.Empty;
    public string? DatasetUrl { get; set; }
    public int RecommendedDurationWeeks { get; set; } = 2;
}
