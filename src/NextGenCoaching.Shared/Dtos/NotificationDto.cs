namespace NextGenCoaching.Shared.Dtos;

public class NotificationDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Message { get; init; }
    public string? LinkUrl { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ReadAtUtc { get; init; }
}
