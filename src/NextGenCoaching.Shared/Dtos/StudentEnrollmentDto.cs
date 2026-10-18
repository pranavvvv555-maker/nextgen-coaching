namespace NextGenCoaching.Shared.Dtos;

public class StudentEnrollmentDto
{
    public Guid EnrollmentId { get; init; }
    public Guid CourseId { get; init; }
    public string CourseSlug { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public string BatchName { get; init; } = string.Empty;
    public DateOnly BatchStartDate { get; init; }
    public DateOnly? BatchEndDate { get; init; }
    public decimal CompletionPercentage { get; init; }
    public string Status { get; init; } = string.Empty;
    public int CompletedSessions { get; init; }
    public int TotalSessions { get; init; }
}
