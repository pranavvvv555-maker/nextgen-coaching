namespace NextGenCoaching.Shared.Dtos;

public class AdminUserListItemDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public List<string> Roles { get; init; } = new();
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? LastLoginAtUtc { get; init; }
    public bool IsActive { get; init; }
}
