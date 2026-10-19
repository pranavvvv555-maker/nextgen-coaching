using System.ComponentModel.DataAnnotations;
namespace NextGenCoaching.Shared.Requests;

public class UpsertCurriculumToolRequest
{
    [Required, StringLength(120)] public string ToolName { get; set; } = string.Empty;
    [Url, StringLength(500)] public string? IconUrl { get; set; }
    [StringLength(2000)] public string? Description { get; set; }
    [Range(0, 1000)] public int SortOrder { get; set; }
}

public class UpsertCurriculumJourneyStepRequest
{
    [Range(1, 200)] public int StepNumber { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(4000)] public string Description { get; set; } = string.Empty;
    public string? Icon { get; set; }
}

public class UpsertCurriculumFeatureRequest
{
    [Required, StringLength(300)] public string Label { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
}

public class UpsertCurriculumAssignmentRequest
{
    [Range(1, 1000)] public int AssignmentNumber { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(8000)] public string Description { get; set; } = string.Empty;
    [Url, StringLength(1000)] public string? ResourcesUrl { get; set; }
    [Range(0, 3650)] public int DueOffsetDays { get; set; }
    [Range(1, 10000)] public int MaxScore { get; set; } = 100;
    public bool IsPublished { get; set; } = true;
}

public class UpsertCurriculumProjectRequest
{
    [Range(1, 1000)] public int ProjectNumber { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(8000)] public string Brief { get; set; } = string.Empty;
    [Url, StringLength(1000)] public string? DatasetUrl { get; set; }
    [Range(1, 104)] public int RecommendedDurationWeeks { get; set; } = 2;
}
