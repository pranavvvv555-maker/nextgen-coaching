namespace NextGenCoaching.Shared.Dtos;

public class StudentPlacementDto
{
    public Guid Id { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string RoleTitle { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;   // Referred, Screening, Interviewing, Offered, Joined, Rejected, Declined
    public DateTime ReferredAtUtc { get; init; }
    public DateTime? StatusUpdatedAtUtc { get; init; }
    public decimal? OfferedCtcLpa { get; init; }
    public DateOnly? JoiningDate { get; init; }
    public string? Notes { get; init; }
}
