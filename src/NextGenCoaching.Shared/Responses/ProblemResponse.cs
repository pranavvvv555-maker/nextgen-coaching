namespace NextGenCoaching.Shared.Responses;

public class ProblemResponse
{
    public string Type { get; init; } = "about:blank";
    public string Title { get; init; } = string.Empty;
    public int Status { get; init; }
    public string? Detail { get; init; }
    public string? TraceId { get; init; }
    public Dictionary<string, string[]>? Errors { get; init; }
}
