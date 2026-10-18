namespace NextGenCoaching.Shared.Dtos;

public class LeadActivityDto
{
    public Guid Id { get; init; }
    public string ActivityType { get; init; } = string.Empty;
    public string? Outcome { get; init; }
    public string? Description { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public string? PerformedByName { get; init; }
}
