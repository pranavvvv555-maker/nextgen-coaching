namespace NextGenCoaching.Shared.Dtos;

public class FaqDto
{
    public Guid Id { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Question { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}
