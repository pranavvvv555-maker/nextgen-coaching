namespace NextGenCoaching.Shared.Dtos;

public class AdminInstructorDetailDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? City { get; init; }
    public string? Headline { get; init; }
    public string? Bio { get; init; }
    public List<string> ExpertiseTags { get; init; } = new();
    public int? YearsExperience { get; init; }
    public string? LinkedinUrl { get; init; }
    public decimal? HourlyRate { get; init; }
    public bool IsPublic { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public List<AdminInstructorBatchDto> Batches { get; init; } = new();
}

public class AdminInstructorBatchDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
