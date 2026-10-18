namespace NextGenCoaching.Shared.Dtos;

public class TestimonialDto
{
    public Guid Id { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string? PhotoUrl { get; init; }
    public string? CurrentRole { get; init; }
    public string? CurrentCompany { get; init; }
    public string? CourseTitle { get; init; }
    public string? BatchName { get; init; }
    public string Quote { get; init; } = string.Empty;
    public string? VideoUrl { get; init; }
}
