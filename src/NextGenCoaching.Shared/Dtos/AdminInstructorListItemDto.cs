namespace NextGenCoaching.Shared.Dtos;

public class AdminInstructorListItemDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Headline { get; init; }
    public int? YearsExperience { get; init; }
    public bool IsPublic { get; init; }
    public int BatchCount { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
