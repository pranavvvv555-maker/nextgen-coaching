namespace NextGenCoaching.Shared.Dtos;

public class SearchResultsDto
{
    public string Query { get; init; } = string.Empty;
    public List<SearchHitDto> Hits { get; init; } = new();
}

public class SearchHitDto
{
    public string Type { get; init; } = string.Empty;     // Student, Course, Instructor, Lead, Job
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public string Url { get; init; } = string.Empty;
}
