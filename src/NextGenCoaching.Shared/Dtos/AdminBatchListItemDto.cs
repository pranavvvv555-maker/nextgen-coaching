namespace NextGenCoaching.Shared.Dtos;

public class AdminBatchListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = string.Empty;
    public Guid? PrimaryInstructorId { get; init; }
    public string? PrimaryInstructorName { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string Mode { get; init; } = "Weekend";
    public string? ScheduleDescription { get; init; }
    public int? Capacity { get; init; }
    public int EnrolledCount { get; init; }
    public string Status { get; init; } = "Planned";
}
