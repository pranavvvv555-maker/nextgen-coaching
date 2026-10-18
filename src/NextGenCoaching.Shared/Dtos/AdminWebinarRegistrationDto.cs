namespace NextGenCoaching.Shared.Dtos;

public class AdminWebinarRegistrationDto
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? City { get; init; }
    public bool Attended { get; init; }
    public DateTime RegisteredAtUtc { get; init; }
}
