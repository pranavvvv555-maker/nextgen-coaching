namespace NextGenCoaching.Shared.Dtos;

public class HrProfileDto
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? City { get; init; }
}
