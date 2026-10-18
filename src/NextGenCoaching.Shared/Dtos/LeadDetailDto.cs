namespace NextGenCoaching.Shared.Dtos;

public class LeadDetailDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? City { get; init; }
    public string? Graduation { get; init; }
    public int? YearOfGraduation { get; init; }
    public string Source { get; init; } = string.Empty;
    public string? SourceCampaign { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? LostReason { get; init; }
    public string? CourseInterestTitle { get; init; }
    public string? AssignedCounsellorName { get; init; }
    public string? Notes { get; init; }
    public DateTime? NextFollowupAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public IReadOnlyList<LeadActivityDto> Activities { get; init; } = Array.Empty<LeadActivityDto>();
}
