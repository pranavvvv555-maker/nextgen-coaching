namespace NextGenCoaching.Shared.Dtos;

public class StudentResumeDto
{
    public Guid Id { get; init; }
    public int Version { get; init; }
    public string FileUrl { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;   // Draft, UnderReview, Reviewed, Approved
    public bool IsCurrent { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public string? ReviewerNotes { get; init; }
    public DateTime? ReviewedAtUtc { get; init; }
}
