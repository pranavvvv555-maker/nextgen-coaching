namespace NextGenCoaching.Shared.Dtos;

public class AdminStudentListItemDto
{
    public Guid Id { get; init; }            // StudentProfile.Id
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? City { get; init; }
    public string Status { get; init; } = string.Empty;
    public int EnrollmentCount { get; init; }
    public string? LatestCourseTitle { get; init; }
    public DateTime? LastLoginAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
