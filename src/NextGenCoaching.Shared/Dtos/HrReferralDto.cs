namespace NextGenCoaching.Shared.Dtos;

public class HrReferralDto
{
    public Guid Id { get; init; }
    public Guid JobOpeningId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string RoleTitle { get; init; } = string.Empty;
    public Guid StudentProfileId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public string Status { get; init; } = "Referred";
    public DateTime ReferredAtUtc { get; init; }
    public DateTime? StatusUpdatedAtUtc { get; init; }
    public decimal? OfferedCtcLpa { get; init; }
    public DateOnly? JoiningDate { get; init; }
    public string? Notes { get; init; }
}
