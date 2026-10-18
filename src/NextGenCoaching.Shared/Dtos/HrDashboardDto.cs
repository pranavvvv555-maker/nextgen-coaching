namespace NextGenCoaching.Shared.Dtos;

public class HrDashboardDto
{
    public string FullName { get; init; } = string.Empty;
    public HrStatsDto Stats { get; init; } = new();
    public IReadOnlyList<HrResumeListItemDto> RecentPendingResumes { get; init; } = Array.Empty<HrResumeListItemDto>();
    public IReadOnlyList<HrJobOpeningDto> RecentJobs { get; init; } = Array.Empty<HrJobOpeningDto>();
}

public class HrStatsDto
{
    public int ResumesUnderReview { get; init; }
    public int OpenJobs { get; init; }
    public int ActiveReferrals { get; init; }
    public int StudentsPlacedThisYear { get; init; }
}
