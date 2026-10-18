namespace NextGenCoaching.Shared.Dtos;

public class LeadDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? City { get; init; }
    public string Source { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid? CourseInterestId { get; init; }
    public string? CourseInterestTitle { get; init; }
    public Guid? AssignedCounsellorId { get; init; }
    public string? AssignedCounsellorName { get; init; }
    public DateTime? NextFollowupAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
