namespace NextGenCoaching.Shared.Dtos;

public class StudentAnnouncementDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public string? LinkUrl { get; init; }
}
