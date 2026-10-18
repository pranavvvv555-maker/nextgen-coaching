namespace NextGenCoaching.Shared.Dtos;

public class SelectOptionDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string? Sublabel { get; init; }
}
