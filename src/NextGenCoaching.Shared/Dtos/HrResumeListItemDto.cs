namespace NextGenCoaching.Shared.Dtos;

public class HrResumeListItemDto
{
    public Guid Id { get; init; }
    public Guid StudentProfileId { get; init; }
    public int Version { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public string? TargetRole { get; init; }
    public string Status { get; init; } = "Draft";
    public DateTime CreatedAtUtc { get; init; }
    public int? AgeHours { get; init; }
}
